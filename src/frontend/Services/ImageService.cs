using frontend.Models;

namespace frontend.Services;

public class ImageService
{
    private readonly HttpClient _httpClient;

    public ImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        if(_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("http://dev_api:8080");
            //_httpClient.BaseAddress = new Uri("https://dev_api:8081");
        }
    }

    public async Task<HttpResponseMessage> GetImageByIdAsync(long id)
    {
        return await _httpClient.GetAsync($"/api/images/{id}");
    }

    public async Task<List<Image>> GetImages(DateTime startDate, DateTime endDate, int pageIndex, int pageSize)
    {
        var response = await _httpClient.GetFromJsonAsync<List<Image>>($"/api/images/paginated?startDate={startDate}&endDate={endDate}&pageIndex={pageIndex}&pageSize={pageSize}");

        //Console.WriteLine($"DEBUG: ImageService.GetImages(): {response.Count()}");

        return response ?? new List<Image>();
    }

    public string GetImageUri(long Id)
    {
        var imageUri = $"/api/images/{Id}";

        //Console.WriteLine($"DEBUG: ImageService.GetImageUri(): {imageUri}");

        return imageUri;
    }
}
