using SFManagement.Application.Abstractions;
using SFManagement.Infrastructure;
using SFManagement.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var encryptionKey = Convert.FromBase64String(
    builder.Configuration["EncryptionKey"]
    ?? throw new InvalidOperationException("EncryptionKey not found in configuration."));

builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddSingleton<IIdEncryptionService>(new IdEncryptionService(encryptionKey));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
