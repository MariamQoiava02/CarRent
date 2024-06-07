using System.Security.Claims;
using CarRent.Data;
using CarRent.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRent.Controllers;


[Authorize]
public class UserController : Controller
{
    private readonly ApplicationDbContext _context;

    // Injects the database context into the controller
    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Gets the profile and shows it in the profile view
    public async Task<IActionResult> Profile()
    {
        // takes the user id from the token
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Checks if the user is authorized
        if (userId == null)
        {
            return Unauthorized();
        }

        // Gets the user from the database. 
        var user = await _context.Users
            .Include(u => u.FavoriteCars)
            .Include(u => u.RentedCars).ThenInclude(cr => cr.Car)
            .Include(u => u.MyCars)
            .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

        if (user == null)
        {
            return NotFound();
        }

        // Creates a new object of the UserProfileViewModel 
        var viewModel = new UserProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            FavoriteCars = user.FavoriteCars,
            RentedCars = user.RentedCars,
            MyCars = user.MyCars
        };

        // And shows it in the profile partial view
        return View(viewModel);
    }
}