using CAT.Controllers.DTO;

namespace CAT.Services.Interfaces
{
    public interface IDailyActionService
    {
        public IEnumerable<dynamic> GetDailyActions(Guid organizationId, string type, DailyActionsSortInfoDTO sort);
        public IEnumerable<dynamic> GetDailyActionsByPage(Guid organizationId, string type, DailyActionsSortInfoDTO sort,
                                                             int page = 1, bool isMoblile = default);
        public void DeleteDailyAction(Guid dailyActionId);
        public void DeleteResearch(Guid researchId);
        public void CreateDailyAction(Guid organizationId, CreateDailyActionDTO dto);
        public void CreateDailyActionWithMedicine(Guid organizationId, Guid animalId, DailyActionMedicineItemDTO dto);
    }
}