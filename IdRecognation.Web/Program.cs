using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add MVC (controllers + views)
builder.Services.AddControllersWithViews();

// Add Infrastructure & Application layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Swagger (for API testing, optional)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build app
var app = builder.Build();

// Migrate database automatically (optional but handy during dev)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
    dbContext.Database.Migrate();
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

// Static files for images, CSS, etc.
app.UseStaticFiles();

// Routing
app.UseRouting();

// (Add if you'll have identity later)
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Card}/{action=Index}/{id?}");

// === TESSDATA DEBUG INFO ===
Console.WriteLine("=== TESSDATA DEBUG INFO ===");
var baseDir = AppContext.BaseDirectory;
Console.WriteLine($"App Base Directory: {baseDir}");

var tessdataPath = Path.Combine(baseDir, "tessdata");
Console.WriteLine($"Expected tessdata path: {tessdataPath}");
Console.WriteLine($"Tessdata directory exists: {Directory.Exists(tessdataPath)}");

if (Directory.Exists(tessdataPath))
{
    var files = Directory.GetFiles(tessdataPath);
    Console.WriteLine($"Files in tessdata: {string.Join(", ", files)}");

    var engFile = Path.Combine(tessdataPath, "eng.traineddata");
    Console.WriteLine($"eng.traineddata exists: {File.Exists(engFile)}");
    if (File.Exists(engFile))
    {
        var fileInfo = new FileInfo(engFile);
        Console.WriteLine($"eng.traineddata size: {fileInfo.Length} bytes");
        Console.WriteLine("✅ Tessdata is ready for OCR!");
    }
}
else
{
    Console.WriteLine("❌ tessdata folder NOT found in output directory!");
    Console.WriteLine("Checking Infrastructure bin directory...");

    var infraPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "IdRecognation.Infrastructure", "bin", "Debug", "net8.0", "tessdata");
    Console.WriteLine($"Infrastructure tessdata path: {Path.GetFullPath(infraPath)}");
    Console.WriteLine($"Exists: {Directory.Exists(infraPath)}");
}
Console.WriteLine("=== END DEBUG INFO ===");

app.Run();