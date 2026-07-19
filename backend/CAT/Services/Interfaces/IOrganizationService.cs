using CAT.Controllers.DTO;
using CAT.EF.DAL;
using CAT.Models;

namespace CAT.Services.Interfaces
{
    public interface IOrganizationService
    {
        Guid Create(CreateOrgDTO orgInfo);

        public IEnumerable<UserDTO>? GetEmployees(Guid organizationId, bool isPrivate = default);

        bool IsOrgExist(Guid orgId);

        bool CheckAnimalById(Guid orgId, Guid? animalId);

        bool CheckEmployeeById(Guid orgId, Guid? userId);

        bool CheckGroupById(Guid orgId, Guid? groupId);

        bool CheckDailyActionById(Guid orgId, Guid? actionId);

        bool CheckResearchById(Guid orgId, Guid? researchId);

        bool CheckInseminationById(Guid orgId, Guid? inseminationId);
        List<Guid> GetAll();
        void DeleteEmployee(Guid orgId, Guid? userId);

        void AddEmployee(Guid orgId, Guid userId, Guid RoleId);
    }
}
