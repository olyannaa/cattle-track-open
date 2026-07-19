using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using CAT.Controllers.DTO;
using CAT.EF;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CAT.Services;

public class UserActionService : IUserActionService
{
    private readonly IDbContextFactory<PostgresContext> _dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserActionService> _logger;

    public UserActionService(
        IDbContextFactory<PostgresContext> dbContextFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserActionService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogUserActionAsync(UserActionDto dto)
    {
        try
        {
            var userIdString = dto.UserId;

            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out var userId))
                throw new InvalidOperationException("UserId is invalid");

            var table = dto.Table;
            if (table == null && dto.DbMethod != null)
            {
                var tableAttr = dto.DbMethod.GetCustomAttribute<TableNameAttribute>();
                table = tableAttr?.TableName;
            }

            var oldJson = dto.OldValues != null ? JsonSerializer.Serialize(dto.OldValues) : null;
            var newJson = dto.NewValues != null ? JsonSerializer.Serialize(dto.NewValues) : null;

            var combinedAdditionalInfo = new
            {
                OriginalInfo = ParseAdditionalInfo(dto.AdditionalInfo),
                Caller = new { Method = dto.CallerMethod }
            };

            var additionalJson = JsonSerializer.Serialize(combinedAdditionalInfo);

            await using var db = _dbContextFactory.CreateDbContext();

            await db.LogUserActionAsync(
                userId.ToString(),
                dto.ActionType,
                table,
                dto.RecordId,
                oldJson,
                newJson,
                null,
                dto.Status,
                dto.ErrorMessage,
                additionalJson
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при логировании действия пользователя: {dto.CallerMethod}");
        }
    }

    private static object? ParseAdditionalInfo(string? additionalInfo)
    {
        if (string.IsNullOrWhiteSpace(additionalInfo))
            return null;

        try
        {
            using var document = JsonDocument.Parse(additionalInfo);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return additionalInfo;
        }
    }
}
