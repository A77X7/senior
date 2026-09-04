using auth.Endpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection().SetApplicationName("senior-auth");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "text/html; charset=utf-8";
            return context.Response.WriteAsync(
                "<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\" /><title>401</title></head>" +
                "<body><h1>401 Unauthorized</h1><p>Требуется вход. <a href=\"/login\">Войти</a></p></body></html>");
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

app.Run();
