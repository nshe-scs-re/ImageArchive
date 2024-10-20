using frontend.Components;
using frontend.Services;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment;

builder.Services.AddHttpClient<ImageService>(httpClient =>
{
    //TODO: Investigate why base address isn't assigned here
    if(environment.IsDevelopment())
    {
        httpClient.BaseAddress = new Uri("http://dev_api:8080");
    }
    else
    {
        httpClient.BaseAddress = new Uri("https://10.176.244.112");
    }
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddScoped<ImageService>();

var app = builder.Build();

//if(!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    // app.UseHsts(); // TODO: Investigate HSTS
//}

//app.UseHttpsRedirection(); // HTTPS redirection handled by Nginx in production, unneccessary in dev

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapGet("/api/images/{id}", async (ImageService imageService, long id) =>
{
    //Console.WriteLine($"DEBUG: [Program.cs] [endpoint /api/images/{id}] endpoint hit.");

    try
    {

        var response = await imageService.GetImageByIdAsync(id);

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
