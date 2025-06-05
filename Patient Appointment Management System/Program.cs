// File: Program.cs (Root of your project)
using Microsoft.AspNetCore.Builder; // For WebApplicationBuilder, WebApplication
using Microsoft.AspNetCore.Hosting; // For IWebHostEnvironment
using Microsoft.AspNetCore.Http;  // For IHttpContextAccessor, Session options
using Microsoft.EntityFrameworkCore; // For DbContextOptionsBuilder, UseSqlServer
using Microsoft.Extensions.Configuration; // For IConfiguration
using Microsoft.Extensions.DependencyInjection; // For IServiceCollection, AddScoped, AddDbContext, etc.
using Microsoft.Extensions.Hosting; // For IHostEnvironment
using Patient_Appointment_Management_System.Data; // For your PatientAppointmentDbContext
using Patient_Appointment_Management_System.Services; // For all your service interfaces and implementations
using System; // For TimeSpan, InvalidOperationException

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// --- Database Context Configuration ---
// The key "PatientAppointmentConnection" MUST match the key in your appsettings.json
var connectionString = builder.Configuration.GetConnectionString("PatientAppointmentConnection");

if (string.IsNullOrEmpty(connectionString))
{
    // This exception will halt the application if the connection string is missing, which is good for diagnostics.
    throw new InvalidOperationException(
        "Connection string 'PatientAppointmentConnection' not found in appsettings.json. " +
        "Please ensure it is configured correctly under the 'ConnectionStrings' section."
    );
}

builder.Services.AddDbContext<PatientAppointmentDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- Register your custom application services here ---
// Ensure all services used by your controllers (especially AdminController) are registered.
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>(); // For logging events
builder.Services.AddScoped<IConflictService, ConflictService>();   // For handling scheduling conflicts
// Add other services as you create them (e.g., IAppointmentService, INotificationService)


// --- Session Configuration ---
builder.Services.AddDistributedMemoryCache(); // Enables session storage in memory (for development/simple scenarios)

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout duration
    options.Cookie.HttpOnly = true;  // Prevents client-side script access to the cookie
    options.Cookie.IsEssential = true; // Marks the cookie as essential for the application to function (GDPR compliance)
    // In a production environment with HTTPS, you would uncomment the following:
    // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Register IHttpContextAccessor to allow access to HttpContext from services if needed (e.g., for session in layout)
// Though it's generally better to pass necessary HttpContext data from controllers to services if possible.
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Configure a user-friendly error page for production
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts(); // Adds Strict-Transport-Security header
}
else
{
    app.UseDeveloperExceptionPage(); // Shows detailed error pages during development
    // Potentially add app.UseMigrationsEndPoint(); if you're using EF Core migrations and Identity
}

app.UseHttpsRedirection(); // Redirects HTTP requests to HTTPS
app.UseStaticFiles(); // Enables serving static files (like CSS, JavaScript, images) from wwwroot

app.UseRouting(); // Adds route matching to the middleware pipeline

// IMPORTANT: Session middleware must be configured BEFORE authentication/authorization
// and endpoint mapping if session state is used by them.
app.UseSession();

// Authentication and Authorization middleware
// app.UseAuthentication(); // Call UseAuthentication before UseAuthorization if you add specific authentication schemes (like ASP.NET Core Identity)
app.UseAuthorization();  // Enables authorization capabilities

// Map controller routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// You might want to change the default route to your admin login page if this is primarily an admin system:
// pattern: "{controller=Admin}/{action=AdminLogin}/{id?}");

app.Run(); // Starts the application