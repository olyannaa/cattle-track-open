using System.Reflection;
using System.Runtime.CompilerServices;

namespace CAT.Controllers.DTO;

public class UserActionDto
{
    public string UserId { get; set; }
    public string ActionType { get; set; } = null!;
    public MethodBase? DbMethod { get; set; }
    public Guid? RecordId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
    public string Status { get; set; } = "success";
    public string? ErrorMessage { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? Table { get; set; }
    public string? CallerMethod { get; set; }
}


public static class UserActionDtoFactory
{
    public static UserActionDto Create(
        string userId,
        string actionType,
        MethodBase? dbMethod = null,
        Guid? recordId = null,
        object? oldValues = null,
        object? newValues = null,
        string status = "success",
        string? errorMessage = null,
        string? additionalInfo = null,
        string? table = null,
        [CallerMemberName] string? callerMethod = null)
    {
        return new UserActionDto
        {
            ActionType = actionType,
            DbMethod = dbMethod,
            RecordId = recordId,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = userId,
            Status = status,
            ErrorMessage = errorMessage,
            AdditionalInfo = additionalInfo,
            Table = table,
            CallerMethod = callerMethod
        };
    }
}