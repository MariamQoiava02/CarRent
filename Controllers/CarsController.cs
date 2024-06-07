using System.Security.Claims;
using CarRent.Data;
using CarRent.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRent.Controllers;

public class CarsController : Controller
{
    private readonly ApplicationDbContext _context;

    // Injecting database context to the controller
    public CarsController(ApplicationDbContext dbContext)
    {
        _context = dbContext;
    }
    
    [HttpGet]
    [Authorize]
    public IActionResult AddCar()
    {
        return View();
    }
    
    // Tries to show car details view.
    [HttpGet]
    public async Task<IActionResult> CarDetails(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null)
        {
            return NotFound();
        }

        return View(car);
    }
    

    // Adds a new car to the database
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddCar(AddCarViewModel model)
    {
        if (ModelState.IsValid)
            //If the model state is valid
        {
            // Get the logged-in user's ID from the claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            // Find the user in the database
            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            // Set the owner and phone number of the car
            var car = new Car
            {   
                Brand = model.Brand,
                Year = model.Year,
                PriceDay = model.PriceDay,
                Capacity = model.Capacity,
                Transmision = model.Transmision,
                City = model.City,
                TankCapacity = model.TankCapacity,
                Pic1 = model.Pic1,
                Pic2 = model.Pic2,
                Pic3 = model.Pic3,
                OwnerId = user.Id,
                UsersWhoFavorited = new List<User>(),
                PhoneNumber = user.PhoneNumber
            };

            // Add the car to the database
            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("CarDetails", new { id = car.Id });
        }

        return BadRequest(ModelState);
    }
    
    [HttpGet]
    public async Task<IActionResult> Index(string? city, int? minYear, int? maxYear, int? capacity)
    {
        // Getting a logged in user's id
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        // Brings the favourite cars list of the logged in user
        var list = _context.Users.Include(u => u.FavoriteCars).SingleOrDefault(u => u.Id == userId)?.FavoriteCars?.ToList();
        
        // Brings the whole list of cars
        var cars = _context.Cars.AsQueryable();

        // filters by the following conditions
        if (!string.IsNullOrEmpty(city))
        {
            cars = cars.Where(c => c.City == city);
        }

        if (minYear.HasValue)
        {
            cars = cars.Where(c => c.Year >= minYear);
        }

        if (maxYear.HasValue)
        {
            cars = cars.Where(c => c.Year <= maxYear);
        }

        if (capacity.HasValue)
        {
            cars = cars.Where(c => c.Capacity == capacity);
        }

        var carList = await cars.ToListAsync();

        // Creates a filtered view model of cars 
        var model = new CarFilterViewModel
        {
            City = city,
            MinYear = minYear,
            MaxYear = maxYear,
            Capacity = capacity,
            UserId = userId,
            FavCars = list,
            Cars = carList
        };
        

        return View(model);
    }

    // Adds to favorites
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddToFavorites(int id)
    {
        // Takes the user id, checks if it's authorized
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (userId == null)
        {
            return Unauthorized();
        }

        // Takes the use in the database, if it can be found
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }


        // Checks if a car can be found in the database and gets it 
        var car = await _context.Cars.FindAsync(id);
        if (car == null)
        {
            return NotFound();
        }

        // If a car has not been favorited by anyone, a new empty list is created
        if (car.UsersWhoFavorited == null)
        {
            car.UsersWhoFavorited = new List<User>();
        }

        // Checks if I haven't favorited the car, and adds me to the favorited users 
        if (!car.UsersWhoFavorited.Any(u => u.Id == userId))
        {
            car.UsersWhoFavorited.Add(user);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

        // Removes cars from my favorited list
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveFromFavorites(int id)
        {
            // Gets the user's id and checks if it's authorized
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }
            
            // Gets the user from the database using the id
            var user = await _context.Users.FindAsync(int.Parse(userId));
            // Checks if the user exists in the database
            if (user == null)
            {
                return NotFound();
            }

            // Gets the car from the database by the id 
            var car = _context.Cars.Include(u => u.UsersWhoFavorited).FirstOrDefault(u => u.Id == id);
            // Checks if the car exists in the database
            if (car == null)
            {
                return NotFound();
            }

            // If I have favorited the car, then it removes me from the UsersWhoFavortied list. 
            if (car.UsersWhoFavorited != null && car.UsersWhoFavorited.Any(u => u.Id == user.Id))
            {
                car.UsersWhoFavorited.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }


        // Renting out a car
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RentCar(int carId) 
        {
            // Gets the user id from the token
            string nameIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Finds and gets the car from the database
            var car = await _context.Cars.FindAsync(carId);

            // Finds and gets the user from the database by the user id 
            var user = await _context.Users
                .Where(u => u.Id == int.Parse(nameIdentifier))
                .FirstOrDefaultAsync();
            
            // Extract rental days from rental days field. 
            int rentalDays;
            // Tries to get the value of the rental days and saves it in the RentalDaysString
            if (Request.Form.TryGetValue("rentalDays", out var rentalDaysString))
            {
                // Parses the string to an int and saves it in rentalDays
                if (!int.TryParse(rentalDaysString, out rentalDays))
                {
                    // Handle invalid rental days format
                    return BadRequest("Invalid rental days format"); // Or a more informative message
                }
            }
            else
            {
                // Handle missing rental days information
                return BadRequest("Missing rental days information");
            }
            
            // Creates a new car rental object, or a row in the database 
            var rental = new CarRental
            {
                Car = car,
                User = user,
                City = car.City,
                RentDate = DateTime.UtcNow,
                RentalDays = rentalDays,
                PricePaid = rentalDays * car.PriceDay
            };
            
            // Saves it in the dataset.
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    
}