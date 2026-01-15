
using FirebaseAdmin;
using Google.Api;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using WebApplication2.Interface;
using WebApplication2.Models;
using WebApplication2.Services;

var builder = WebApplication.CreateBuilder(args);

// Debug: Check configuration
var firestoreSettings = builder.Configuration.GetSection("FirestoreSettings").Get<FirestoreSettings>();
if (firestoreSettings != null)
{
    Console.WriteLine($"ProjectId: {firestoreSettings.ProjectId}");
    Console.WriteLine($"CredentialsPath: {firestoreSettings.CredentialsPath}");
    Console.WriteLine($"StorageBucket: {firestoreSettings.StorageBucket}");

    // Convert to absolute path
    var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), firestoreSettings.CredentialsPath);
    Console.WriteLine($"Absolute path: {absolutePath}");
    Console.WriteLine($"File exists: {File.Exists(absolutePath)}");
}
else
{
    Console.WriteLine("❌ FirestoreSettings not found in configuration");
}

// 🔥 CRITICAL: Initialize Firebase Admin SDK
try
{
    if (firestoreSettings != null)
    {
        var absoluteCredentialsPath = Path.Combine(Directory.GetCurrentDirectory(), firestoreSettings.CredentialsPath);

        if (File.Exists(absoluteCredentialsPath))
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(absoluteCredentialsPath),
                ProjectId = firestoreSettings.ProjectId
                // Remove StorageBucket - it's not a valid property in AppOptions
            });
            Console.WriteLine("✅ Firebase Admin SDK initialized successfully!");
            Console.WriteLine($"✅ Storage Bucket: {firestoreSettings.StorageBucket}");
        }
        else
        {
            Console.WriteLine($"❌ Credentials file not found at: {absoluteCredentialsPath}");
            throw new FileNotFoundException("Firebase credentials file not found");
        }
    }
    else
    {
        Console.WriteLine("❌ Cannot initialize Firebase - FirestoreSettings not found");
        throw new InvalidOperationException("FirestoreSettings not found in configuration");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Firebase initialization failed: {ex.Message}");
    throw;
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<FirestoreSettings>(
    builder.Configuration.GetSection("Firestore"));
// Configure FirestoreSettings from appsettings.json
builder.Services.Configure<FirestoreSettings>(
    builder.Configuration.GetSection("FirestoreSettings"));
builder.Services.AddSingleton<FirestoreDb>(provider =>
{
    //var firestoreService = provider.GetRequiredService<FirestoreService>();
    //return firestoreService.GetFirestoreDb();
    var settings = provider.GetRequiredService<IOptions<FirestoreSettings>>().Value;
    var builder = new FirestoreDbBuilder
    {
        ProjectId = settings.ProjectId,
        CredentialsPath = settings.CredentialsPath
    };
    return builder.Build();
});
// Register services
builder.Services.AddSingleton<FirestoreService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<FirebaseStorageService>();
builder.Services.AddScoped<TrainingService>(); builder.Services.AddScoped<IReportService, ReportService>();

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// Add session support (for your layout)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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
app.UseSession(); // Add session middleware
app.UseAuthentication();
app.UseAuthorization();

// ✅ Add explicit routes
app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

app.MapControllerRoute(
    name: "profile",
    pattern: "Profile/{action=Index}/{id?}",
    defaults: new { controller = "Profile" });
app.MapControllerRoute(
    name: "reports",
    pattern: "Reports/{action=Index}/{id?}",
    defaults: new { controller = "Reports" });

app.MapControllerRoute(
    name: "employee",
    pattern: "Employee/{action=Employee}/{id?}",
    defaults: new { controller = "Employee" });
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Employee}/{action=EmployeeSkills}/{id?}");

app.MapControllerRoute(
    name: "employee-details",
    pattern: "Employee/Details/{id}",
    defaults: new { controller = "Employee", action = "Details" });

app.Run();