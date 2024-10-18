using frontend.Components;
using frontend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddHttpClient<ImageService>(client =>
{
    var environment = builder.Environment;
    if(environment.IsDevelopment())
    {
        client.BaseAddress = new Uri("https://localhost:8001");
    }
    else
    {
        client.BaseAddress = new Uri("https://10.176.244.112");
    }
});

builder.Services.AddScoped<ImageService>();

var app = builder.Build();

//if(!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    // app.UseHsts(); // TODO: Maybe implement later
//}

// app.UseHttpsRedirection(); // HTTPS redirection handled by Nginx in production, unneccessary in dev

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapGet("/api/images/{id}", async (ImageService imageService, long id) =>
{
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
