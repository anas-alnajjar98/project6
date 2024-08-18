using EcommerceProject.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using System.Web;
using System.Web.Mvc;
using System.Net;

namespace EcommerceProject.Controllers
{
    public class UserController : Controller
    {

        private EcommerceEntities DB = new EcommerceEntities();
        // GET: User
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Login() { return View(); }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User model)
        {
            if (ModelState.IsValid)
            {
                var user = DB.Users.FirstOrDefault(x => x.Email == model.Email && x.Password == model.Password);
                if (user != null)
                {
                    Session["UserId"] = user.ID;
                    Session["UserEmail"] = user.Email;
                    Session["FullName"] = user.Name;
                    ViewBag.LoginSuccess = true;
                    Session["islogin"] = true;
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.LoginSuccess = false;
                    ViewBag.LoginMessage = "Invalid email or password!";
                    Session["islogin"] = false;
                }
            }
            return View();
        }
        public ActionResult Register() { return View(); }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (DB.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Email already in use.");
                    return View(model);
                }
                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = model.Password,
                    Image = "Default.png"
                };

                DB.Users.Add(user);
                await DB.SaveChangesAsync();
                return RedirectToAction("Login", "User");
            }

            return View(model);
        }
        public ActionResult Logout() { Session["islogin"] = false; return RedirectToAction("Index", "Home"); }
        public ActionResult Profile(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = DB.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile([Bind(Include = "ID,Name,Email,Password,Image")] User user)
        {

            if (ModelState.IsValid)
            {
                if (user.Image == null)
                {
                    user.Image = "Default.png"; // Optionally set to a specific default image path

                    DB.Entry(user).State = EntityState.Modified;
                    DB.SaveChanges();
                    return RedirectToAction("Index","Home");
                }
            }

            return View();
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        public ActionResult ForgotPassword(string email)
        {
            var checkInputs = DB.Users.FirstOrDefault(model => model.Email == email);

            if (checkInputs != null)
            {
                Session["UserID"] = checkInputs.ID;
                ViewBag.emailError = " ";
                ViewBag.alert = true;

                Random rand = new Random();
                string randomNumber = rand.Next(100000, 1000000).ToString();
                Session["otp"] = randomNumber;
                DB.Entry(checkInputs).State = EntityState.Modified;
                DB.SaveChanges();

                try
                {
                    string fromEmail = "techlearnhub.contact@gmail.com";
                    string fromName = "Support Team";
                    string subjectText = "Your OTP Code";
                    string messageText = $@"
        <html>
        <body dir='rtl'>
            <h2>Hello {checkInputs.Name}</h2>
            <p><strong>Your OTP code is {randomNumber}. This code is valid for a short period of time.</strong></p>
            <p>If you have any questions or need additional assistance, please feel free to contact our support team.</p>
            <p>Best wishes,<br>Support Team</p>
        </body>
        </html>";

                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(fromName, fromEmail));
                    message.To.Add(new MailboxAddress("", checkInputs.Email));
                    message.Subject = subjectText;
                    message.Body = new TextPart("html") { Text = messageText };

                    using (var client = new MailKit.Net.Smtp.SmtpClient())
                    {
                        client.Connect("smtp.gmail.com", 465, true);
                        client.Authenticate("techlearnhub.contact@gmail.com", "lyrlogeztsxclank");
                        client.Send(message);
                        client.Disconnect(true);
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.emailError = "An error occurred while sending the email. Please try again later.";
                    return View();
                }

                return RedirectToAction("SetNewPassword");
            }
            else
            {
                ViewBag.emailError = "Invalid Email";
                ViewBag.alert = false;
                return View();
            }
        }

        public ActionResult SetNewPassword(string otp, string newPassword, string confirmNewPassword)
        {
            var user = DB.Users.Find((int)Session["UserID"]);

            if (newPassword == confirmNewPassword && Session["otp"].ToString() == otp)
            {

                user.Password = newPassword;

                DB.Entry(user).State = EntityState.Modified;
                DB.SaveChanges();

                return RedirectToAction("Login","User");
            }
            else
            {
                ViewBag.error = "Passwords don't match!";
                return View();
            }

        }

    }
   }