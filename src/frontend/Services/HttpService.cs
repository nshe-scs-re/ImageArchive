using frontend.Models;

namespace frontend.Services;

public class HttpService(IHttpClientFactory httpClientFactory)
{
    public HttpClient CreateClient()
    {
        return httpClientFactory.CreateClient("_httpClient_");
    }

    public async Task<HttpResponseMessage> GetImageByIdAsync(long id)
    {
        var httpClient = this.CreateClient();

        return await httpClient.GetAsync($"api/images/{id}");
    }

    public async Task<List<Image>> GetImages(DateTime startDate, DateTime endDate, int pageIndex, int pageSize)
    {
        var httpClient = this.CreateClient();

        var response = await httpClient.GetFromJsonAsync<List<Image>>($"api/images/paginated?startDate={startDate}&endDate={endDate}&pageIndex={pageIndex}&pageSize={pageSize}");

        return response ?? new List<Image>();
    }
}
