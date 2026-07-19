using CAT.Controllers.DTO;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Models;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;

namespace CAT.Services
{
    public class AnimalCardService : IAnimalCardService
    {
        private readonly PostgresContext _db;
        private readonly UserActionQueue _actionQueue;
        private readonly IHttpContextAccessor _hc;
        
        public AnimalCardService(PostgresContext postgresContext, UserActionQueue actionQueue, IHttpContextAccessor hc)
        {
            _db = postgresContext;
            _actionQueue = actionQueue;
            _hc = hc;
        }

        public IEnumerable<ActiveAnimalDAL> GetAcviteAnimals(Guid organizationId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetActiveAnimals))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod
            ));
           return _db.GetActiveAnimals(organizationId);
        }

        public AnimalDetailDAL GetAnimalDetail(Guid animalId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalDetails))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod
            ));
            return _db.GetAnimalDetails(animalId).First();
        }

        public AnimalDetail2Response GetAnimalDetail2(Guid animalId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalDetails2))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod
            ));

            var dal = _db.GetAnimalDetails2(animalId).AsNoTracking().First();

            dal.ParseIdentificationData();

            var response = new AnimalDetail2Response
            {
                Id = dal.Id,
                OrganizationId = dal.OrganizationId,
                TagNumber = dal.TagNumber,
                Type = dal.Type,
                Breed = dal.Breed,
                MotherId = dal.MotherId,
                MotherTagNumber = dal.MotherTagNumber,
                FatherIds = dal.FatherIds,                   
                FatherTagNumbers = dal.FatherTagNumbers,      
                Status = dal.Status,
                GroupId = dal.GroupId,
                GroupName = dal.GroupName,
                Origin = dal.Origin,
                OriginLocation = dal.OriginLocation,
                BirthDate = dal.BirthDate,
                DateOfReceipt = dal.DateOfReceipt,
                DateOfDisposal = dal.DateOfDisposal,
                ReasonOfDisposal = dal.ReasonOfDisposal,
                IdentificationData = dal.IdentificationData
            };

            return response;
        }

        public ChartInfo<DateOnly, string> GetActionChartData(Guid animalId, DateOnly startDate, DateOnly endDate)
        {
            var result = new ChartInfo<DateOnly, string>();
            
            var animalDetails = SafeQuery(
                () => _db.GetAnimalDetails2(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalDetails)),
                animalId);

            var animal = animalDetails.FirstOrDefault();

            if (animal?.BirthDate is { } birthDate)
                result.AddPoint(birthDate, "Рождение");

            var dailyActions = SafeQuery(
                () => _db.GetAnimalAction(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalAction)),
                animalId);

            var weights = SafeQuery(
                () => _db.GetAnimalWeights(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalWeights)),
                animalId);

            var calvings = SafeQuery(
                () => _db.GetAnimalCalvings(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalCalvings)),
                animalId);
            
            var calvingsNotRegist = SafeQuery(
                    () => _db.GetAnimalChildrenFromAnimals(animalId),
                    typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalPregnancy)),
                    animalId);

            calvings.AddRange(calvingsNotRegist);

            var inseminations = SafeQuery(
                () => _db.GetAnimalInseminations(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalInseminations)),
                animalId);

            var researches = SafeQuery(
                () => _db.GetAnimalResearch(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalResearch)),
                animalId);

            foreach (var action in dailyActions)
                result.AddPoint(action.ActionDate, action.ActionType);

            foreach (var w in weights)
                result.AddPoint(w.WeighingDate, "Взвешивание");

            foreach (var c in calvings)
                result.AddPoint(c.CalvingDate, "Отёл");

            foreach (var ins in inseminations)
            {
                var label = animal?.Type == "Бык"
                    ? "Использование в осеменении"
                    : "Осеменение";

                result.AddPoint(ins.Date, label);
            }

            foreach (var r in researches)
            {
                if (r.CollectionDate is { } d)
                    result.AddPoint(d, "Исследование");
            }

            result.XAxisLabel = "Дата события";
            result.YAxisLabel = "Событие";
            result.Title      = "История событий";

            result.Sort();
            result.Points = result.Points
                .Where(p => p.X > startDate && p.X < endDate)
                .ToList();
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalResearch))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod
            ));

            return result;
        }

        private List<T> SafeQuery<T>(Func<IEnumerable<T>> query,
            MethodBase? dbMethod, Guid animalId)
        {
            try
            {
                var result = query()?.ToList();
                return result ?? [];
            }
            catch (Exception ex)
            {
                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod,
                    errorMessage: ex.Message
                ));

                return [];
            }
        }

        public IEnumerable<DailyActionAnimalCardDTO> GetDailyAction(Guid animalId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalAction))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod
           ));

            return _db.GetAnimalAction(animalId).ToList();
        }

        public IEnumerable<AnimalReproductionDAL> GetAnimalCalvings(Guid animalId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalCalvings))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod
           ));

            return _db.GetAnimalCalvings(animalId).ToList();
        }

        public IEnumerable<AnimalInseminationDAL> GetAnimalInseminations(Guid animalId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalInseminations))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod
           ));

            return _db.GetAnimalInseminations(animalId).ToList();
        }

        public IEnumerable<AnimalPregnancyDAL> GetAnimalPregnancies(Guid animalId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalPregnancy))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod
           ));

            return _db.GetAnimalPregnancy(animalId).ToList();
        }

        public IEnumerable<AnimalResearchDAL> GetAnimalResearhces(Guid animalId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalResearch))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod
           ));

            return _db.GetAnimalResearch(animalId).ToList();
        }

        public ChartInfo<DateOnly, float> GetWeightChartData(Guid animalId, DateOnly startDate, DateOnly endDate)
        {
            var result = new ChartInfo<DateOnly, float>();
            var weights = _db.GetAnimalWeights(animalId).ToList();
            foreach (var action in weights)
                result.AddPoint(action.WeighingDate, action.Weight);
            result.XAxisLabel = "Дата";
            result.YAxisLabel = "Вес, кг";
            result.Title = "Изменение веса";
            result.Sort();
            result.Points = result.Points.Where(p => p.X > startDate && p.X < endDate).ToList();
            return result;
        }

        private List<AnimalDetailDAL> GetParents(Guid animalId, string motherName, string fatherName)
        {
            var result = new List<AnimalDetailDAL>();
            var animal = _db.GetAnimalDetails(animalId).First();
            var mother = animal.MotherId != null ? _db.GetAnimalDetails((Guid)animal.MotherId).First() : null;
            var father = animal.FatherId != null ? _db.GetAnimalDetails((Guid)animal.FatherId).First() : null;
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalDetails))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod
           ));
            
            if (mother != null)
                mother.Name = motherName;
            if (father != null)
                father.Name = fatherName;

            result.Add(mother);
            result.Add(father);
            return result.ToList();

        }

        public IEnumerable<AnimalDetail2DAL> GetAnimalParentsDetail(Guid animalId)
        {
            var result = new List<AnimalDetail2DAL>();
            var animal = _db.GetAnimalDetails4(animalId).FirstOrDefault();

            if (animal == null)
                return result;  // если животное не найдено, возвращаем пустой список

            var animalParents = GetParents2(animalId, "Мать", "Отец");
            var motherParents = new List<AnimalDetail2DAL>() { null, null };
            var fatherParentsList = new List<List<AnimalDetail2DAL>>();

            var mother = animalParents.FirstOrDefault();
            if (mother != null)
                motherParents = GetParents2(mother.Id, "Бабушка (мать матери)", "Дедушка (отец матери)");

            var fathers = animalParents.Skip(1).ToList();
            foreach (var father in fathers)
            {
                if (father != null)
                    fatherParentsList.Add(GetParents2(father.Id, $"Бабушка (мать отца)", $"Дедушка (отец отца)"));
                else
                    fatherParentsList.Add(new List<AnimalDetail2DAL>() { null, null });
            }

            result.AddRange(fathers);

            foreach (var fatherParents in fatherParentsList)
            {
                if (fatherParents.Count > 0)
                    result.Add(fatherParents[0]); // мать отца
                if (fatherParents.Count > 1)
                    result.Add(fatherParents[1]); // отец отца
            }

            result.Add(mother);

            if (motherParents.Count > 0)
                result.Add(motherParents[0]); // мать матери
            if (motherParents.Count > 1)
                result.Add(motherParents[1]); // отец матери

            // Убираем повторяющиеся элементы по Id
            var distinctResult = result.DistinctBy(parent => parent?.Id).ToList();

            return distinctResult;
        }


        private List<AnimalDetail2DAL> GetParents2(Guid animalId, string motherName, string fatherName)
        {
            var result = new List<AnimalDetail2DAL>();
            var animal = _db.GetAnimalDetails2(animalId).FirstOrDefault(); // Получаем данные с использованием AnimalDetail2DAL

            if (animal == null)
                return new List<AnimalDetail2DAL>() { null, null }; // Возвращаем список с null значениями, если животное не найдено

            // Мать
            var mother = animal.MotherId != null ? _db.GetAnimalDetails2((Guid)animal.MotherId).FirstOrDefault() : null;
            if (mother != null)
                mother.Name = motherName;

            // Отцы (удаляем дубликаты с использованием Distinct)
            var fathers = new List<AnimalDetail2DAL>();
            if (animal.FatherIds != null && animal.FatherIds.Any())
            {
                foreach (var fatherId in animal.FatherIds.Distinct())  // Добавляем только уникальные fatherIds
                {
                    var father = _db.GetAnimalDetails2(fatherId).FirstOrDefault();
                    if (father != null)
                    {
                        father.Name = fatherName;
                        fathers.Add(father);
                    }
                }
            }

            // Логирование
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalDetails2))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod
            ));

            // Добавляем мать и отцов в результат
            result.Add(mother);
            result.AddRange(fathers);

            return result;
        }
        
        public IEnumerable<AnimalActionDTO> GetAllAnimalActions(Guid animalId)
        {
            var result = new List<AnimalActionDTO>();

            // безопасные запросы к БД
            var animal = SafeQuery(
                    () => _db.GetAnimalDetails2(animalId),
                    typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalDetails2)),
                    animalId)
                .FirstOrDefault();

            // если вообще не нашли животное — дальше нет смысла
            if (animal == null)
                return result;

            var dailyActions = SafeQuery(
                    () => _db.GetAnimalAction(animalId),
                    typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalAction)),
                    animalId);

            var weights = SafeQuery(
                () => _db.GetAnimalWeights(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalWeights)),
                animalId);

            var calvings = SafeQuery(
                () => _db.GetAnimalCalvings(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalCalvings)),
                animalId);

            var calvingsNotRegist = SafeQuery(
                () => _db.GetAnimalChildrenFromAnimals(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalChildrenFromAnimals)),
                animalId);

            calvings.AddRange(calvingsNotRegist);

            var inseminations = SafeQuery(
                () => _db.GetAnimalInseminations(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalInseminations)),
                animalId);

            var pregnancies = SafeQuery(
                () => _db.GetAnimalPregnancy(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalPregnancy)),
                animalId);

            var researches = SafeQuery(
                () => _db.GetAnimalResearch(animalId),
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalResearch)),
                animalId);

            // ежедневные действия
            foreach (var action in dailyActions)
            {
                var animalAction = new AnimalActionDTO
                {
                    ActionId = action.Id,
                    AnimalId = animalId,
                    EventType = action.ActionType,
                    EventDate = action.ActionDate,
                    PerformedBy = action.PerformedBy,
                };

                if (action.ActionType == "Осмотры")
                {
                    animalAction.Fields.Add("Тип осмотра", action.ActionSubtype);
                    animalAction.Fields.Add("Результат", action.Result);
                    animalAction.Fields.Add("Дата следующего осмотра", action.NextActionDate?.ToString());
                }
                else if (action.ActionType == "Вакцинации и обработки")
                {
                    animalAction.Fields.Add("Тип обработки", action.ActionSubtype);
                    animalAction.Fields.Add("Препарат", action.Medicine);
                    animalAction.Fields.Add("Доза", action.Dose);
                    animalAction.Fields.Add("Дата следующей обработки", action.NextActionDate?.ToString());
                }
                else if (action.ActionType == "Лечение")
                {
                    animalAction.Fields.Add("Диагноз", action.ActionSubtype);
                    animalAction.Fields.Add("Препарат", action.Medicine);
                    animalAction.Fields.Add("Доза", action.Dose);
                    animalAction.Fields.Add("Дата следующего осмотра", action.NextActionDate?.ToString());
                }
                else if (action.ActionType == "Перевод")
                {
                    animalAction.Fields.Add("Старая группа", action.OldGroupId?.ToString());
                    animalAction.Fields.Add("Новая группа", action.NewGroupId?.ToString());
                }
                else if (action.ActionType == "Выбытие")
                {
                    animalAction.Fields.Add("Назначение", action.Notes);
                }

                result.Add(animalAction);
            }

            // взвешивания
            foreach (var action in weights)
            {
                var animalAction = new AnimalActionDTO
                {
                    ActionId = action.Id,
                    AnimalId = animalId,
                    EventType = "Взвешивание",
                    EventDate = action.WeighingDate,
                };
                animalAction.Fields.Add("Вес", $"{action.Weight} кг");
                result.Add(animalAction);
            }

            // осеменения
            foreach (var action in inseminations)
            {
                var animalAction = new AnimalActionDTO
                {
                    ActionId = action.Id,
                    AnimalId = animalId,
                    EventType = "Осеменение",
                    EventDate = action.Date,
                };
                animalAction.Fields.Add("Партия спермы", action.SpermBatch);
                animalAction.Fields.Add("Производитель спермы", action.SpermManufacturer);
                animalAction.Fields.Add("Бык", action.BullTagNumber);

                if (action.InseminationType == "Естественное")
                    animalAction.BullId = action.BullId;

                result.Add(animalAction);
            }

            // отёлы (зарегистрированные + незарегистрированные)
            foreach (var action in calvings)
            {
                var animalAction = new AnimalActionDTO
                {
                    ActionId = action.Id ?? Guid.Empty, // нулевой guid, если Id нет
                    AnimalId = animalId,
                    EventType = "Отёл",
                    EventDate = action.CalvingDate,
                };
                animalAction.Fields.Add("Дата отёла", action.CalvingDate.ToString());
                animalAction.Fields.Add("Тип отёла", action.CalvingType);
                animalAction.Fields.Add("Осложнения", action.Complication);
                animalAction.Fields.Add("Ветеринар", action.Veterinarian);
                animalAction.Fields.Add("Лечение", action.Treatments);
                animalAction.Fields.Add("Патология", action.Pathology);
                animalAction.CalfId = action.CalfId;
                animalAction.Fields.Add("Номер телёнка", action.CalfTagNumber);
                result.Add(animalAction);
            }

            // исследования
            foreach (var action in researches)
            {
                var animalAction = new AnimalActionDTO
                {
                    ActionId = action.Id,
                    AnimalId = animalId,
                    EventType = "Исследование",
                    EventDate = action.CollectionDate,
                };
                animalAction.Fields.Add("Название исследования", action.ResearchName);
                animalAction.Fields.Add("Вид материала", action.MaterialType);
                animalAction.Fields.Add("Результат", action.ResearchResult);
                result.Add(animalAction);
            }

            // рождение животного
            var animalBirth = new AnimalActionDTO
            {
                ActionId = Guid.NewGuid(),
                AnimalId = animalId,
                EventType = "Рождение",
                EventDate = animal.BirthDate,
            };
            animalBirth.Fields.Add("Мать", animal.MotherTagNumber);
            animalBirth.Fields.Add("Отец", animal.FatherTagNumbers);
            animalBirth.Fields.Add("Порода", animal.Breed);
            animalBirth.Fields.Add("Происхождение", animal.Origin);
            animalBirth.Fields.Add("Место происхождения", animal.OriginLocation);
            result.Add(animalBirth);

            return result;
        }


        /// <inheritdoc />
        public string? UpdateAnimalCard(UpdateAnimalCardDTO dto)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.UpdateAnimalCard));
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException(), 
                "update",
                dbMethod,
                newValues: dto
           ));

           var currentAnimalType = _db.GetAnimalDetails2(dto.Id).FirstOrDefault()?.Type;
           
           if (currentAnimalType == null && dto.Type == null)
           {
               return "Животное с таким идентификатором не найдено: " + dto.Id;
           }
           if ((currentAnimalType == null && dto.Type == null) || (IsDifferentSex(currentAnimalType, dto.Type)
               && IsHaveReproductiveData(dto.Id)))
           {
               return "Изменение пола невозможно, так как у животного есть репродуктивные данные.";
           }

           if (dto.IdentificationData != null)
               dto.IdentificationData = PrepareIdentificationData(dto.IdentificationData);

           return _db.UpdateAnimalCard(dto);;
        }

        private static bool IsDifferentSex(string currentType, string newType)
        {
            var maleSex = new HashSet<string>{"Бычок", "Бык"};
            var femaleSex = new HashSet<string>{"Корова", "Телка", "Нетель"};
            
            return (maleSex.Contains(currentType) && femaleSex.Contains(newType)) ||
                   (femaleSex.Contains(currentType) && maleSex.Contains(newType));
        }
        
        private bool IsHaveReproductiveData(Guid animalId)
        {
            var calvings = _db.GetAnimalCalvings(animalId).ToList();
            var inseminations = _db.GetAnimalInseminations(animalId).ToList();
            var pregnancies = _db.GetAnimalPregnancy(animalId).ToList();
            return calvings.Count > 0 || inseminations.Count > 0 || pregnancies.Count > 0;
        }
        
        private static Dictionary<string, string> PrepareIdentificationData(
            Dictionary<string, string> raw
        )
        {
            return raw
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value)) 
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!
                );
        }
    }
}
