
using CAT.Controllers.DTO;
using CAT.EF;
using CAT.Filters;
using CAT.Logic;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;

namespace CAT.Services
{
    public class CookiesAuthService : IAuthService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IUserService _userService;
        private readonly PostgresContext _db;
        private readonly IConfiguration _config;
        private readonly UserActionQueue _actionQueue;
        
        public CookiesAuthService(IHttpContextAccessor contextAccessor,
            PostgresContext db, IConfiguration config,
            IUserService userService, UserActionQueue actionQueue)
        {
            _contextAccessor = contextAccessor;
            _userService = userService;
            _db = db;
            _config = config;
            _actionQueue = actionQueue;
        }

        public UserInfoAuthDTO LogIn(string login, string password)
        {
            var userInfo = _userService.GetAuthUserInfo(login: login,
                        hashedPass: ControllersLogic.CalculateSHA256(password));

            if (userInfo is null) throw new NullReferenceException();
            
            AuthCookie(userInfo);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetUserInfo))!;
            if (userInfo.Id != null && userInfo.Id.IsNormalized())
                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    userInfo.Id,
                    "login",
                    dbMethod,
                    null,
                    null,
                    null,
                    "success",
                    userInfo is null ? "Invalid login or password" : null,
                    null,
                    null
                ));
            
            return userInfo;
        }

        public UserInfoAuthDTO? LogInTg(LoginTgDTO tgInfo)
        {
            if (!CheckTgHash(tgInfo))
                throw new AccessViolationException();

            var userInfo = _userService.GetAuthUserInfo(tgId: tgInfo.Id);
            if (userInfo is null) throw new NullReferenceException();

            AuthCookie(userInfo);
            return userInfo;
        }

        public UserInfoAuthDTO? RefreshLoginData()
        {
            var curUserInfo = _userService.GetCurrentUserInfo();

            LogOut();

            if (curUserInfo == null)
                return null;
            var userInfo = new UserInfoAuthDTO(curUserInfo);

            AuthCookie(userInfo);

            return userInfo;
        }

        public async void LogOut()
        {
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "logout",
                null
            ));
            await _contextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public List<Claim> GetUserClaims()
        {
            return _contextAccessor?.HttpContext?.User.Claims.ToList() ?? new List<Claim>();
        }

        public bool CheckLogin(string? login)
        {
            return _db.Users.SingleOrDefault(e => e.Username == login) is not null;
        }

        public bool CheckPhone(string phone)
        {
            return _db.Users.SingleOrDefault(e => e.PhoneNumber == phone) is not null;
        }

        private bool CheckTgHash(LoginTgDTO tgInfo)
        {
            var apiKey = _config.GetValue<string>("TelegramBot:ApiKey");

            if (apiKey == null)
                throw new NotImplementedConfigurationException();

            var dataCheckString = ComputeCheckString(tgInfo);
            var secretKey = EncodeSha256(apiKey);
            var validationKey = EncodeHmac(dataCheckString, secretKey);
            var calculatedHash = BitConverter.ToString(validationKey).Replace("-", "").ToLower();
            
            return calculatedHash == tgInfo.Hash;
        }

        private static byte[] EncodeHmac(string message, byte[] key)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        }

        private static byte[] EncodeSha256(string message)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(message));
        }

        private string ComputeCheckString(LoginTgDTO tgInfo)
        {
            return $"auth_date={tgInfo.AuthDate}\nfirst_name={tgInfo.FirstName}\nid={tgInfo.Id}\nlast_name={tgInfo.LastName}\nphoto_url={tgInfo.PhotoUrl}\nusername={tgInfo.UserName}";
        }

        private void AuthCookie(UserInfoAuthDTO userInfo)
        {
            var claimsPrincipal = GetUserPrincipal(userInfo);
            _contextAccessor.HttpContext!.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal
            ).Wait();
        }

        private ClaimsPrincipal GetUserPrincipal(UserInfoAuthDTO userInfo)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userInfo.Id),
                new Claim("Organization", userInfo.OrganizationId ?? String.Empty),
                new Claim(ClaimTypes.Role, userInfo.RoleId)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }
    }
}
