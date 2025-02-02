﻿using frontend.Models;
using Microsoft.AspNetCore.Antiforgery;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Forms;
using System;
namespace frontend.Services; 

public class HttpService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAntiforgery _antiforgery;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpService(IHttpClientFactory httpClientFactory, IAntiforgery antiforgery, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _antiforgery = antiforgery;
        _httpContextAccessor = httpContextAccessor;
    }

    public HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("HttpClient");
        AddAntiForgeryToken(client);
        return client;
    }

    public void AddAntiForgeryToken(HttpClient client)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if(httpContext is not null)
        {
            var tokens = _antiforgery.GetAndStoreTokens(httpContext);
            if (tokens.RequestToken is not null && tokens.HeaderName is not null)
            {
                client.DefaultRequestHeaders.Add(tokens.HeaderName, tokens.RequestToken);
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

    public async Task<HttpResponseMessage> UploadImageAsync(string url, List<IBrowserFile> files)
    {
        //most commented lines are remains from testing
        //var startDate = DateTime.Now.AddDays(-7);
        //var endDate = DateTime.Now;

        //This is practice with a random request object
        //UploadPractice request = new()
        //{
        //    Name = "blah",
        //    dateTime = startDate,
        //    Description = "This is blah"

        //};

        var httpClient = CreateClient();

        using var content = new MultipartFormDataContent();
        foreach(var file in files)
        {
            var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)); // 10 MB max file size
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "files", file.Name);
        }

        var response = await httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        return response;
    }


}