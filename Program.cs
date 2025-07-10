using AwsHelper.Services;
using Amazon.SimpleSystemsManagement;
using AwsHelper.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configuração de localização para português brasileiro
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("pt-BR") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("pt-BR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSimpleSystemsManagement>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ParameterStoreService>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
db.Database.Migrate();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();