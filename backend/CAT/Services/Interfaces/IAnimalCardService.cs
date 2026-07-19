using CAT.Controllers.DTO;
using CAT.EF.DAL;
using CAT.Models;

namespace CAT.Services.Interfaces
{
    public interface IAnimalCardService
    {
        IEnumerable<ActiveAnimalDAL> GetAcviteAnimals(Guid organizationId);
        AnimalDetailDAL GetAnimalDetail(Guid animalId);
        AnimalDetail2Response GetAnimalDetail2(Guid animalId);
        string? UpdateAnimalCard(UpdateAnimalCardDTO dto);
        ChartInfo<DateOnly, string> GetActionChartData(Guid animalId, DateOnly startDate, DateOnly endDate);
        ChartInfo<DateOnly, float> GetWeightChartData(Guid animalId, DateOnly startDate, DateOnly endDate);
        IEnumerable<DailyActionAnimalCardDTO> GetDailyAction(Guid animalId);
        IEnumerable<AnimalReproductionDAL> GetAnimalCalvings(Guid animalId);
        IEnumerable<AnimalInseminationDAL> GetAnimalInseminations(Guid animalId);
        IEnumerable<AnimalPregnancyDAL> GetAnimalPregnancies(Guid animalId);
        IEnumerable<AnimalResearchDAL> GetAnimalResearhces(Guid animalId);
        IEnumerable<AnimalDetail2DAL> GetAnimalParentsDetail(Guid animalId);

        IEnumerable<AnimalActionDTO> GetAllAnimalActions(Guid animalId);
    }
}
