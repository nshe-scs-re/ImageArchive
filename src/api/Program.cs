using api;
using api.Data;
using api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
//======================
using Microsoft.AspNetCore.Authentication.JwtBearer;
//======================

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Image Archive API",
        Version = "v1",
        Description = "An ASP.NET Core 8 minimal Web API for managing an image archive."
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSqlServer<ImageDbContext>(builder.Configuration.GetConnectionString("ImageDb"));

builder.Services.AddScoped<ArchiveManager>();

builder.Services.AddScoped<ImageUploadService>();

//builder.Services.AddAntiforgery();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        string[] origins = builder.Environment.IsDevelopment()
        ? new string[] { "http://127.0.0.1", "http://localhost"}
        : new string[] { "http://10.176.244.111"};

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth0:Authority"];
        options.Audience = builder.Configuration["Auth0:Audience"];
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AllowFrontend");

if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

//app.UseAntiforgery();

app.MapEndpoints();

app.Run();

public partial class Program { }