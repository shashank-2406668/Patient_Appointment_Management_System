// File: Program.cs (Root of your project)
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data; // For your PatientAppointmentDbContext
// ---- Add this if you created the Services folder and IAdminService/AdminService ----
using Patient_Appointment_Management_System.Services;

// In Program.cs

// ... other using statements

var builder = WebApplication.CreateBuilder(args);

// ... AddControllersWithViews, DbContext configuration ...

builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPatientService, PatientService>(); // <-- ADD THIS
builder.Services.AddScoped<IDoctorService, DoctorService>();   // <-- ADD THIS
builder.Services.AddScoped<ISystemLogService, SystemLogService>(); // <-- ADD THIS
builder.Services.AddScoped<IConflictService, ConflictService>();   // <-- ADD THIS

// ... Session Configuration ...
// ... var app = builder.Build(); ...

// Add services to the container.
builder.Services.AddControllersWithViews();

// --- Database Context Configuration ---
var connectionString = builder.Configuration.GetConnectionString("PatientAppointmentConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'PatientAppointmentConnection' not found in appsettings.json. " +
        "Please ensure it is configured correctly. Example: \n" +
        "\"ConnectionStrings\": {\n" +
        "  \"PatientAppointmentConnection\": \"Server=YOUR_SERVER;Database=YourDbName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True\"\n" +
        "}"
    );
}

builder.Services.AddDbContext<PatientAppointmentDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- Register your custom services here ---
// If you have created IAdminService and AdminService, register them:
builder.Services.AddScoped<IAdminService, AdminService>();
// Add other services like IPatientService, IDoctorService as you create them





// --- Session Configuration ---
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Uncomment in production if using HTTPS
});



var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: app.UseSession() must be called BEFORE app.UseAuthentication() and app.UseAuthorization()
// if your authentication or authorization logic relies on session state.
// It should also generally be before endpoint mapping (app.MapControllerRoute).
app.UseSession();

app.UseAuthentication();   // Call UseAuthentication before UseAuthorization if you add authentication schemes (like Identity)
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();