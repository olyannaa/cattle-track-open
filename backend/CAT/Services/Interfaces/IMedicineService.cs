using CAT.Controllers.DTO.Medicine;
using CAT.EF.DAL;

namespace CAT.Services.Interfaces
{
    public interface IMedicineService
    {
        Guid CreateMedicine(CreateMedicineDTO dto);
        bool UpdateMedicine(UpdateMedicineDTO dto);
        bool DeleteMedicine(Guid medicineId);
        IEnumerable<Medicine> GetMedicinesByOrganization(Guid organizationId);
    }
}
