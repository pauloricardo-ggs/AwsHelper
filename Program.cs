using AwsHelper.Services;
using Amazon.SimpleSystemsManagement;
using Amazon.S3;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("pt-BR") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("pt-BR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSimpleSystemsManagement>();
builder.Services.AddAWSService<IAmazonS3>();

builder.Services.AddSingleton<JsonDataService>();

builder.Services.AddScoped<ParameterStoreService>();
builder.Services.AddScoped<AwsProfileService>();
builder.Services.AddScoped<S3Service>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();