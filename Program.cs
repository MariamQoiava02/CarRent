using CarRent.Data;
using CarRent.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add controllers and views to the services
builder.Services.AddControllersWithViews();

// Connect to the database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        // Takes default connection string from appsettings.json
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 23))
    ));

// Helps to hash password for the user
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Adds cookie based authentication service to the application
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
    });

var app = builder.Build();

// Call the seed method
// To populate the new database, run only once. 

/*using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await ApplicationDbInitializer.Seed(services);
}*/


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Adds authentication to the application
app.UseAuthentication();
app.UseAuthorization();

// Defines the main route for the application
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();