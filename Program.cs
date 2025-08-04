using Microsoft.AspNetCore.Diagnostics;
using NewsSite.Services;
using NewsSite1.DAL;
using NewsSite1.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddScoped<OpenAiTagService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<DBServices>();
builder.Services.AddSingleton<ImageGenerationService>();
builder.Services.AddSingleton<AdsGenerationService>();
builder.Services.AddSingleton<FirebaseRealtimeService>();



builder.Services.AddSingleton<NewsApiService>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});




var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (errorFeature != null)
        {
            var ex = errorFeature.Error;

            var response = new { error = "Server error", details = ex.Message };
            await context.Response.WriteAsJsonAsync(response);
        }
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(policy =>
       policy.AllowAnyOrigin()
             .AllowAnyHeader()
             .AllowAnyMethod()
   );

app.UseStaticFiles();


app.UseAuthorization();

app.MapControllers();

app.Run();
