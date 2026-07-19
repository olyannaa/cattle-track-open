
using CAT.Controllers.DTO;
using System.Security.Claims;

namespace CAT.Services
{
    public interface IAuthService
    {
        UserInfoAuthDTO LogIn(string username, string password);
        UserInfoAuthDTO LogInTg(LoginTgDTO tgInfo);
        UserInfoAuthDTO? RefreshLoginData();
        void LogOut();
        List<Claim> GetUserClaims();
        bool CheckLogin(string login);
        bool CheckPhone(string phone);
    }
}
