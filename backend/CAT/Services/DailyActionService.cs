using System.Security.Claims;
using System.Text.Json;
using CAT.Controllers.DTO;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Logic;
using CAT.Services.Interfaces;

namespace CAT.Services
{
    public class DailyActionService : IDailyActionService
    {
        private readonly PostgresContext _db;
        private readonly UserActionQueue _actionQueue;
        private readonly IHttpContextAccessor _hc;

        public DailyActionService(PostgresContext postgresContext, UserActionQueue actionQueue, IHttpContextAccessor hc)
        {
            _db = postgresContext;
            _actionQueue = actionQueue;
            _hc = hc;
        }

        public IEnumerable<dynamic> GetDailyActions(Guid organizationId, string type, DailyActionsSortInfoDTO sort)
        {
            var res = _db.GetDailyActions(organizationId, type, sort);
            
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetDailyActions))!
            ));

            return res;
        }

        public IEnumerable<dynamic>? GetDailyActionsByPage(
            Guid organizationId,
            string type,
            DailyActionsSortInfoDTO sort,
            int page = 1,
            bool isMoblile = default)
        {
            var (skip, take) = ControllersLogic.ComputePagination(isMoblile, page);

            var list = _db.GetDailyActionsWithPagination(organizationId, type, sort, skip, take)?.ToList();
            if (list == null) return null;

            if (type != "Исследования")
            {
                var actions = list.Cast<GetActionsDAL>().ToList();
                EnrichProcessingActions(actions, organizationId);
                list = actions.Cast<dynamic>().ToList();
            }

            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetDailyActions))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod
            ));

            return list;
        }

        private void EnrichProcessingActions(List<GetActionsDAL> actions, Guid organizationId)
        {
            var processing = actions
                .Where(a =>
                    string.Equals(a.Type, "Обработка", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.Subtype, "Обработка", StringComparison.OrdinalIgnoreCase))
                .Where(a => !string.IsNullOrWhiteSpace(a.Medicine))
                .ToList();

            var medicineIds = processing
                .Select(a => Guid.TryParse(a.Medicine, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (medicineIds.Count == 0) return;

            var meds = _db.Medicinies
                .Where(m => m.OrganizationId == organizationId && medicineIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Name, m.DrugEliminatior })
                .ToDictionary(x => x.Id, x => x);

            foreach (var a in processing)
            {
                if (!Guid.TryParse(a.Medicine, out var id)) continue;
                if (!meds.TryGetValue(id, out var m)) continue;

                a.MedicineId = id;           
                a.Medicine = m.Name;         
                a.DrugEliminator = m.DrugEliminatior;

                var days = TryExtractFirstInt(m.DrugEliminatior);
                if (days.HasValue && a.Date.HasValue)
                    a.WithdrawalUntil = a.Date.Value.AddDays(days.Value);
            }
        }

        private static int? TryExtractFirstInt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            var digits = new string(s.SkipWhile(c => !char.IsDigit(c))
                                     .TakeWhile(char.IsDigit)
                                     .ToArray());

            return int.TryParse(digits, out var v) ? v : null;
        }



        public void DeleteDailyAction(Guid dailyActionId)
        {
            try
            {
                var dailyAction = _db.DailyActions.FirstOrDefault(d => d.Id == dailyActionId);
                if (dailyAction == null)
                    throw new Exception($"Ежедневное действие с id {dailyActionId} не найдено.");

                var oldValuesJson = JsonSerializer.Serialize(dailyAction);
                
                var result = _db.DeleteDailyAction(dailyActionId);
                if (result == 0)
                    throw new Exception($"Не удалось удалить ежедневное действие с id {dailyActionId}.");
                
                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteDailyAction)),
                    recordId: dailyActionId,
                    oldValues: oldValuesJson,
                    newValues: null,
                    status: "success",
                    table: "daily_actions"
                ));
            }
            catch (Exception ex)
            {
                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteDailyAction)),
                    recordId: dailyActionId,
                    oldValues: null,
                    newValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "daily_actions"
                ));

                throw;
            }
        }


        public void DeleteResearch(Guid researchId)
        {
            try
            {
                var research = _db.Researches.FirstOrDefault(r => r.Id == researchId);

                if (research == null)
                    throw new Exception($"Исследование с id {researchId} не найдено.");

                var oldValuesJson = JsonSerializer.Serialize(research);

                var result = _db.DeleteResearch(researchId);

                if (result == 0)
                    throw new Exception($"Не удалось удалить исследование с id {researchId}.");

                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteResearch)),
                    recordId: researchId,
                    oldValues: oldValuesJson,
                    newValues: null,
                    status: "success",
                    table: "research"
                ));
            }
            catch (Exception ex)
            {
                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteResearch)),
                    recordId: researchId,
                    oldValues: null,
                    newValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "research"
                ));
                
                throw;
            }
        }



        public void CreateDailyAction(Guid organizationId, CreateDailyActionDTO dto)
        {
            var guid = Guid.NewGuid();
            if (dto.Type == "Исследования")
            {
                _db.InsertResearch(guid, organizationId, dto.AnimalId, dto.ResearchName, dto.MaterialType, dto.Date,
                    dto.PerformedBy, dto.Result, dto.Notes);
                
                var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertResearch))!;
                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    "insert",
                    dbMethod,
                    recordId: guid,
                    newValues: dto
                ));
            }
            else if (dto.Type == "Изменение половозрастной группы")
            {
                _db.InsertDailyActionType(guid, dto.AnimalId, dto.Type, dto.Subtype, dto.Date,
                                    dto.PerformedBy, dto.Result, dto.Medicine, dto.Dose,
                                    dto.Notes, dto.NextDate, dto.OldType, dto.NewType);
                
                var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertDailyActionType))!;
                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    "insert",
                    dbMethod,
                    recordId: guid,
                    newValues: dto
                ));
            }
            else
            {
                _db.InsertDailyAction(guid, dto.AnimalId, dto.Type, dto.Subtype, dto.Date,
                                    dto.PerformedBy, dto.Result, dto.Medicine, dto.Dose,
                                    dto.Notes, dto.NextDate, dto.OldGroupId, dto.NewGroupId);
                
                var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertDailyAction))!;
                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    "insert",
                    dbMethod,
                    recordId: guid,
                    newValues: dto
                ));
            }
            
            
            if (dto.Type == "Присвоение номеров")
                _db.UpdateAnimal(dto.AnimalId, identificationFieldName: dto.Subtype,
                    identificationValue: dto.IdentificationValue);

            if (dto.Type == "Перевод")
                _db.UpdateAnimal(dto.AnimalId, groupId: dto.NewGroupId);

            if (dto.Type == "Выбытие")
                _db.UpdateAnimal(dto.AnimalId, status: "Выбывшее", reasonOfDisposal: dto.Subtype);

            if (dto.Type == "Изменение половозрастной группы")
                _db.UpdateAnimal(dto.AnimalId, type: dto.NewType);
        }

        public void CreateDailyActionWithMedicine(Guid organizationId, Guid animalId, DailyActionMedicineItemDTO dto)
        {
            var id = Guid.NewGuid();

            _db.InsertDailyActionWithMedicine(
                id: id,
                animalId: animalId,
                actionType: dto.Type!,
                actionSubtype: dto.Subtype!,
                date: dto.Date,
                performedBy: dto.PerformedBy,
                result: dto.Result,
                medicine: dto.Medicine,
                dose: dto.Dose,
                notes: dto.Notes,
                nextActionDate: dto.NextDate,
                oldGroupId: null,
                newGroupId: null,
                oldType: null,
                newType: null,
                drugEliminationPeriod: dto.DrugEliminationPeriod
            );

            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertDailyActionWithMedicine))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "insert",
                dbMethod,
                recordId: id,
                newValues: new { AnimalId = animalId, Action = dto },
                table: "daily_actions"
            ));
        }
    }
}