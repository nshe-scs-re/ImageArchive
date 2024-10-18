using frontend.Models;

namespace frontend.Services;

public class ImageService(HttpClient httpClient)
{
    public async Task<HttpResponseMessage> GetImageByIdAsync(long id)
    {
        return await httpClient.GetAsync($"/api/images/{id}");
    }

    public async Task<List<Image>> GetImages(DateTime startDate, DateTime endDate, int pageIndex, int pageSize)
    {
        var response = await httpClient.GetFromJsonAsync<List<Image>>($"https://localhost:443/api/images/paginated?startDate={startDate}&endDate={endDate}&pageIndex={pageIndex}&pageSize={pageSize}");

        Console.WriteLine($"DEBUG: ImageService.GetImages(): {response.Count()}");

        return response ?? new List<Image>();
    }

    public string GetImageUri(long Id)
    {
        var imageUri = $"https://localhost:443/api/images/{Id}";

        Console.WriteLine($"DEBUG: ImageService.GetImageUri(): {imageUri}");

        return imageUri;
    }
}
