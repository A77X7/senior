using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using ocr.Endpoints;
using ocr.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection().SetApplicationName("senior-auth");

var authBaseUrl = builder.Configuration["Auth:BaseUrl"] ?? "http://localhost:5085";

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            var returnUrl = Uri.EscapeDataString(
                $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}");
            context.Response.Redirect($"{authBaseUrl}/login?ReturnUrl={returnUrl}");
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<TesseractOptions>(builder.Configuration.GetSection("Tesseract"));

builder.Services.AddAntiforgery();

builder.Services.AddHttpClient<TesseractClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<TesseractOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapOcrEndpoints();

app.Run();
