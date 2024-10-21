using frontend.Components;
using frontend.Services;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment;

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddHttpClient("_httpClient_", httpClient =>
{
    if(environment.IsDevelopment())
    {
        httpClient.BaseAddress = new Uri("http://dev_api:8080/");
    }
    else
    {
        httpClient.BaseAddress = new Uri("https://10.176.244.112/");
    }
});

builder.Services.AddScoped<HttpService>();

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

app.Run();
