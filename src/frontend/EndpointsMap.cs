using Auth0.AspNetCore.Authentication;
using frontend.Services;
using frontend.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;

namespace frontend;

public static class EndpointsMap
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/proxy/images/{id}", async (HttpService HttpService, long id) =>
        {
            try
            {
                var response = await HttpService.GetImagesByIdAsync(id);

                if(!response.IsSuccessStatusCode)
                {
                    return Results.NotFound();
                }

                var stream = await response.Content.ReadAsStreamAsync();

                return Results.Stream(stream, "image/jpeg");
            }
            catch(Exception exception)
            {
                Console.WriteLine($"[ERROR] [/proxy/images/{id}]: Exception message: {exception.Message}");
                return Results.Problem();
            }
        });

        builder.MapGet("/proxy/archive/download/{jobId}", async (HttpService HttpService, Guid jobId) =>
        {
            try
            {
                var response = await HttpService.GetArchiveDownloadAsync(jobId);

                if(!response.IsSuccessStatusCode)
                {
                    return Results.NotFound();
                }

                var stream = await response.Content.ReadAsStreamAsync();

                return Results.Stream(stream, "application/zip", $"{jobId}.zip");
            }
            catch(Exception exception)
            {
                Console.WriteLine($"[ERROR] [/proxy/archive/download/{jobId}]: Exception message: {exception.Message}");
                return Results.Problem();
            }
        });

        builder.MapGet("/Account/Login", async (HttpContext httpContext, string returnUrl = "/") =>
        {
            var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
                    .WithRedirectUri(returnUrl)
                    .Build();

            await httpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        });

        builder.MapGet("/Account/Logout", async (HttpContext httpContext) =>
        {
            var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
                    .WithRedirectUri("/")
                    .Build();

            await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        });

        return builder;
    }
}
