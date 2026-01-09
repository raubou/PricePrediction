using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Text.Json.Serialization;
using TMDInvestments.Models;
using static Org.BouncyCastle.Math.EC.ECCurve;

var builder = WebApplication.CreateBuilder(args);
//public static IConfiguration? configuration;
// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("TraderGo",
        builder =>
        {
            builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
            //.AllowCredentials();
        });
});

builder.Services.AddSession();
builder.Services.AddMemoryCache();

builder.Services.AddRazorPages(options =>
{
    //options.Conventions.AuthorizePage("/");
});
builder.Services.AddControllers(options =>
{
    // Suppress buffering through the file system
    options.SuppressOutputFormatterBuffering = true;
}).AddJsonOptions(options =>
{
    // Converters for types we own
    //options.JsonSerializerOptions.Converters.Add(new FirstCustomConverter());
    //options.JsonSerializerOptions.Converters.Add(new SecondCustomConverter());
    // Make sure that enums are serialised as strings by default
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    // Ignore `null` values by default to reduce the payload size
    options.JsonSerializerOptions.IgnoreNullValues = true;
});
//.AddNewtonsoftJson();

builder.Services.Configure<ConnectionSettings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
//builder.Services.Configure<ConnectionSettings2>(builder.Configuration.GetSection("ConnectionStringsNews"));
builder.Services.Configure<FinnHub>(builder.Configuration.GetSection("finnhub"));
builder.Services.Configure<Polygon>(builder.Configuration.GetSection("polygon"));
builder.Services.Configure<SchwabSettings>(builder.Configuration.GetSection("SchwabAPI"));
builder.Services.Configure<CoinBaseSettings>(builder.Configuration.GetSection("CoinBase"));
builder.Services.Configure<ETradeSettings>(builder.Configuration.GetSection("ETradeAPI"));
builder.Services.Configure<yfapiSettings>(builder.Configuration.GetSection("yfapi"));
builder.Services.AddHttpClient();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(options =>
       {
           options.LoginPath = "/Login";
           options.LogoutPath = "/Logout";
       });

builder.Services.AddAuthorization(options =>
{
    // Define a default policy that requires authenticated users
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    //// Optionally, define additional policies for specific roles or claims
    //options.AddPolicy("AdminOnly", policy =>
    //    policy.RequireRole("Admin"));

    //options.AddPolicy("EmployeeOnly", policy =>
    //    policy.RequireClaim("EmployeeNumber", "1", "2", "3"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

    options.LoginPath = "/Login"; // Your custom login page path
    options.AccessDeniedPath = "/AccessDenied";
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//}
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
    app.UseExceptionHandler("/Error");
    //app.UseStatusCodePagesWithReExecute("/404");
    //ligershark web optimizer
    //app.UseWebOptimizer(env);
}
app.UseStatusCodePagesWithRedirects("/404");

app.UseSession();

app.UseStaticFiles();

app.UseRouting();

//for login
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("TraderGo");
//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapRazorPages();
//    endpoints.MapControllers();
//});
app.MapRazorPages();
app.MapControllers();


app.Run();
