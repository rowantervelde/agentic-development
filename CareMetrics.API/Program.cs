using CareMetrics.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// the data service can optionally load a real Vektis CSV (local path or URL)
// if the configuration section `Vektis:CsvPath` or `Vektis:CsvUrl` is set.  An
// HttpClient is required for URL downloads.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IVektisDataService, VektisDataService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Note: to run against downloaded files you can start the app with
//     dotnet run -- Vektis:CsvPath="Data/vektis"
// or with an environment variable:
//     set Vektis__CsvPath=Data/vektis
// This loads both the postcode3 and gemeente CSV files recursively.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CareMetrics API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
