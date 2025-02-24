﻿using frontend.Models;
using Microsoft.AspNetCore.Antiforgery;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Globalization;
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

        if (tokens.RequestToken is null || tokens.HeaderName is null)
            {
            Console.WriteLine($"[ERROR] [HttpService] [AddAntiForgeryToken]: Antiforgery token fields null.");
        }

                client.DefaultRequestHeaders.Add(tokens.HeaderName, tokens.RequestToken);

        //Console.WriteLine($"[INFO] [HttpService] [AddAntiForgeryToken]: {nameof(tokens.HeaderName)}: {tokens.HeaderName}");
        //Console.WriteLine($"[INFO] [HttpService] [AddAntiForgeryToken]: {nameof(tokens.RequestToken)}: {tokens.RequestToken}");
            }
        }
    }

    public async Task<HttpResponseMessage> GetImagesByIdAsync(long id)
    {
        var httpClient = CreateClient();
        return await httpClient.GetAsync($"api/images/{id}");
    }

    public async Task<HttpResponseMessage> GetImagesByPageAsync(DateTime startDate, DateTime endDate, int pageIndex, int pageSize, string? siteName, int? siteNumber, int? cameraPosition)
    {
        var httpClient = CreateClient();
        return await httpClient.GetAsync($"api/images/paginated?filter={startDate},{endDate},{pageIndex},{pageSize},{siteName},{siteNumber},{cameraPosition}");
    }

    public async Task<HttpResponseMessage> GetImagesAllAsync()
    {
        var httpClient = CreateClient();
        return await httpClient.GetAsync("api/images/all");
    }

    public async Task<HttpResponseMessage> GetArchiveStatusAsync(Guid jobId)
    {
        var httpClient = CreateClient();
        return await httpClient.GetAsync($"api/archive/status/{jobId}");
    }

    public async Task<HttpResponseMessage> GetArchiveDownloadAsync(Guid jobId)
    {
        var httpClient = CreateClient();
        return await httpClient.GetAsync($"api/archive/download/{jobId}");
    }

    public async Task<HttpResponseMessage> PostArchiveRequestAsync(ArchiveRequest request)
    {
        var httpClient = CreateClient();
        return await httpClient.PostAsJsonAsync("api/archive/request", request);
    }

    public async Task<HttpResponseMessage> UploadImageAsync(List<IBrowserFile> files)
    {
        var httpClient = CreateClient();

        using var content = new MultipartFormDataContent();
        foreach(var file in files)
        {
            var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)); // 10 MB max file size
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "files", file.Name);
        }

        var response = await httpClient.PostAsync("/api/upload/multiple", content);
        response.EnsureSuccessStatusCode();
        return response;
    }


}