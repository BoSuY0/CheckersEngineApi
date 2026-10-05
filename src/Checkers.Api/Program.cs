using System.Text.Json;
using System.Text.Json.Serialization;
using Checkers.Api.ErrorHandling;
using Checkers.Application;
using Checkers.Engine;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCheckersApplication(builder.Configuration);
builder.Services.AddKingsRowEngine(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UnprocessableRequestExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseDefaultFiles();

// Routing must run after the default-file rewrite of "/" to "/index.html", or "/" matches no endpoint.
app.UseRouting();
app.MapStaticAssets();
app.MapControllers();
app.Run();

/// <summary>Entry point; public so that integration tests can host the application.</summary>
public partial class Program;
