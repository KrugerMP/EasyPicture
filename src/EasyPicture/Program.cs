using EasyPicture.Modules;
using EasyPicture.Services;
using System.Diagnostics;

// WILL RUN AS A BASIC WEB API CLIENT
#if true

Console.WriteLine($"Application Started at {DateTime.Now}");

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:50112");

// ConfigureAppConfiguration
builder.Host.ConfigureAppConfiguration((context, config) =>
{
  config.SetBasePath(GetBasePath());
  _ = config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
}).UseWindowsService(x =>
{
  x.ServiceName = "Easy Picture";
});

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

// Add my own repository to the service collection
builder.Services.AddScoped<IRuleApiController, RuleApiController>();
builder.Services.AddScoped<IAccessDataFactory, AccessDataFactory>();

builder.Services.AddHostedService<ServiceA>();

// Build the app
var app = builder.Build();

// And always use https
app.UseHttpsRedirection();


// Add 1st enpoint: An example without async
app.MapGet("/api/rule34downloader", async (IRuleApiController ruleApiCont, HttpContext context) =>
{
  // SAMPLE: ?tags=feet nakano_nino
  string queryString = context.Request.QueryString.Value;

  string[] queryStrings = queryString.Split("=")[1].Split("%20");

  do
  {
    await ruleApiCont.GetPostByTagsAsync(queryStrings); 
  } while (ruleApiCont.ContinueDownloadLoop());

  GC.Collect(0, GCCollectionMode.Forced);
  context.Response.StatusCode = 200;
}).WithName("Rule34 Downloaded async calls");



// Start the api website
app.Run();

string GetBasePath()
{
  using var processModule = Process.GetCurrentProcess().MainModule;
  return Path.GetDirectoryName(processModule?.FileName);
}

#endif
