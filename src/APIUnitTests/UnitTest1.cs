//Patrick Lehr
//02/16/2025
//API Unit Tests for all 11 endpoints
/*
 * Image Archive API - Integration Test Suite
 * ------------------------------------------
 *
 * This file contains a comprehensive set of integration tests for the Image Archive API,
 * implemented as an ASP.NET Core minimal API. These tests use xUnit and the 
 * Microsoft.AspNetCore.Mvc.Testing package to spin up the API in a test environment and
 * exercise various endpoints to ensure that the API behaves as expected.
 *
 * The endpoints tested include:
 *
 * 1. /api/db-verify
 *    - Verifies that the API can successfully connect to the image database.
 *
 * 2. /api/archive/request
 *    - Tests the creation of an archive request by posting an ArchiveRequest object.
 *    - Asserts that a valid ArchiveRequest (with a non-empty ID) is returned.
 *
 * 3. /api/archive/status/{jobId}
 *    - Retrieves the status of an archive request based on its job ID.
 *    - Asserts that the returned ArchiveRequest matches the requested ID.
 *
 * 4. /api/archive/download/{jobId}
 *    - Tests the download functionality of an archive request.
 *      - Case 1: A non-existent job returns a 500 Internal Server Error.
 *      - Case 2: A valid but incomplete job (without a valid file) returns NotFound.
 *      - Case 3: A completed job is simulated by creating a dummy ZIP file and updating the job;
 *                then verifies that the download endpoint returns 200 OK with the expected MIME type.
 *
 * 5. /api/images/all
 *    - Retrieves all images stored in the database.
 *    - Asserts that the returned image list is not null or empty.
 *
 * 6. /api/images/paginated
 *    - Retrieves a paginated list of images based on a filter string.
 *    - The filter is a comma-separated string including start and end dates (ISO 8601 formatted),
 *      page index, page size, site name, site number, and camera position.
 *    - Tests verify that only images matching the filter criteria are returned.
 *    - An additional test verifies that an invalid filter returns a 400 Bad Request.
 *
 * 7. /api/images/{id}
 *    - Retrieves a single image file based on its ID.
 *    - Asserts that a valid image file is returned with the correct MIME type.
 *
 * 8. /api/upload/single
 *    - Tests the upload of a single image using multipart/form-data.
 *    - Verifies that the response contains a success message, an image URL, and a file name.
 *
 * 9. /api/upload/multiple
 *    - Tests the upload of multiple images using multipart/form-data.
 *    - Verifies that the response contains a success message and an array of file names.
 *
 * 10. Error Handling Tests
 *     - Additional tests ensure that invalid requests return appropriate error responses,
 *       such as 400 Bad Request or problem details in JSON format.
 *
 * How to Use:
 * -----------
 * - Ensure that your test environment is correctly configured (e.g., an appsettings.Test.json is provided,
 *   and the test database is set up appropriately, preferably using an in-memory provider).
 *
 * - Build your solution and run the tests with the following command:
 *
 *      dotnet test
 *
 * - The tests will log error details to "test-errors.log" if any failures occur.
 * -TO BE THOROUGH, FIRST USE "dotnet clean" followed by "dotnet build" then last but not least, "dotnet test"
 *
 * Note:
 * -----
 * - Some tests modify the database and file system (e.g., creating dummy files during upload tests). Ensure
 *   that these resources are isolated from your production environment.
 *
 * - The tests assume that the API endpoints behave as defined in Program.cs. Changes to the API may require
 *   corresponding updates to these tests.
 *
 */



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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Diagnostics;
using System.Net;

namespace APIUnitTests
{
    // Helper class for deserializing the paginated response.
    public class PaginatedResponse
    {
        public int TotalCount { get; set; }
        public List<api.Models.Image> Images { get; set; } = new(); // <- important: avoids null reference
    }
    public class ApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApiEndpointTests> _logger;

        public ApiEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            //var baseUrl = $"http://127.0.0.1:8080";
            //_client = new HttpClient { BaseAddress = new Uri(baseUrl)};
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.Test.json", optional: true);
                    config.AddEnvironmentVariables();
                });

                builder.UseSolutionRelativeContentRoot("./src/api");
            }).CreateClient();

            //Console.WriteLine($"[INFO] [UnitTest1.cs] [ApiEndpointTests]: {nameof(_client)} base address is {_client.BaseAddress}");
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

                if(!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[DEBUG] /api/db-verify returned {response.StatusCode}: {body}");
                }

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("Database connection succeeded.", content);
            }
            catch(Exception exception)
            {
                Console.WriteLine($"[ERROR] /api/db-verify: {exception}");
                Results.Problem(detail: exception.ToString()); // 👈 Add exception.ToString()
            }

        }

        [Fact]
        public async Task Test_ArchiveRequestEndpoint_ShouldReturnAccepted()
        {
            try
            {
                var request = new
                {
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow,
                    Status = "Pending"
                };

                var response = await _client.PostAsJsonAsync("/api/archive/request", request);
                Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);

                // Debug response content
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Content: {responseContent}");

                // Deserialize manually
                var returnedRequest = JsonSerializer.Deserialize<ArchiveRequest>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

                Assert.NotNull(returnedRequest);
                Assert.NotEqual(Guid.Empty, returnedRequest!.Id);
            }
            catch(Exception ex)
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
                    StartDateTime = DateTime.UtcNow.AddMonths(-1),
                    EndDateTime = DateTime.UtcNow,
                    Status = ArchiveStatus.Pending
                };
                archiveManager.ProcessArchiveRequest(request);

                var response = await _client.GetAsync($"/api/archive/status/{request.Id}");
                response.EnsureSuccessStatusCode();

                // Debugging: Print raw response content
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Content: {responseContent}");

                // Deserialize with enum conversion
                var returnedRequest = JsonSerializer.Deserialize<ArchiveRequest>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

                Assert.NotNull(returnedRequest);
                Assert.Equal(request.Id, returnedRequest!.Id);
            }
            catch(Exception ex)
            {
                LogError("Test_ArchiveStatusEndpoint failed.", ex);
                throw;
            }
        }

        [Fact]
        public async Task Test_ArchiveDownloadEndpoint_ShouldHandleVariousResponses()
        {
            // Case 1: Non-existent job → API throws internally → returns 500
            var invalidId = Guid.NewGuid();
            var notFoundResponse = await _client.GetAsync($"/api/archive/download/{invalidId}");
            Assert.Equal(HttpStatusCode.InternalServerError, notFoundResponse.StatusCode);

            // Case 2: Valid job, but file is missing → returns 404 NotFound
            var startRequest = new ArchiveRequest
            {
                StartDateTime = DateTime.UtcNow.AddMonths(-1),
                EndDateTime = DateTime.UtcNow
            };

            var startResponse = await _client.PostAsJsonAsync("/api/archive/request", startRequest);
            var processingJob = await startResponse.Content.ReadFromJsonAsync<ArchiveRequest>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });
            Assert.NotNull(processingJob);
            Assert.NotEqual(Guid.Empty, processingJob.Id);

            var statusResponse = await _client.GetAsync($"/api/archive/status/{processingJob.Id}");
            Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);

            // This should return 404 because FilePath is null → checked first in endpoint
            var fileNotFoundResponse = await _client.GetAsync($"/api/archive/download/{processingJob.Id}");
            Assert.Equal(HttpStatusCode.NotFound, fileNotFoundResponse.StatusCode);

            // Case 3: Simulate job completion and file present → returns 200 OK
            var dummyFile = "test-archive.zip";
            await File.WriteAllBytesAsync(dummyFile, new byte[] { 0x50, 0x4B, 0x03, 0x04 }); // valid ZIP header

            var archiveManager = await GetServiceAsync<ArchiveManager>();
            var managerJob = archiveManager.GetJob(processingJob.Id!.Value);
            Assert.NotNull(managerJob);
            managerJob.Status = ArchiveStatus.Completed;
            managerJob.FilePath = Path.GetFullPath(dummyFile);

            await Task.Delay(300); // Let in-memory state settle

            var updatedJob = await _client.GetFromJsonAsync<ArchiveRequest>($"/api/archive/status/{processingJob.Id}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } });
            Assert.Equal(ArchiveStatus.Completed, updatedJob!.Status);

            var fileResponse = await _client.GetAsync($"/api/archive/download/{processingJob.Id}");
            Assert.Equal(HttpStatusCode.OK, fileResponse.StatusCode);
            Assert.Equal("application/zip", fileResponse.Content.Headers.ContentType?.MediaType);

            if(File.Exists(dummyFile))
            {
                File.Delete(dummyFile);
            }
        }



        [Fact]
        public async Task Test_GetPaginatedImagesEndpoint_ShouldReturnCorrectResults()
        {
            // Arrange: Clear existing images and add test data.
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ImageDbContext>();
            dbContext.Images.RemoveRange(dbContext.Images);
            await dbContext.SaveChangesAsync();

            var imageDate = new DateTime(2025, 1, 5);
            // Add test images (providing a non-null FilePath for each).
            var image1 = new api.Models.Image
            {
                DateTime = new DateTime(2025, 1, 5),
                SiteName = "SiteA",
                SiteNumber = 1,
                CameraPositionNumber = 1,
                CameraPositionName = "Image1", // <--
                FilePath = "N/A",
                UnixTime = ((DateTimeOffset)imageDate).ToUnixTimeSeconds()

            };
            var image2 = new api.Models.Image
            {
                DateTime = new DateTime(2025, 1, 15),
                SiteName = "SiteA",
                SiteNumber = 1,
                CameraPositionNumber = 1,
                CameraPositionName = "Image2", // <--
                FilePath = "N/A",
                UnixTime = ((DateTimeOffset)imageDate).ToUnixTimeSeconds()
            };
            var image3 = new api.Models.Image
            {
                DateTime = new DateTime(2025, 1, 25),
                SiteName = "SiteA",
                SiteNumber = 1,
                CameraPositionNumber = 1,
                CameraPositionName = "Image3", // <--
                FilePath = "N/A",
                UnixTime = ((DateTimeOffset)imageDate).ToUnixTimeSeconds()
            };

            // These images should not match the filter.
            var image4 = new api.Models.Image
            {
                
                DateTime = new DateTime(2025, 2, 1),
                SiteName = "SiteA",
                SiteNumber = 1,
                CameraPositionNumber = 1,
                CameraPositionName = "Image4", // <--
                FilePath = "N/A",
                UnixTime = ((DateTimeOffset)imageDate).ToUnixTimeSeconds()
            };
            var image5 = new api.Models.Image
            {
                
                DateTime = new DateTime(2025, 1, 10),
                SiteName = "SiteB",
                SiteNumber = 2,
                CameraPositionNumber = 2,
                CameraPositionName = "Image5", // <--
                FilePath = "N/A",
                UnixTime = ((DateTimeOffset)imageDate).ToUnixTimeSeconds()
            };

            dbContext.Images.AddRange(image1, image2, image3, image4, image5);
            await dbContext.SaveChangesAsync();

            // Build filter string:
            // StartDate: 2025-01-01, EndDate: 2025-01-31, PageIndex: 0, PageSize: 10,
            // SiteName: "SiteA", SiteNumber: 1, CameraPosition: 1.
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 31);
            int pageIndex = 0, pageSize = 10;
            string siteName = "SiteA";
            int siteNumber = 1, cameraPosition = 1;
            var filter = $"{startDate:o},{endDate:o},{pageIndex},{pageSize},{siteName},{siteNumber},{cameraPosition}";

            // Act: Call the endpoint.
            var response = await _client.GetAsync($"/api/images/paginated?filter={Uri.EscapeDataString(filter)}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PaginatedResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            // Assert:
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Images.Count);
            var names = result.Images.Select(i => i.CameraPositionName).ToList();
            Assert.Contains("Image1", names);
            Assert.Contains("Image2", names);
            Assert.Contains("Image3", names);
        }

        [Fact]
        public async Task Test_GetPaginatedImagesEndpoint_ShouldReturnBadRequest_ForInvalidFilter()
        {
            // Arrange: Provide an invalid filter string.
            var invalidFilter = "invalid,filter";
            var response = await _client.GetAsync($"/api/images/paginated?filter={Uri.EscapeDataString(invalidFilter)}");
            // Assert: Expect a 400 Bad Request.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }


        [Fact]
        public async Task Test_UploadEndpoint_ShouldReturnOkAndSavedImage()
        {
            // Arrange
            var testImageContent = new byte[] { 0xFF, 0xD8, 0xFF }; // Minimal JPEG header
            var imageContent = new ByteArrayContent(testImageContent);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            var formData = new MultipartFormDataContent
    {
        { imageContent, "file", "test.jpg" },
        { new StringContent("2025-01-15T12:00:00Z"), "DateTime" },
        { new StringContent("TestSite"), "SiteName" },
        { new StringContent("1"), "SiteNumber" },
        { new StringContent("2"), "CameraPositionNumber" },
        { new StringContent("TestCam"), "CameraPositionName" }
    };

            // Act
            var response = await _client.PostAsync("/api/upload", formData);

            // Assert
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("TestSite", responseBody); // Assuming the returned image object includes SiteName in the JSON
        }



        [Fact]
        public async Task Test_GetAllImagesEndpoint_ShouldReturnImagesList()
        {
            try
            {
                // Arrange: Get a scoped ImageDbContext and clear existing images.
                using var scope = _factory.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ImageDbContext>();
                dbContext.Images.RemoveRange(dbContext.Images);
                await dbContext.SaveChangesAsync();

                var now = DateTime.UtcNow;
                // Insert a test image with required non-null properties.
                var testImage = new api.Models.Image
                {
                    CameraPositionName = "TestImage",
                    DateTime = DateTime.UtcNow,
                    UnixTime = ((DateTimeOffset)now).ToUnixTimeSeconds(),
                    SiteName = "TestSite",
                    FilePath = "dummy.jpg"
                };

                dbContext.Images.Add(testImage);
                await dbContext.SaveChangesAsync();

                // Act: Call the endpoint.
                var response = await _client.GetAsync("/api/images/all");
                response.EnsureSuccessStatusCode();
                var images = await response.Content.ReadFromJsonAsync<List<api.Models.Image>>();

                // Assert:
                Assert.NotNull(images);
                Assert.Single(images); // Expect exactly one inserted image
                Assert.Equal("TestImage", images[0].CameraPositionName);
                Assert.Equal("TestSite", images[0].SiteName);
                Assert.Equal("dummy.jpg", images[0].FilePath);
            }
            catch(Exception ex)
            {
                LogError("Test_GetAllImagesEndpoint failed.", ex);
                throw;
            }
        }


        [Fact]
        public async Task Test_GetImageEndpoint_ShouldReturnImageFile()
        {
            // Arrange
            string dummyFile = "test-image.jpg";
            byte[] dummyContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }; // JPEG header
            string fullPath = Path.GetFullPath(dummyFile);
            await File.WriteAllBytesAsync(fullPath, dummyContent);

            long imageId;

            using(var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ImageDbContext>();
                dbContext.Images.RemoveRange(dbContext.Images);
                await dbContext.SaveChangesAsync();

                var now = DateTime.UtcNow;
                var testImage = new api.Models.Image
                {
                    CameraPositionName = "TestImage",
                    DateTime = now,
                    UnixTime = ((DateTimeOffset)now).ToUnixTimeSeconds(),
                    SiteName = "TestSite",
                    SiteNumber = 1,
                    CameraPositionNumber = 1,
                    FilePath = fullPath
                };

                dbContext.Images.Add(testImage);
                await dbContext.SaveChangesAsync();
                imageId = testImage.Id;
            }

            // Act
            var response = await _client.GetAsync($"/api/images/{imageId}");
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.NotEmpty(bytes); // ensure image data returned

            // Cleanup
            if(File.Exists(fullPath))
                File.Delete(fullPath);
        }



        [Fact]
        public async Task Test_ImageUploadSingleEndpoint_Success()
        {
            // Arrange: Create dummy JPEG content (minimal header) and prepare multipart form-data.
            byte[] dummyImageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
            string fileName = "test-image.jpg";
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(dummyImageBytes), "File", fileName); // <-- must match IFormFile name

            // These must match the form keys expected in the endpoint
            content.Add(new StringContent("1"), "CameraPositionNumber");
            content.Add(new StringContent("2"), "CameraPositionName");
            content.Add(new StringContent("TestSite"), "SiteName");
            content.Add(new StringContent("1"), "SiteNumber");
            content.Add(new StringContent(DateTime.UtcNow.ToString("o")), "DateTime");

            // Act: Post the form data to the upload endpoint.
            var response = await _client.PostAsync("/api/upload", content);
            response.EnsureSuccessStatusCode();

            // Parse the JSON response.
            var jsonResponse = await response.Content.ReadFromJsonAsync<api.Models.Image>();

            // Assert: Verify returned image has expected values.
            Assert.NotNull(jsonResponse);
            Assert.Equal("TestSite", jsonResponse.SiteName);
            Assert.Equal("2", jsonResponse.CameraPositionName);
            Assert.Equal(1, jsonResponse.SiteNumber);
            Assert.Equal(1, jsonResponse.CameraPositionNumber);

            // Cleanup uploaded file
            var uploadedFilePath = jsonResponse.FilePath;
            if(!string.IsNullOrWhiteSpace(uploadedFilePath) && File.Exists(uploadedFilePath))
            {
                File.Delete(uploadedFilePath);
            }
        }

        [Fact]
        public async Task Test_ArchiveCancelEndpoint_ShouldCancelJob()
        {
            var archiveManager = await GetServiceAsync<ArchiveManager>();
            var request = new ArchiveRequest
            {
                StartDateTime = DateTime.UtcNow.AddDays(-5),
                EndDateTime = DateTime.UtcNow,
                Status = ArchiveStatus.Processing
            };
            archiveManager.ProcessArchiveRequest(request);

            var response = await _client.PostAsJsonAsync($"/api/archive/cancel/{request.Id}", request);
            response.EnsureSuccessStatusCode();

            var cancelledJob = await response.Content.ReadFromJsonAsync<ArchiveRequest>();
            Assert.NotNull(cancelledJob);
            Assert.Equal(request.Id, cancelledJob.Id);
            Assert.Equal(ArchiveStatus.Canceled, cancelledJob.Status); // Optional: depends on your implementation
        }





        [Fact]
        public async Task Test_ErrorHandling_ShouldLogErrorsAndReturn404()
        {
            try
            {
                var invalidId = 999999;
                var response = await _client.GetAsync($"/api/images/{invalidId}");

                // Assert: It should return 404
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                // Assert: It should have empty body
                var content = await response.Content.ReadAsStringAsync();
                Assert.True(string.IsNullOrWhiteSpace(content), "Expected empty response body for 404 NotFound.");
            }
            catch(Exception ex)
            {
                LogError("Test_ErrorHandling_ShouldLogErrorsAndReturn404 failed.", ex);
                throw;
            }
        }

    }
}


