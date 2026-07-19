using Amazon.S3.Model;
using CAT.Controllers.DTO.Feeding;
using CAT.EF.DAL;

namespace CAT.Services.Interfaces
{
    public interface IFeedingService
    {
        public Task<List<ComponentDTO>> GetComponents(Guid organizationId);
        public Task<List<Ration>> GetRations(Guid organizationId);
        public Task<(Guid? RationId, string Error)> CreateRation(CreateRationRequestDTO ration);
        public Task<Guid> CreateComponent(CreateComponentDTO component);
        public Task<(bool Success, string ErrorText)> DeleteComponent(Guid componentId);
        public Task UpdateComponent(UpdateComponentDTO component);
        public Task<List<GroupWithStatsDTO>> GetGroupWithStats(Guid organizationId);
        public Task<RationSummaryDTO> GetRationSummaryEnhanced(Guid rationId);
        public Task<List<RationGroupedDTO>> GetRationWithComponents(Guid organizationId);
        public Task UpdateRationFull(Guid rationId, Guid? organizationId, UpdateRationRequestDTO dto);
        public Task CreateRationToGroup(Guid groupId, Guid rationId, string rationType);
        public Task<Guid> AssignRationToGroup(AssignRationToGroupDTO dto);

        public Task<List<GroupWithRationDTO>> GetGroupWithRations(Guid organizationId);
        public Task<List<GroupedFeedingByRationDTO>> GetFeedingDailyStats(Guid organizationId);
        public Task<List<GroupedFeedingRecordDTO>> GetGroupRationStats(Guid organizationId, Guid groupId);
        public Task<List<GroupFeedingRecordCostDTO>> GetGroupRationStatsCost(Guid organizationId, Guid groupId);
        public Task<List<GroupFeedingRecordYearlyCostDTO>> GetGroupRationStatsCostYearly(Guid organizationId, Guid groupId);
        public Task<List<GroupFeedingNutritionDTO>> GetGroupRationNutritionStats(Guid organizationId, Guid groupId);
        public List<GroupFeedingStatsDTO> GetGroupFeedingStats(Guid organizationId);
        public List<GroupFeedingDailyDTO> GetGroupFeedingDailyStats(Guid organizationId, DateOnly date);
        public Guid RecordFeeding(RecordFeedingDTO dto);

        public Task RunDailyFeedingRecordFill(Guid organizationId);

    }
}
