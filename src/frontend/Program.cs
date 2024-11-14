using Auth0.AspNetCore.Authentication;
using frontend.Components;
using frontend.Models;
using frontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"];
    options.ClientId = builder.Configuration["Auth0:ClientId"];
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddHttpClient("HttpClient", httpClient =>
{
    if(builder.Environment.IsDevelopment())
    {
        httpClient.BaseAddress = new Uri("http://dev_api:8080/");
    }
    else
    {
        httpClient.BaseAddress = new Uri("https://10.176.244.112/");
    }
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<HttpService>();

builder.Services.AddScoped<TokenProvider>();

builder.Services.AddScoped<ThemeService>();

var app = builder.Build();

if(app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts(); // TODO: Investigate HSTS
    //app.UseHttpsRedirection(); // HTTPS redirection handled by Nginx
}

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.UseAuthentication();

app.UseAuthorization();

app.MapGet("/api/images/{id}", async (HttpService HttpService, long id) =>
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
        Console.WriteLine($"ERROR [Program.cs] [/api/images/{id}]: Exception message: {exception.Message}");
        return Results.Problem($"Error fetching image: {exception.Message}");
    }
});

app.MapGet("/api/archive/download/{jobId}", async (HttpService HttpService, Guid jobId) =>
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
        Console.WriteLine($"ERROR [Program.cs] [/api/archive/download/{jobId}]: Exception message: {exception.Message}");
        return Results.Problem(exception.Message);
    }
});

app.MapGet("/Account/Login", async (HttpContext httpContext, string returnUrl = "/") =>
{
    var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(returnUrl)
            .Build();

    await httpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
});

app.MapGet("/Account/Logout", async (HttpContext httpContext) =>
{
    var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri("/")
            .Build();

    await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
});

app.Run();
