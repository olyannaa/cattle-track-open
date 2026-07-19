using CAT.Controllers.DTO;
using CAT.EF.DAL;
using CAT.Models;

namespace CAT.Services.Interfaces
{
    public interface IAnimalService
    {
        Guid RegisterAnimal(AnimalRegistrationDTO animal, Guid organizationId);
        void UpdateAnimal(UpdateAnimalDTO updateInfo);
        List<GroupInfoDTO>? GetGroupsInfo(Guid org_id);
        List<IdentificationInfoDTO>? GetIdentificationsFields(Guid org_id);
        public ImportAnimalsInfo ImportAnimalsFromXLSX(List<AnimalInfoDTO> animals, Guid org_id);
        IEnumerable<dynamic> GetAnimalCensus(Guid organisationId, string? animalType = default,
                                                    string? search = default, CensusSortInfoDTO? sortInfo = default);
        IEnumerable<dynamic> GetAnimalCensusByPage(Guid organisationId, string? animalType = default, string? search = default,
                                CensusSortInfoDTO? sortInfo = default, int page = 1, bool isMobile = default);

        IEnumerable<AnimalByOrgAllTypesDto> GetAnimalCensusWithFilters(Guid organizationId, AnimalFiltersDTO? filters = null,
            CensusSortInfoDTO? sortInfo = default);
        int CountAnimalCensusWithFilters(Guid organizationId, AnimalFiltersDTO? filters = null,
            CensusSortInfoDTO? sortInfo = default);
        IEnumerable<dynamic> GetAnimalCensusByPageWithFilters(Guid organizationId, AnimalFiltersDTO? filters = null,
            CensusSortInfoDTO? sortInfo = default, int page = 1, bool isMobile = default);
        
        IEnumerable<ActiveAnimalDAL> GetAnimalsForDA(Guid organizationId, DailyAnimalsDTO filters,
                                    int? page = default, bool isMobile = default);

        AnimalDTO? GetAnimalInfo(Guid organizationId, Guid animalId);
        Dictionary<string, int> GetMainPageInfo(Guid organizationId);
        IEnumerable<CowDTO> GetCows(Guid organizationId);
        IEnumerable<BullDTO> GetBulls(Guid organizationId);
        void InsertInsemination(InseminationDTO dto);
        IReadOnlyList<Guid> InsertInseminations(IEnumerable<InseminationItemDTO> items);
        IEnumerable<CowInseminationDTO> GetPregnanciesForInsert(Guid organizationId);
        IEnumerable<CowInseminationDTO> GetPregnanciesForCalving(Guid organizationId);
        void InsertPregnancy(InsertPregnancyDTO dto);
        Guid InsertCalving(InsertCalvingDTO dto, Guid organizationId);
        IEnumerable<BreedDTO> GetAllBreeds();
        bool RemoveCowFromBarren(Guid animalId);
        IEnumerable<AnimalReproductionDTO> GetAnimalReproductions(Guid organizationId);
        void UpdatePregnancy(UpdatePregnancyDTO dto);
        IEnumerable<AnimalByOrgAllTypesDto> GetAnimalCensusByPageWithIF(IEnumerable<AnimalByOrgAllTypesDto> census);
        string[] GetPlaceOfOrigin(Guid organizationId);
        string[] GetOrigins(Guid organizationId);
    }
}
