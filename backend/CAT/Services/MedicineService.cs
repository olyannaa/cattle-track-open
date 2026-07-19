using CAT.Controllers.DTO;
using CAT.Controllers.DTO.Medicine;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Services.Interfaces;
using System.Security.Claims;

namespace CAT.Services
{
    public class MedicineService : IMedicineService
    {
        private readonly PostgresContext _db;
        private readonly UserActionQueue _actionQueue;
        private readonly IHttpContextAccessor _hc;

        public MedicineService(PostgresContext postgresContext, UserActionQueue actionQueue, IHttpContextAccessor hc)
        {
            _db = postgresContext;
            _actionQueue = actionQueue;
            _hc = hc;
        }

        public Guid CreateMedicine(CreateMedicineDTO dto)
        {
            var id = _db.CreateMedicine(
                organizationId: dto.OrganizationId,
                name: dto.Name,
                substance: dto.Substance,
                drugEliminationPeriod: dto.DrugEliminationPeriod,
                shelfLife: dto.ShelfLife,
                factory: dto.Factory
            );

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "insert",
                dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.CreateMedicine)),
                recordId: id,
                newValues: dto,
                table: "medicine"
            ));

            return id;
        }

        public bool UpdateMedicine(UpdateMedicineDTO dto)
        {
            var ok = _db.UpdateMedicine(
                id: dto.Id,
                name: dto.Name,
                substance: dto.Substance,
                drugEliminationPeriod: dto.DrugEliminationPeriod,
                shelfLife: dto.ShelfLife,
                factory: dto.Factory
            );

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "update",
                dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.UpdateMedicine)),
                recordId: dto.Id,
                newValues: dto,
                status: ok ? "success" : "not_found",
                table: "medicine"
            ));

            return ok;
        }

        public bool DeleteMedicine(Guid medicineId)
        {
            var ok = _db.DeleteMedicine(medicineId);

            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "delete",
                dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteMedicine)),
                recordId: medicineId,
                status: ok ? "success" : "not_found",
                table: "medicine"
            ));

            return ok;
        }

        public IEnumerable<Medicine> GetMedicinesByOrganization(Guid organizationId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetMedicinesByOrganization))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: organizationId
            ));

            return _db.GetMedicinesByOrganization(organizationId).OrderBy(x => x.Name);
        }
    }
}
