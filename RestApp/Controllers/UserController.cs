using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Needed for SaveChangesAsync
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using restapp.Dal;
using restapp.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace restapp.Controllers
{
    public class UserController : Controller
    {
        private readonly RestContext _context;

        public UserController(RestContext context)
        {
            _context = context;
        }

        
        // --- AUTHENTICATION ACTIONS ---

        public IActionResult Login(string returnUrl) // returns login.cshtml (view)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public IActionResult Logout() // removing the user logged in details from the session
        {
            HttpContext.Session.Remove("loggedinuser");
            HttpContext.Session.Remove("loggedinuserRole");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult ValidateUser(UserLogin ul)
        {
            if (!ModelState.IsValid)
            {
                return View("Login", ul);
            }

            User? user = _context.users.FirstOrDefault(u => u.UserId.ToLower() == ul.UserId.ToLower());

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "The username you entered is not registered.");
                return View("Login", ul);
            }

            if (user.Password != ul.Password)
            {
                ModelState.AddModelError(string.Empty, "The password you entered is incorrect.");
                return View("Login", ul);
            }

            if (user.Status == true)
            {
                // Find the role based on the RoleId
                Role? r = _context.roles.Find(user.RoleId);

                if (r != null)
                {
                    HttpContext.Session.SetString("loggedinuser", ul.UserId);
                    HttpContext.Session.SetString("loggedinuserRole", r.RoleName); // Store the actual role name

                    if (r.RoleName == "Admin")
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (r.RoleName == "User")
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Your account has an unrecognized role configuration.");
                        return View("Login", ul);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Could not verify user role.");
                    return View("Login", ul);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Your account is inactive. Please contact the administrator.");
                return View("Login", ul);
            }
        }
        // --- REGISTRATION & PROFILE ACTIONS (Omitted for brevity, assuming they are correct) ---
        public IActionResult Register() { 
            
            return View(); 
        }

        [HttpPost]
        //triggered after registration form is submitted
        public IActionResult RegisterUser(User u)
        {
            // ... (Your RegisterUser implementation) ...
            //Checks if the form data is valid based on the User model's data annotations.
            if (!ModelState.IsValid) return View("Register", u);

            try
            {
                if (_context.users.Any(user => user.UserId.ToLower() == u.UserId.ToLower()))
                {
                    
                    ModelState.AddModelError("UserId", "This Username is already taken.");
                    return View("Register", u);
                }
                //Assigns the "User" role to the newly registered user.
                Role? defaultRole = _context.roles.FirstOrDefault(r => r.RoleName == "User");
                u.RoleId = defaultRole?.RoleId ?? 0;

                //Adds the new user to the database and saves changes.
                _context.users.Add(u);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! Please login with your new credentials.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during registration.");
                return View("Register", u);
            }
        }
        public IActionResult Profile()
        {
            //Checks if user is logged in
            string? loggedInUserId = HttpContext.Session.GetString("loggedinuser");
            if (loggedInUserId == null) return RedirectToAction("Login", "User");

            //if found get all details about that user
            User? user = _context.users.FirstOrDefault(u => u.UserId.ToLower() == loggedInUserId.ToLower());
            if (user == null) { HttpContext.Session.Clear(); return RedirectToAction("Login", "User"); }

            return View(user);
        }
        [HttpPost]
        public IActionResult Profile(User updatedUser)
        {
            //Checks if user is logged in
            string? loggedInUserId = HttpContext.Session.GetString("loggedinuser");
            if (loggedInUserId == null) return RedirectToAction("Login", "User");

            //Prevents users from editing others' profiles.
            if (updatedUser.UserId.ToLower() != loggedInUserId.ToLower()) return Forbid();

            //Ignore validation for RoleId and Status:
            ModelState.Remove("RoleId");
            ModelState.Remove("Status");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return View(updatedUser);
            }

            try
            {
                //Find original user record

                User? originalUser = _context.users.Find(updatedUser.Id);

                if (originalUser == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Logout", "User");
                }
                //Update fields

                originalUser.FirstName = updatedUser.FirstName;
                originalUser.LastName = updatedUser.LastName;
                originalUser.Mobile = updatedUser.Mobile;
                originalUser.Email = updatedUser.Email;

                if (!string.IsNullOrEmpty(updatedUser.Password))
                {
                    originalUser.Password = updatedUser.Password;
                }

                _context.users.Update(originalUser);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Your profile has been updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred while saving your details.";
                return View(updatedUser);
            }
        }

        // --- CART ACTIONS ---

        // Helper to get cart from session
        private List<CartItem> GetCartFromSession()
        {
            // Uses the session key "ShoppingCart"
            //here in the session memory the data is stored in the form of key and value pairs
            string? cartJson = HttpContext.Session.GetString("ShoppingCart");

            // Checks if the session string is null/empty and deserializes if found
            //Deserialization means converting that JSON string back into a
            //C# object so you can work with it in your code.
            return string.IsNullOrEmpty(cartJson)
                   ? new List<CartItem>() // for true
                   : Newtonsoft.Json.JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();
            //turns it into a List<CartItem> object.if there is no cart in the session
        }

        // Helper to save cart to session
        private void SaveCartToSession(List<CartItem> cart)
        {
            //Serializes the cart list into a JSON string.
            
            string updatedCartJson = Newtonsoft.Json.JsonConvert.SerializeObject(cart);

            //Stores it in session under the key "ShoppingCart".
            HttpContext.Session.SetString("ShoppingCart", updatedCartJson);
        }

        

        [HttpPost]
        public IActionResult AddToCartSubmit(int id, string returnUrl)
        {
            // Check if user is logged in
            string? loggedInUserId = HttpContext.Session.GetString("loggedinuser");
            if (loggedInUserId == null)
            {
                return RedirectToAction("", "Home", new { returnUrl = returnUrl });
            }
            var foodItem = _context.fooditems.FirstOrDefault(f => f.ItemId == id);
            if (foodItem == null) 
            {
                TempData["ErrorMessage"] = "Item not found."; 
                return RedirectToAction("Menu", "Home"); 
            }

            List<CartItem> cart = GetCartFromSession();
            CartItem? existingItem = cart.FirstOrDefault(item => item.FoodItemId == id);

            if (existingItem != null)
            {
                existingItem.Quantity++;
                TempData["SuccessMessage"] = $"{existingItem.Name}'s quantity increased";
            }
            else
            {
                var newCartItem = new CartItem
                {
                    FoodItemId = foodItem.ItemId,
                    Name = foodItem.ItemName,
                    Price = (decimal)foodItem.SellingPrice,
                    Quantity = 1
                };
                cart.Add(newCartItem);
                TempData["SuccessMessage"] = $"{foodItem.ItemName} added to cart";
            }

            SaveCartToSession(cart);
            
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            
            return RedirectToAction("Menu", "Home");
        }

        public IActionResult Cart()
        //this action triggers when the user clicks on the cart icon in the nav bar 
        {
            string? loggedInUserRole = HttpContext.Session.GetString("loggedinuserRole");
            if (loggedInUserRole != "User")
            {
                return RedirectToAction("Index", "Home");
            }

            List<CartItem> cart = GetCartFromSession();
            return View(cart);
        }

        [HttpPost]
        public IActionResult RemoveFromCartSubmit(int id, string returnUrl)
        {
            List<CartItem> cart = GetCartFromSession();
            CartItem? existingItem = cart.FirstOrDefault(item => item.FoodItemId == id);

            if (existingItem != null)
            {
                existingItem.Quantity--;

                if (existingItem.Quantity <= 0)
                {
                    cart.Remove(existingItem);
                    TempData["SuccessMessage"] = $"{existingItem.Name} has been completely removed from your cart.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"{existingItem.Name} quantity reduced to {existingItem.Quantity}.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Could not find item in cart to reduce quantity.";
            }

            SaveCartToSession(cart);

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Menu", "Home"); // Fallback
        }

        [HttpPost]
        public IActionResult RemoveItemFullyFromCart(int id)
        {
            string? loggedInUserRole = HttpContext.Session.GetString("loggedinuserRole");
            if (loggedInUserRole != "User")
            {
                TempData["ErrorMessage"] = "Unauthorized action.";
                return RedirectToAction("Login", "User");
            }

            List<CartItem> cart = GetCartFromSession();
            CartItem? existingItem = cart.FirstOrDefault(item => item.FoodItemId == id);
            string itemName = "Item";

            if (existingItem != null)
            {
                itemName = existingItem.Name;
                cart.Remove(existingItem);
                TempData["SuccessMessage"] = $"{itemName} has been completely removed from your cart.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not find item in cart to remove.";
            }

            SaveCartToSession(cart);
            return RedirectToAction("Cart");
        }

        // --- ORDER & PAYMENT ACTIONS ---
        // --- HELPER METHODS ---

        // NEW: Helper to get the logged-in user ID from the session
        private string? GetUserId()
        {
            // The session key is "loggedinuser" as per login logic
            return HttpContext.Session.GetString("loggedinuser");
        }

        public IActionResult Checkout()
        {
            string? userId = GetUserId();
            List<CartItem> cartItems = GetCartFromSession();

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Please log in to proceed.";
                return RedirectToAction("Login", "User");
            }

            if (cartItems.Count == 0)
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Menu", "Home");
            }
            //Calculates the total cost of all items in the cart.
            decimal cartTotal = cartItems.Sum(i => i.Price * i.Quantity);
            ViewData["CartTotal"] = cartTotal;
            
            return View("PaymentSelection");
        }



        // Helper to get the logged-in user's Primary Key (int) from the database
        private int? GetUserPkId()
        {
            //You need the user's primary key (Id)
            //to link the order and payment records to the correct user.
            string? loggedInUsername = GetUserId();
            if (string.IsNullOrEmpty(loggedInUsername)) return null;

            int? userPkId = _context.users
                .Where(u => u.UserId.ToLower() == loggedInUsername.ToLower())
                .Select(u => (int?)u.Id)
                .FirstOrDefault();

            return userPkId;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string paymentMethod = "COD")
        {
            //logged in user pk Id
            //nullable integer
            int? userPkId = GetUserPkId();
            List<CartItem> cartItems = GetCartFromSession();
            
            if (userPkId == null || cartItems.Count == 0)
            {
                TempData["ErrorMessage"] = "Unable to process order. Please try again.";
                return RedirectToAction("Cart");
            }
        //userId: refers to the user's primary key (Id).
        //ForFk: means it's being used as a foreign key in other tables like Order, Payment.

            //converting nullable to int value
            int userIdForFk = userPkId.Value;

            decimal orderTotal = 0;

            try
            {
                // Create Order
                Order newOrder = new Order
                {
                    Id = userIdForFk,
                    OrderDate = DateTime.Now
                };
                _context.orders.Add(newOrder);
                await _context.SaveChangesAsync();

                // Create Order Details
                //Loops through each cart item.
                foreach (var item in cartItems)
                {
                    OrderDetails detail = new OrderDetails
                    {
                        OrderId = newOrder.OrderId,
                        ItemId = item.FoodItemId,
                        Price = item.Price,
                        QuantityOrdered = item.Quantity,
                        Amount = item.Price * item.Quantity
                    };
                    _context.orderdetails.Add(detail);
                    orderTotal += detail.Amount;
                }

                // Create Payment
                Payment payment = new Payment
                {
                    Id = userIdForFk,
                    BillAmount = orderTotal,
                    PaymentMethod = paymentMethod,
                    Status = true
                };
                _context.payments.Add(payment);
                await _context.SaveChangesAsync();

                // Clear cart and redirect
                HttpContext.Session.Remove("ShoppingCart");
                TempData["SuccessMessage"] = $"Order #{newOrder.OrderId} placed successfully!";
                return RedirectToAction("OrderConfirmation", new { orderId = newOrder.OrderId });
            }
            catch (Exception ex)
            { 
                TempData["ErrorMessage"] = "Order failed to save due to an internal error."; ;
                return RedirectToAction("Cart");
            }

        }

        public IActionResult OrderConfirmation(int orderId)
        {
            //this is the ID of the order that was just placed.
            ViewData["OrderId"] = orderId;
            return View();
        }
         
    }
}