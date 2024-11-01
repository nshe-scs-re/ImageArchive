using Auth0.AspNetCore.Authentication;
using frontend.Components;
using frontend.Models;
using frontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"];
    options.ClientId = builder.Configuration["Auth0:ClientId"];
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddHttpClient("_httpClient_", httpClient =>
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

builder.Services.AddScoped<HttpService>();
builder.Services.AddScoped<TokenProvider>();

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
    //Console.WriteLine($"DEBUG: [Program.cs] [endpoint /api/images/{id}] endpoint hit.");

    try
    {
        var response = await HttpService.GetImageByIdAsync(id);

        if(!response.IsSuccessStatusCode)
        {
            return Results.NotFound();
        }

        var stream = await response.Content.ReadAsStreamAsync();

        return Results.Stream(stream, "image/jpeg");
    }
    catch(Exception ex)
    {
        return Results.Problem($"Error fetching image: {ex.Message}");
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
