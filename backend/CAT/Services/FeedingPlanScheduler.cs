using CAT.Services.Interfaces;

namespace CAT.Services
{
    public class FeedingPlanScheduler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _runTime = TimeSpan.FromHours(21);
        private readonly IHttpContextAccessor _hc;
        private readonly UserActionQueue _actionQueue;

        public FeedingPlanScheduler(IServiceProvider serviceProvider, IHttpContextAccessor hc, UserActionQueue actionQueue)
        {
            _serviceProvider = serviceProvider;
            _hc = hc;
            _actionQueue = actionQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var nextRun = DateTime.UtcNow.Date.Add(_runTime);
                if (now > nextRun)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);

                using var scope = _serviceProvider.CreateScope();

                try
                {
                    var feedingService = scope.ServiceProvider.GetRequiredService<IFeedingService>();
                    var organizationService = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
                    var organizations = organizationService.GetAll();

                    foreach (var orgId in organizations)
                    {
                        await feedingService.RunDailyFeedingRecordFill(orgId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Ошибка при сохранении планов кормления] {ex.Message}");
                }
            }
        }
    }
}

