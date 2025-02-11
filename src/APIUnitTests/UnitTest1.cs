using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using api.Data;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using static System.Net.Mime.MediaTypeNames;

namespace APIUnitTests
{
    public class ApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApiEndpointTests> _logger;

        public ApiEndpointTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
            _serviceProvider = factory.Services;
            _logger = factory.Services.GetRequiredService<ILogger<ApiEndpointTests>>();
        }

        private async Task<T> GetServiceAsync<T>() where T : notnull
        {
            using var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        private void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
            {
                _logger.LogError(ex, message);
            }
            else
            {
                _logger.LogError(message);
            }

            File.AppendAllText("test-errors.log", $"[{DateTime.UtcNow}] ERROR: {message}\n{ex?.ToString()}\n\n");
        }

        [Fact]
        public async Task Test_DbVerifyEndpoint_ShouldReturnSuccess()
        {
            try
            {
                var response = await _client.GetAsync("/api/db-verify");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("Database connection succeeded.", content);
            }
            catch (Exception ex)
            {
                LogError("Test_DbVerifyEndpoint failed.", ex);
                throw;
            }
        }

        [Fact]
        public async Task Test_ArchiveRequestEndpoint_ShouldReturnAccepted()
        {
            try
            {
                var request = new ArchiveRequest
                {
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow,
                    Status = ArchiveStatus.Pending
                };

                var response = await _client.PostAsJsonAsync("/api/archive/request", request);
                Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
                var returnedRequest = await response.Content.ReadFromJsonAsync<ArchiveRequest>();
                Assert.NotNull(returnedRequest);
                Assert.NotEqual(Guid.Empty, returnedRequest.Id);
            }
            catch (Exception ex)
            {
                LogError("Test_ArchiveRequestEndpoint failed.", ex);
                throw;
            }
        }

        [Fact]
        public async Task Test_ArchiveStatusEndpoint_ShouldReturnJobDetails()
        {
            try
            {
                var archiveManager = await GetServiceAsync<ArchiveManager>();
                var request = new ArchiveRequest
                {
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow,
                    Status = ArchiveStatus.Pending
                };
                archiveManager.ProcessArchiveRequest(request);

                var response = await _client.GetAsync($"/api/archive/status/{request.Id}");
                response.EnsureSuccessStatusCode();
                var returnedRequest = await response.Content.ReadFromJsonAsync<ArchiveRequest>();
                Assert.NotNull(returnedRequest);
                Assert.Equal(request.Id, returnedRequest.Id);
            }
            catch (Exception ex)
            {
                LogError("Test_ArchiveStatusEndpoint failed.", ex);
                throw;
            }
        }

        [Fact]
        public async Task Test_ImageUploadSingleEndpoint_ShouldUploadImage()
        {
            try
            {
                var content = new MultipartFormDataContent();
                var imagePath = "test-image.jpg";
                await File.WriteAllBytesAsync(imagePath, new byte[] { 0xFF, 0xD8, 0xFF }); // Simulate JPEG header
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(imagePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(fileContent, "file", "test-image.jpg");
                content.Add(new StringContent("1"), "camera");

                var response = await _client.PostAsync("/api/upload/single", content);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                Assert.NotNull(result);
                Assert.Contains("Upload successful!", result["Message"]);
                File.Delete(imagePath); // Clean up test file
            }
            catch (Exception ex)
            {
                LogError("Test_ImageUploadSingleEndpoint failed.", ex);
                throw;
            }
        }

        [Fact]
        public async Task Test_GetAllImagesEndpoint_ShouldReturnImagesList()
        {
            try
            {
                var dbContext = await GetServiceAsync<ImageDbContext>();
                dbContext.Images.Add(new Image { Name = "TestImage", DateTime = DateTime.UtcNow });
                await dbContext.SaveChangesAsync();

                var response = await _client.GetAsync("/api/images/all");
                response.EnsureSuccessStatusCode();
                var images = await response.Content.ReadFromJsonAsync<List<Image>>();
                Assert.NotNull(images);
                Assert.NotEmpty(images);
            }
            catch (Exception ex)
            {
                LogError("Test_GetAllImagesEndpoint failed.", ex);
                throw;
            }
        }

        // Additional tests with similar logging mechanism... 

        [Fact]
        public async Task Test_ErrorHandling_ShouldLogErrorsAndReturnProblemDetails()
        {
            try
            {
                var invalidId = 999999;
                var response = await _client.GetAsync($"/api/images/{invalidId}");
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
                Assert.NotNull(problemDetails);
                Assert.Contains("not found", problemDetails.Detail);
            }
            catch (Exception ex)
            {
                LogError("Test_ErrorHandling failed.", ex);
                throw;
            }
        }
    }
}
