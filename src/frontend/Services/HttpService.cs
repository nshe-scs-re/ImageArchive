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

    public HttpService(IHttpClientFactory httpClientFactory, IAntiforgery antiforgery, IHttpContextAccessor httpContextAccessor, CookieContainer cookieContainer)
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

        //AddAntiForgeryCookie(client);

        //AddAntiForgeryToken(client);

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
            Console.WriteLine($"[ERROR] [HttpService] [AddAntiForgeryCookie]: {nameof(httpContext)} is null.");
            return;
        }

        var antiCookie = httpContext.Request.Cookies
            .FirstOrDefault(c => c.Key.StartsWith(".AspNetCore.Antiforgery."));

        if(!string.IsNullOrEmpty(antiCookie.Value) && client.BaseAddress != null)
        {
            _cookieContainer.Add(client.BaseAddress, new Cookie(antiCookie.Key, antiCookie.Value));
            //Console.WriteLine($"[INFO] [HttpService] [AddAntiForgeryCookie]: Added antiforgery cookie: {antiCookie.Key}");
        }

        //foreach(var cookie in httpContext.Request.Cookies)
        //{
        //    Console.WriteLine($"[INFO] [HttpService] [AddAntiForgeryToken]: Cookie Key: {cookie.Key}");
        //}
    }

    public void AddAntiForgeryToken(HttpClient client)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if(httpContext == null)
        {
            Console.WriteLine($"[ERROR] [HttpService] [AddAntiForgeryToken]: {nameof(httpContext)} is null.");
            return;
        }

        var tokens = _antiforgery.GetAndStoreTokens(httpContext);

        if(tokens.RequestToken is null || tokens.HeaderName is null)
        {
            Console.WriteLine($"[ERROR] [HttpService] [AddAntiForgeryToken]: Antiforgery token fields null.");
        }

        client.DefaultRequestHeaders.Add(tokens.HeaderName, tokens.RequestToken);

        //Console.WriteLine($"[INFO] [HttpService] [AddAntiForgeryToken]: {nameof(tokens.HeaderName)}: {tokens.HeaderName}");
        //Console.WriteLine($"[INFO] [HttpService] [AddAntiForgeryToken]: {nameof(tokens.RequestToken)}: {tokens.RequestToken}");
    }

    public async Task<HttpResponseMessage> GetImagesByIdAsync(long id)
    {
        var httpClient = CreateForwardClient();
        return await httpClient.GetAsync($"api/images/{id}");
    }

    public async Task<HttpResponseMessage> GetImagesByPageAsync(DateTime startDate, DateTime endDate, int pageIndex, int pageSize, string? siteName, int? siteNumber, int? cameraPosition)
    {
        var httpClient = CreateForwardClient();
        return await httpClient.GetAsync($"api/images/paginated?filter={startDate},{endDate},{pageIndex},{pageSize},{siteName},{siteNumber},{cameraPosition}");
    }

    public async Task<HttpResponseMessage> GetImagesByPageAsync(ImageQuery imageQuery, int pageIndex, int pageSize)
    {
        var httpClient = CreateForwardClient();
        return await httpClient.GetAsync($"api/images/paginated?filter={imageQuery.StartDateTime},{imageQuery.EndDateTime},{pageIndex},{pageSize},{imageQuery.SiteName},{imageQuery.SiteNumber},{imageQuery.CameraPositionNumber}");
    }

    public async Task<HttpResponseMessage> GetImagesAllAsync()
    {
        var httpClient = CreateForwardClient();
        return await httpClient.GetAsync("api/images/all");
    }

    public async Task<HttpResponseMessage> GetArchiveStatusAsync(Guid jobId)
    {
        var httpClient = CreateForwardClient();
        return await httpClient.GetAsync($"api/archive/status/{jobId}");
    }

    public async Task<HttpResponseMessage> GetArchiveDownloadAsync(Guid jobId)
    {
        var httpClient = CreateForwardClient();
        return await httpClient.GetAsync($"api/archive/download/{jobId}", HttpCompletionOption.ResponseHeadersRead);
    }

    public async Task<HttpResponseMessage> PostArchiveRequestAsync(ArchiveRequest request)
    {
        var httpClient = CreateForwardClient();
        return await httpClient.PostAsJsonAsync("api/archive/request", request);
    }

    public async Task<HttpResponseMessage> PostFileAsync(FileUploadItem item)
    {
        var httpClient = CreateForwardClient();

        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(item.File.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));

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

        return await httpClient.PostAsync("api/upload", content);
    }

    public async Task<HttpResponseMessage> UploadFilesAsync(List<FileUploadItem> fileItems)
    {
        var httpClient = CreateForwardClient();

        using var content = new MultipartFormDataContent();

        for(int i = 0; i < fileItems.Count; i++)
        {
            var item = fileItems[i];

            var fileContent = new StreamContent(item.File.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)); // 10 MB max file size

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(item.File.ContentType);

            content.Add(fileContent, "files", item.File.Name);

            //content.Add(new StringContent(item.DateTime.Value.ToString("o")), $"FileItems[{i}].DateTime");
            //content.Add(new StringContent(item.SiteName), $"FileItems[{i}].SiteName");
            //content.Add(new StringContent(item.SiteNumber.ToString()), $"FileItems[{i}].SiteNumber");
            //content.Add(new StringContent(item.CameraPositionNumber.ToString()), $"FileItems[{i}].CameraPositionNumber");
            //content.Add(new StringContent(item.CameraPositionName), $"FileItems[{i}].CameraPositionName");
        }

        return await httpClient.PostAsync("/api/upload", content);
    }
}