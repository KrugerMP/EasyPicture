namespace EasyPicture.Services
{
  public class ServiceA : BackgroundService
  {
    public ServiceA(ILoggerFactory loggerFactory)
    {
      Logger = loggerFactory.CreateLogger<ServiceA>();
    }

    public ILogger Logger { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      Logger.LogInformation("EasyPicture is starting.");

      stoppingToken.Register(() => Logger.LogInformation("EasyPicture is stopping."));

      while (!stoppingToken.IsCancellationRequested)
      {
        Logger.LogInformation("EasyPicture is doing background work.");

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
      }

      Logger.LogInformation("EasyPicture has stopped.");
    }
  }
}