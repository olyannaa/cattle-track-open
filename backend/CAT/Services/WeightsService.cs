using System.Security.Claims;
using CAT.Controllers.DTO;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Logic;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CAT.Services
{
    public class WeightsService : IWeightsService
    {
        private readonly PostgresContext _db;
        private readonly UserActionQueue _actionQueue;
        private readonly IHttpContextAccessor _hc;

        public WeightsService(PostgresContext postgresContext, UserActionQueue actionQueue, IHttpContextAccessor hc)
        {
            _db = postgresContext;
            _actionQueue = actionQueue;
            _hc = hc;
        }

        public double? ComputeSUP(Guid animalId, DateOnly date, double weight)
        {
            var lastWeight = _db.GetAnimalWeightInfo(animalId).Skip(1).FirstOrDefault();
            if (lastWeight == null)
                return null;
            var num = Math.Round((weight - lastWeight.Weight ?? weight) / (date.DayNumber - lastWeight?.Date?.DayNumber ?? date.DayNumber), 2);
            return num;
        }

        public IEnumerable<WeightInfoDTO> GetWeightsInfo(Guid animalId, WeightsSortInfoDTO? sort = default)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalWeightInfo))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: animalId
            ));
            
            return WeightInfoDTO.Parse(_db.GetAnimalWeightInfo(animalId, sort));
        }

        public WeightStatisticsDTO GetWeightsStatistics(Guid animalId)
        {
            var weights = _db.GetAnimalWeightInfo(animalId);
            if (!weights.Any())
                return new WeightStatisticsDTO();
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalWeightInfo))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: animalId
            ));
            
            var meanSup = weights.Where(e => e.SUP != null).Count() == 0 ? 0 :
                Math.Round(weights.Where(e => e.SUP != null).Select(e => (double)e.SUP).Sum() / weights.Where(e => e.SUP != null).Count(), 2);

            return new WeightStatisticsDTO()
            {
                DataByAge = weights.GroupBy(e => e.Age)
                                    .Select(g => new AgeNodeDTO { Age = g.Key ?? default, Weight = g.Average(e => e.Weight) ?? default })
                                    .OrderBy(e => e.Age),
                DataByDate = weights.GroupBy(e => e.Date)
                                    .Select(g => new DateNodeDTO { Date = g.Key ?? default, Weight = g.Average(e => e.Weight) ?? default })
                                    .OrderBy(e => e.Date),
                DataBySUP = weights.GroupBy(e => e.Date)
                                    .Select(g => new SUPNodeDTO { SUP = g.Average(e => e.SUP) ?? default, Date = g.Key ?? default })
                                    .OrderBy(e => e.Date),
                MaxSUP = weights.Where(e => e.SUP != null).Select(e => e.SUP).Max() ?? 0,
                MinSUP = weights.Where(e => e.SUP != null).Select(e => e.SUP).Min() ?? 0,
                MeanSUP = meanSup
            };

        }

        public IEnumerable<WeightInfoDTO> GetWeightsInfoByPage(Guid animalId, WeightsSortInfoDTO? sort = default,
            int page = 1, bool isMoblile = false)
        {
            var (skip, take) = ControllersLogic.ComputePagination(isMoblile, page);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalWeightInfo))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: animalId
            ));
            
            return WeightInfoDTO.Parse(_db.GetAnimalWeightInfo(animalId, sort).Skip(skip).Take(take));
        }

        public OkDTO InsertWeights(WeightCreateDTO weightInfo)
        {
            try
            {
                _db.InsertAnimalWeight(weightInfo);

                var sup = ComputeSUP(weightInfo.AnimalId, weightInfo.Date, weightInfo.Weight ?? 0);
                
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "InsertWeight",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertAnimalWeight)),
                    recordId: weightInfo.AnimalId,
                    newValues: weightInfo,
                    status: "success",
                    additionalInfo: $"Среднесуточный привес = {sup}"
                ));

                return new OkDTO($@"Взвешивание зарегистрировано! Среднесуточный привес: {sup} кг/сут");
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "InsertWeight",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertAnimalWeight)),
                    recordId: weightInfo.AnimalId,
                    newValues: weightInfo,
                    status: "error",
                    errorMessage: ex.Message
                ));

                throw new Exception($"Ошибка при регистрации взвешивания: {ex.Message}");
            }
        }
    }
}