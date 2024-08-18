using EcommerceProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Web.Mvc;

namespace EcommerceProject.Controllers
{
    public class HomeController : Controller
    {
        private EcommerceEntities DB = new EcommerceEntities();

        public ActionResult Index(int? categoryId)
        {
            var groupedProducts = GetProductsByCategory(categoryId);
            return View(groupedProducts);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        [HttpPost]
        public ActionResult AddToCart(int productID, int quantity)
        {
            if (quantity <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var userId = Session["UserId"] as int?;
            if (userId == null)
            {
                
                return RedirectToAction("Index", "Home");
            }

            var cart = DB.ShoppingCarts.SingleOrDefault(c => c.UserID == userId.Value);
            if (cart == null)
            {
                cart = new ShoppingCart { UserID = userId.Value, CreatedAt = DateTime.Now };
                DB.ShoppingCarts.Add(cart);
                DB.SaveChanges();
            }

            var cartItem = DB.ShoppingCartItems.SingleOrDefault(ci => ci.CartID == cart.CartID && ci.ProductID == productID);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new ShoppingCartItem
                {
                    CartID = cart.CartID,
                    ProductID = productID,
                    Quantity = quantity,
                    CreatedAt = DateTime.Now
                };
                DB.ShoppingCartItems.Add(cartItem);
            }

            DB.SaveChanges();

            return RedirectToAction("Index", "Home");
        }


        private Dictionary<Category, List<Product>> GetProductsByCategory(int? categoryId)
        {
            var products = DB.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
            }

            return products
                .GroupBy(p => p.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public ActionResult Shop(int? categoryId)
        {
            var groupedProducts = GetProductsByCategory(categoryId);
            return View(groupedProducts);
        }

        public ActionResult Details(int id)
        {
            var product = DB.Products.Include(p => p.Category).FirstOrDefault(p => p.ID == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }
        public ActionResult Cart()
        {
            var userId = Session["UserId"] as int?;
            if (userId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var shoppingCart = DB.ShoppingCarts.FirstOrDefault(m => m.UserID == userId);
            if (shoppingCart == null)
            {
               
                return RedirectToAction("Index", "Home");
            }

            var shoppingCartItems = DB.ShoppingCartItems
                .Include(x => x.Product)
                .Where(m => m.CartID == shoppingCart.CartID)
                .ToList();

            var totalAmount = shoppingCartItems.Sum(item => item.Quantity * item.Product.Price);

            ViewBag.TotalAmount = totalAmount;

            return View(shoppingCartItems);
        }


        [HttpPost]
        public ActionResult DeleteItem(int id)
        {
            var item = DB.ShoppingCartItems.SingleOrDefault(i => i.ProductID == id);

            if (item != null)
            {
                DB.ShoppingCartItems.Remove(item);
                DB.SaveChanges();
            }

            return RedirectToAction("Cart");
        }


        [HttpPost]
        public ActionResult UpdateQuantity(int id, string operation)
        {
            var item = DB.ShoppingCartItems.Where(model => model.ProductID == id).FirstOrDefault();

            if (item != null)
            {
                if (operation == "increase")
                {
                    item.Quantity++;
                }
                else if (operation == "decrease" && item.Quantity > 1)
                {
                    item.Quantity--;
                }

                DB.SaveChanges();
            }

            return RedirectToAction("Cart");
        }

        [HttpPost]
        public ActionResult UpdateCart(Dictionary<int, int> quantities)
        {
            var userId = Session["UserId"] as int?;
            if (userId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var shoppingCart = DB.ShoppingCarts.FirstOrDefault(m => m.UserID == userId);
            if (shoppingCart == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var cartItems = DB.ShoppingCartItems.Where(m => m.CartID == shoppingCart.CartID).ToList();

            foreach (var item in cartItems)
            {
                if (quantities.ContainsKey(item.ProductID))
                {
                    item.Quantity = quantities[item.ProductID];
                }
            }

            DB.SaveChanges();

            return RedirectToAction("Cart");
        }

    }
}
