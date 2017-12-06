using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManager.Controllers
{
    public class LoginController : Controller
    {
        private readonly ProjectDBContext _context;

        public LoginController(ProjectDBContext context)
        {
            _context = context;
        }

        // GET: Sign In
        public IActionResult SignIn()
        {
            return View();
        }

        // POST: Sign In
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignIn(User user)
        {
            if (ModelState.IsValid)
            {
                if (_context.User.Any(u => u.Login == user.Login && u.Password == Encrypt(user.Password)))
                {
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("UserName", user.Login.ToString());
                    return RedirectToAction("index", "projects");
                }
                else
                {
                    ModelState.AddModelError("", "Wrong login or password.");
                }
            }
            return View(user);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("SignIn", "Login");
        }

        // GET: Login/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Login/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UserWithNewPassword user)
        {
            if (ModelState.IsValid)
            {
            var currentUser = await _context.User.FirstAsync();                
                if (currentUser.Login == user.Login && currentUser.Password == Encrypt(user.Password))
                {
                    try
                    {
                        currentUser.Password = Encrypt(user.NewPassword);

                        _context.Update(currentUser);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        throw;
                    }
                    return RedirectToAction("SignIn", "Login");
                }
            }
            return View(user);
        }

        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.Id == id);
        }

        private static string Encrypt(string value)
        {
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                UTF8Encoding utf8 = new UTF8Encoding();
                byte[] data = md5.ComputeHash(utf8.GetBytes(value));
                return Convert.ToBase64String(data);
            }
        }
    }
}
