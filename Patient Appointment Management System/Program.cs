// File: Program.cs (Root of your project)
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data; // For your PatientAppointmentDbContext

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// --- Database Context Configuration ---
// Retrieve the connection string from appsettings.json.
// MAKE SURE "PatientAppointmentConnection" is the EXACT name of your connection string key in appsettings.json
var connectionString = builder.Configuration.GetConnectionString("PatientAppointmentConnection");

// Check if the connection string is found
if (string.IsNullOrEmpty(connectionString))
{
    // This helps in early detection of configuration issues.
    // In a real app, you might log this error more robustly.
    throw new InvalidOperationException(
        "Connection string 'PatientAppointmentConnection' not found in appsettings.json. " +
        "Please ensure it is configured correctly. Example: \n" +
        "\"ConnectionStrings\": {\n" +
        "  \"PatientAppointmentConnection\": \"Server=YOUR_SERVER;Database=YourDbName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True\"\n" +
        "}"
    );
}

// Register your DbContext (PatientAppointmentDbContext) with SQL Server
builder.Services.AddDbContext<PatientAppointmentDbContext>(options =>
    options.UseSqlServer(connectionString));


// --- Session Configuration ---
builder.Services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache for session state

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set the session timeout duration
    options.Cookie.HttpOnly = true; // Make the session cookie inaccessible to client-side script (security best practice)
    options.Cookie.IsEssential = true; // Mark the session cookie as essential for GDPR compliance and basic functionality
    // For production, if you are using HTTPS, consider uncommenting:
    // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Custom error handling page
    app.UseHsts(); // Adds Strict-Transport-Security header (forces HTTPS after first visit in production)
}
else
{
    // Optional: Developer exception page for detailed errors in development
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS
app.UseStaticFiles();      // Enable serving static files (CSS, JavaScript, images) from wwwroot

app.UseRouting();          // Enables routing decisions

app.UseSession();          // IMPORTANT: Enable session middleware. Must be called BEFORE UseAuthorization and endpoint mapping if session is used.

app.UseAuthentication();   // Call UseAuthentication before UseAuthorization if you add authentication schemes (like Identity)
app.UseAuthorization();    // Enable authorization capabilities

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Default route configuration

app.Run(); // Start the application