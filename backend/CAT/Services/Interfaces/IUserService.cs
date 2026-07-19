using System.Security.Claims;
using CAT.Controllers.DTO;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace CAT.Services.Interfaces
{
    public interface IUserService
    {
        UserInfoAuthDTO? GetAuthUserInfo(string? login = default, string? hashedPass = default, string? tgId = default);

        UserInfoDTO? GetUserInfo(Guid? userId);

        UserInfoDTO? GetCurrentUserInfo();

        List<Claim>? GetCurrentUserClaims();

        IEnumerable<RoleDTO> GetRoles();

        void RegisterUser(RegisterUserDTO userInfo);

        void ChangeUserRole(Guid userId, Guid roleId);

        void ResetUserPassword(Guid userId, string password);

        bool IsUserRoleExist(Guid roleId);
    }
}
