using NewsSite1.DAL;
using NewsSite1.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<DBServices>();
builder.Services.AddSingleton<NewsApiService>();
builder.Services.AddSingleton<ArticleService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("myProjDB")
        ?? "Data Source=Media.ruppin.ac.il;Initial Catalog=igroup113_test2;User ID=igroup113;Password=igroup113_82421;TrustServerCertificate=True;";
    return new ArticleService(connStr);
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});



var app = builder.Build();

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
