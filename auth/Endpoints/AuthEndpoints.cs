using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace auth.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/");

        group.MapGet("/login", (HttpContext context) =>
        {
            var returnUrl = System.Net.WebUtility.HtmlEncode(context.Request.Query["returnUrl"].ToString());

            return Results.Content(
                $"""
                <!DOCTYPE html>
                <html lang="ru">
                <head>
                    <meta charset="utf-8" />
                    <title>Login</title>
                </head>
                <body>
                    <form method="post" action="/login">
                        <input type="hidden" name="returnUrl" value="{returnUrl}" />
                        <label for="username">Имя</label>
                        <input type="text" id="username" name="username" autocomplete="username" />
                        <label for="password">Пароль</label>
                        <input type="password" id="password" name="password" autocomplete="current-password" />
                        <button type="submit">Войти</button>
                    </form>
                </body>
                </html>
                """,
                "text/html");
        });

        group.MapPost("/login", async (HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var returnUrl = form["returnUrl"].ToString();

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, username)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return Results.Redirect(GetSafeReturnUrl(returnUrl) ?? "/whoami");
        });

        group.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });

        group.MapGet("/whoami", (ClaimsPrincipal user) => Results.Content(
            $"""
            <!DOCTYPE html>
            <html lang="ru">
            <head>
                <meta charset="utf-8" />
                <title>Who am I</title>
            </head>
            <body>
                <p>Вы вошли как: {user.Identity?.Name ?? "anonymous"}</p>
                <form method="post" action="/logout">
                    <button type="submit">Выйти</button>
                </form>
            </body>
            </html>
            """,
            "text/html"))
            .RequireAuthorization();

        return app;
    }

    private static string? GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
        {
            return returnUrl;
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri) &&
            (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
        {
            return uri.ToString();
        }

        return null;
    }
}
