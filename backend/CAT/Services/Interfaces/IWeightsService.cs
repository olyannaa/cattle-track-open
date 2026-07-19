using CAT.Controllers.DTO;
using CAT.EF.DAL;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace CAT.Services.Interfaces
{
    public interface IWeightsService
    {
        IEnumerable<WeightInfoDTO> GetWeightsInfo(Guid animalId, WeightsSortInfoDTO? sort = default);

        IEnumerable<WeightInfoDTO> GetWeightsInfoByPage(Guid animalId, WeightsSortInfoDTO? sort = default,
                                    int page = 1, bool isMoblile = default);

        OkDTO InsertWeights(WeightCreateDTO weightInfo);

        double? ComputeSUP(Guid animalId, DateOnly date, double weight);

        WeightStatisticsDTO GetWeightsStatistics(Guid animalId);
    }
}