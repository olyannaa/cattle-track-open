using Microsoft.Extensions.Hosting;
using CAT.Services.Interfaces;
using CAT.Controllers.DTO;

public class UserActionBackgroundService : BackgroundService
{
    private readonly UserActionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserActionBackgroundService> _logger;

    public UserActionBackgroundService(
        UserActionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<UserActionBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var dto in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var userActionService = scope.ServiceProvider.GetRequiredService<IUserActionService>();
                await userActionService.LogUserActionAsync(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при записи лога из очереди");
            }
        }
    }
}