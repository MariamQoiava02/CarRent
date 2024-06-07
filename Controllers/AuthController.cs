using System.Security.Claims;
using CarRent.Data;
using CarRent.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarRent.Controllers;

public class AuthController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    // Injecting necessary services (database context and password hasher) 
    public AuthController(ApplicationDbContext dbContext, IPasswordHasher<User> passwordHasher)
    {
        _context = dbContext;
        _passwordHasher = passwordHasher;
    }
    
    // Renders Register partial view 
    public IActionResult Register()
    {
        return View();
    }

    // If user is authenticated, renders the index partial view from cars controller
    // If not authenticated, renders the login view
    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Cars");
        }
        return View();
    }

    // You need to be authenticated to log out. 
    [Authorize]
    public IActionResult Logout()
    {
        return View();
    }


    // Register POST request
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // If model state is valid (Contains all the required fields, and all constraints are correct), 
            // creates a new user class object.
            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                FavoriteCars = new List<Car>(),
                MyCars = new List<Car>()
            };

            // Hashes the password and saves it to the newly created user
            user.Password = _passwordHasher.HashPassword(user, model.Password);

            // And adds the user to the data base and saves the changes and redirects to the login page
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Login", "Auth");
        }

        return View(model);
    }
    

    // Login POST request
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            //If the model state is valid, it searches for the unique user in the database by their email 
            var user = _context.Users.SingleOrDefault(u => u.Email == model.Email);
            if (user != null)
            {
                // If the user is found, it compares the newly entered password to the original hashed password. 
                var result = _passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);
                // Checks if the apsswords are the same. 
                if (result == PasswordVerificationResult.Success)
                {
                    // If they are the same, it gathers information that is needed to be added to the token. 
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Email),
                        new Claim("FullName", user.FirstName + " " + user.LastName),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // Sign in and saving the cookies 
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Cars");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
        }

        return View(model);
    }

    //Logout action 
    
    [HttpPost]
    public async Task<IActionResult> LogoutAction()
    {
        // Removes token from cookies
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth");
    }
}