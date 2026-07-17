using SFManagement.Application.Abstractions;
using SFManagement.Infrastructure;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Security;
using SFManagement.Web.Health;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
});

var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(rawConnectionString) && rawConnectionString.StartsWith("postgres://"))
{
    var databaseUri = new Uri(rawConnectionString);
    var userInfo = databaseUri.UserInfo.Split(':');

    connectionString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=True;";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

var encryptionKey = Convert.FromBase64String(
    builder.Configuration["EncryptionKey"]
    ?? throw new InvalidOperationException("EncryptionKey not found in configuration."));

builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddSingleton<IIdEncryptionService>(new IdEncryptionService(encryptionKey));

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

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

app.MapHealthChecks("/health");

app.Run();
