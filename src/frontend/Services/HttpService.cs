using frontend.Models;
using Microsoft.AspNetCore.Antiforgery;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace frontend.Services;

public class HttpService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAntiforgery _antiforgery;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CookieContainer _cookieContainer;


    public HttpService(IHttpClientFactory httpClientFactory, IAntiforgery antiforgery, IHttpContextAccessor httpContextAccessor, CookieContainer cookieContainer, ILogger<HttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _antiforgery = antiforgery;
        _httpContextAccessor = httpContextAccessor;
        _cookieContainer = cookieContainer;
    }

    public HttpClient CreateForwardClient(Uri? baseAddress = null)
    {
        var client = _httpClientFactory.CreateClient("ForwardingClient");

        if(baseAddress != null)
        {
            client.BaseAddress = baseAddress;
        }
        else if(client.BaseAddress == null)
        {
            client.BaseAddress = new Uri("http://localhost/"); // Set a default base address
        }



        return client;
    }

    public HttpClient CreateProxyClient(Uri? baseAddress = null)
    {
        var client = _httpClientFactory.CreateClient("ProxyClient");

        if(baseAddress != null)
        {
            client.BaseAddress = baseAddress;
        }

        AddAntiForgeryCookie(client);
        AddAntiForgeryToken(client);



        return client;
    }

    public void AddAntiForgeryCookie(HttpClient client)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if(httpContext == null)
        {

            return;
        }

        var antiCookie = httpContext.Request.Cookies
            .FirstOrDefault(c => c.Key.StartsWith(".AspNetCore.Antiforgery.", StringComparison.OrdinalIgnoreCase));

        if(!string.IsNullOrEmpty(antiCookie.Value) && client.BaseAddress != null)
        {
            _cookieContainer.Add(client.BaseAddress, new Cookie(antiCookie.Key, antiCookie.Value));
  
        }
    }

    public void AddAntiForgeryToken(HttpClient client)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if(httpContext == null)
        {
   
            return;
        }

        var tokens = _antiforgery.GetAndStoreTokens(httpContext);

        //if(tokens.RequestToken is null || tokens.HeaderName is null)
        //{
        //    _logger.LogError("[HttpService] [AddAntiForgeryToken]: Antiforgery token fields null.");
        //}

        client.DefaultRequestHeaders.Add(tokens.HeaderName!, tokens.RequestToken);
    }
    public async Task<HttpResponseMessage> GetImagesByIdAsync(long id)
    {
        var httpClient = CreateForwardClient();
        var requestUri = $"api/images/{id}";
        return await httpClient.GetAsync(requestUri);
    }

    public async Task<HttpResponseMessage> GetImagesByPageAsync(DateTime startDate, DateTime endDate, int pageIndex, int pageSize, string? siteName, int? siteNumber, int? cameraPosition)
    {
        var httpClient = CreateForwardClient();
        var requestUri = $"api/images/paginated?filter={startDate},{endDate},{pageIndex},{pageSize},{siteName},{siteNumber},{cameraPosition}";
        return await httpClient.GetAsync(requestUri);
    }

    public async Task<HttpResponseMessage> GetImagesByPageAsync(ImageQuery imageQuery, int pageIndex, int pageSize)
    {
        var httpClient = CreateForwardClient();
        var requestUri = $"api/images/paginated?filter={imageQuery.StartDateTime},{imageQuery.EndDateTime},{pageIndex},{pageSize},{imageQuery.SiteName},{imageQuery.SiteNumber},{imageQuery.CameraPositionNumber}";

        return await httpClient.GetAsync(requestUri);
    }

    public async Task<HttpResponseMessage> GetImagesAllAsync()
    {
        var httpClient = CreateForwardClient();
        var requestUri = "api/images/all";
        return await httpClient.GetAsync(requestUri);
    }

    public async Task<HttpResponseMessage> GetArchiveStatusAsync(Guid jobId)
    {
        var httpClient = CreateForwardClient();
        var requestUri = $"api/archive/status/{jobId}";
        return await httpClient.GetAsync(requestUri);
    }

    public async Task<HttpResponseMessage> GetArchiveDownloadAsync(Guid jobId)
    {
        var httpClient = CreateForwardClient();
        var requestUri = $"api/archive/download/{jobId}";
        return await httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);
    }

    public async Task<HttpResponseMessage> PostArchiveRequestAsync(ArchiveRequest request)
    {
        var httpClient = CreateForwardClient();
        var requestUri = "api/archive/request";
        return await httpClient.PostAsJsonAsync(requestUri, request);
    }

    public async Task<HttpResponseMessage> PostArchiveCancellationAsync(ArchiveRequest request)
    {
        var httpClient = CreateForwardClient();
        return await httpClient.PostAsJsonAsync($"api/archive/cancel/{request.Id}", request);
    }

    public async Task<HttpResponseMessage> PostFileAsync(FileUploadItem item)
    {
        var httpClient = CreateForwardClient();

        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(item.File!.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));

        fileContent.Headers.ContentType = new MediaTypeHeaderValue(item.File.ContentType);

        content.Add(fileContent, "files", item.File.Name);

        if(item.DateTime.HasValue)
        {
            content.Add(new StringContent(item.DateTime.Value.ToString("o")), "DateTime");
        }

        if(!string.IsNullOrEmpty(item.SiteName))
        {
            content.Add(new StringContent(item.SiteName), "SiteName");
        }

        if(item.SiteNumber.HasValue)
        {
            content.Add(new StringContent(item.SiteNumber.Value.ToString(CultureInfo.InvariantCulture)), "SiteNumber");
        }

        if(item.CameraPositionNumber.HasValue)
        {
            content.Add(new StringContent(item.CameraPositionNumber.Value.ToString(CultureInfo.InvariantCulture)), "CameraPositionNumber");
        }

        if(!string.IsNullOrEmpty(item.CameraPositionName))
        {
            content.Add(new StringContent(item.CameraPositionName), "CameraPositionName");
        }

        var requestUri = "api/upload";
        return await httpClient.PostAsync(requestUri, content);
    }

    public async Task<HttpResponseMessage> UploadFilesAsync(List<FileUploadItem> fileItems)
    {
        var httpClient = CreateForwardClient();

        using var content = new MultipartFormDataContent();

        for(int i = 0; i < fileItems.Count; i++)
        {
            var item = fileItems[i];

            var fileContent = new StreamContent(item.File!.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)); // 10 MB max file size

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(item.File.ContentType);

            content.Add(fileContent, "files", item.File.Name);
        }

        var requestUri = "/api/upload";
        return await httpClient.PostAsync(requestUri, content);
    }

    // Add the GetQueryHistoryAsync method
    public async Task<HttpResponseMessage> GetQueryHistoryAsync()
    {
        var httpClient = CreateForwardClient();
        return await httpClient.GetAsync("api/query-history");
    }
    public async Task<HttpResponseMessage> PostQueryHistoryAsync(UserQuery query)
    {
        var httpClient = CreateForwardClient();
        return await httpClient.PostAsJsonAsync("api/log-query", query);
    }
}