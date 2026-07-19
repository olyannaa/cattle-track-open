using System.Security.Claims;
using CAT.Controllers.DTO;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Reflection;

namespace CAT.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly PostgresContext _db;
        private readonly UserActionQueue _actionQueue;
        private readonly IHttpContextAccessor _hc;

        public OrganizationService(PostgresContext postgresContext, UserActionQueue actionQueue, IHttpContextAccessor hc)
        {
            _db = postgresContext;
            _actionQueue = actionQueue;
            _hc = hc;
        }

        public IEnumerable<UserDTO>? GetEmployees(Guid organizationId, bool isPrivate = default)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetOrgEmployees))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: organizationId
            ));

            return isPrivate 
                ? _db.GetOrgEmployees(organizationId)?.Select(e => new UserDTO(e.Id, e.Name))
                : _db.GetOrgEmployees(organizationId)?.Select(e => new ManageUserDTO(e.Id, e.Name, e.Username, e.RoleId));
        }

        public bool CheckAnimalById(Guid orgId, Guid? animalId)
        {
            if (animalId == null) return false;

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                table: "animals",
                recordId: animalId
            ));

            return _db.Animals.Where(x => x.Id == animalId).SingleOrDefault()?.OrganizationId == orgId;
        }

        public bool CheckDailyActionById(Guid orgId, Guid? actionId)
        {
            if (actionId == null) return false;

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                table: "daily_actions",
                recordId: actionId
            ));

            return _db.DailyActions.Include(e => e.Animal)
                .Where(x => x.Id == actionId)
                .SingleOrDefault()?
                .Animal?.OrganizationId == orgId;
        }

        public bool CheckResearchById(Guid orgId, Guid? researchId)
        {
            if (researchId == null) return false;

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                table: "research",
                recordId: researchId
            ));

            return _db.Researches
                .Where(x => x.Id == researchId)
                .SingleOrDefault()?
                .OrganizationId == orgId;
        }

        public bool CheckInseminationById(Guid orgId, Guid? inseminationId)
        {
            if (inseminationId == null) return false;

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                table: "insemination",
                recordId: inseminationId
            ));

            return _db.Inseminations
                .Include(x => x.Cow)
                .Where(x => x.Id == inseminationId)
                .SingleOrDefault()?
                .Cow?.OrganizationId == orgId;
        }

        public bool CheckGroupById(Guid orgId, Guid? groupId)
        {
            if (groupId == null) return false;

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                table: "groups",
                recordId: groupId
            ));

            return _db.Groups.Where(x => x.Id == groupId).SingleOrDefault()?.OrganizationId == orgId;
        }

        public List<Guid> GetAll() => _db.Organizations.Select(x => x.Id).ToList();

        public bool CheckEmployeeById(Guid orgId, Guid? userId)
        {
            if (userId == null) return false;

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                table: "users",
                recordId: userId
            ));

            return _db.Users
                .Where(x => x.Id == userId)
                .SingleOrDefault()?
                .OrganizationId == orgId;
        }

        public void DeleteEmployee(Guid orgId, Guid? userId)
        {
            if (userId == null) return;

            var user = _db.Users.Where(x => x.Id == userId).SingleOrDefault();
            if (user == null || user.OrganizationId != orgId) return;

            user.OrganizationId = null;
            user.RoleId = null;
            _db.SaveChanges();

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "delete",
                table: "users",
                recordId: userId
            ));
        }

        public Guid Create(CreateOrgDTO orgInfo)
        {
            var orgId = _db.CreateOrg(orgInfo.Name, orgInfo.Inn, orgInfo.Ogrn);

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "insert",
                table: "organizations",
                recordId: orgId
            ));

            return orgId;
        }

        public void AddEmployee(Guid orgId, Guid userId, Guid roleId)
        {
            _db.UpdateUser(userId, organizationId: orgId, role: roleId);

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "update",
                table: "users",
                recordId: userId
            ));
        }

        public bool IsOrgExist(Guid orgId)
        {
            return _db.Organizations.Where(e => e.Id == orgId).SingleOrDefault() is not null;
        }
    }
}
