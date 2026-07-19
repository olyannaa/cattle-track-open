using System.Reflection;
using System.Runtime.CompilerServices;
using CAT.Controllers.DTO;

namespace CAT.Services.Interfaces;

public interface IUserActionService
{
    public Task LogUserActionAsync(UserActionDto dto);
}