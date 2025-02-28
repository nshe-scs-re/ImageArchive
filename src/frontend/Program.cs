using Auth0.AspNetCore.Authentication;
using frontend;
using frontend.Components;
using frontend.Models;
using frontend.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"];
    options.ClientId = builder.Configuration["Auth0:ClientId"];
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var cookieContainer = new CookieContainer();
builder.Services.AddSingleton(cookieContainer);

builder.Services.AddHttpClient("ForwardingClient", httpClient =>
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

builder.Services.AddHttpClient("ProxyClient", httpClient =>
{
    if(builder.Environment.IsDevelopment())
    {
        httpClient.BaseAddress = new Uri("http://localhost/");
    }
    else
    {
        httpClient.BaseAddress = new Uri("https://10.176.244.111/");
    }
})
.ConfigurePrimaryHttpMessageHandler(() => 
{
    return new HttpClientHandler 
    {
        UseCookies = true,
        CookieContainer = cookieContainer
    };
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

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();

app.MapEndpoints();

app.Run();
