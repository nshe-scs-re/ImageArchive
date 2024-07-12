using ImageProjectFrontend.Models;

namespace ImageProjectFrontend.Services;

public class ImageService(HttpClient httpClient)
{
    public async Task<List<Image>> GetImages(DateTime startDate, DateTime endDate, int pageIndex, int pageSize)
    {
        var response = await httpClient.GetFromJsonAsync<List<Image>>($"https://localhost:7013/api/images/paginated?startDate={startDate}&endDate={endDate}&pageIndex={pageIndex}&pageSize={pageSize}");

        Console.WriteLine("DEBUG: ImageService.GetImages():");
        Console.WriteLine();
        Console.WriteLine(response);

        return response ?? new List<Image>();
    }

    public string GetImageUri(long Id)
    {
        var imageUri = $"https://localhost:7013/api/images/{Id}";

        Console.WriteLine("DEBUG: ImageService.GetImageUri():");
        Console.WriteLine();
        Console.WriteLine(imageUri);

        return imageUri;
    }
}
