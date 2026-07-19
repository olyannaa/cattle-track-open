using System.Security.Claims;
using CAT.Controllers.DTO;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Filters;
using CAT.Logic;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace CAT.Services
{
    public class UserService : IUserService
    {
        private readonly PostgresContext _db;
        private readonly IHttpContextAccessor _context;
        private readonly UserActionQueue _actionQueue;
        private readonly IConfiguration _config;

        public UserService(IHttpContextAccessor context, PostgresContext postgresContext, IConfiguration config, UserActionQueue queue)
        {
            _db = postgresContext;
            _context = context;
            _config = config;
            _actionQueue = queue;
        }

        public UserInfoAuthDTO? GetAuthUserInfo(string? login = default, string? hashedPass = default, string? tgId = default)
        {
            var userInfo = _db.GetUserInfo(login: login, phone: login, hashedPass: hashedPass, tgId: tgId)?.Split(", ");

            return userInfo is null ? null : new UserInfoAuthDTO
            {
                Id = userInfo[0],
                OrganizationId = userInfo[1],
                OrganizationName = userInfo[2],
                Name = userInfo[3],
                RoleId = userInfo[4],
                PermissionIds = userInfo[5].Split("; ")
            };
        }

        public void RegisterUser(RegisterUserDTO userInfo)
        {
            var password = ControllersLogic.CalculateSHA256(userInfo.Password);

            try
            {
                var orgAdminId = _config.GetValue<Guid>("Enviroment:OrgAdminId");

                _db.CreateUser(name: userInfo.Name, login: userInfo.Login, password: password,
                    phoneNumber: userInfo.PhoneNumber, tgId: userInfo.TgId,
                    role: userInfo.IsOrgAdmin ? orgAdminId : null);
            }
            catch
            {
                throw new NotImplementedConfigurationException();
            }
        }

        public void ChangeUserRole(Guid userId, Guid roleId)
        {
            _db.UpdateUser(userId, role: roleId);
        }

        public void ResetUserPassword(Guid userId, string password)
        {
            var hashedPassword = ControllersLogic.CalculateSHA256(password);
            _db.UpdateUser(userId, password: hashedPassword);
        }

        public List<Claim>? GetCurrentUserClaims()
        {
            return _context?.HttpContext?.User.Claims.ToList();
        }

        public UserInfoDTO? GetCurrentUserInfo()
        {
            if (Guid.TryParse(GetCurrentUserClaims()?.Find(x => x.Type == ClaimTypes.NameIdentifier)?.Value, out var id))
                return GetUserInfo(id);
            return null;
        }

        public UserInfoDTO? GetUserInfo(Guid? userId)
        { 
            var user = _db.Users
                .Include(e => e.Organization)
                .Include(e => e.Role)
                .SingleOrDefault(e => e.Id == userId);

            if (user == null) return null;

            var userInfo = new UserInfoDTO(user);
            userInfo.Permissions = _db.RolesPermissions.Include(e => e.Permission)
                                                        .Where(e => e.RoleId == user!.RoleId)
                                                        .Select(e => e.Permission.Name)
                                                        .ToArray();

            return userInfo;
        }

        public IEnumerable<RoleDTO> GetRoles()
        {
            return _db.Roles.Select(e => new RoleDTO(e));
        }

        public bool IsUserRoleExist(Guid roleId)
        {
            return _db.Roles.Where(e => e.Id == roleId).SingleOrDefault() is not null;
        }
    }
}
