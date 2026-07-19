using CAT.Controllers.DTO;
using CAT.Controllers.DTO.Medicine;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Logic;
using CAT.Services;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Xml;

namespace CAT.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class DailyActionsController : ControllerBase
    {
        private readonly IAnimalService _animalService;
        private readonly IAuthService _authService;
        private readonly PostgresContext _db;
        private readonly IDailyActionService _daService;
        private readonly IMedicineService _medicineService;

        private readonly IOrganizationService _orgService;

        public DailyActionsController(IAnimalService animalService,
            IAuthService authService, PostgresContext postgresContext,
            IDailyActionService daService, IOrganizationService orgService, IMedicineService medicineService)
        {
            _animalService = animalService;
            _authService = authService;
            _db = postgresContext;
            _daService = daService;
            _orgService = orgService;
            _medicineService = medicineService;
        }
        
        /// <summary>
        /// Информация о списке ежедневных действий для пагинации
        /// </summary>
        /// <param name="type">Тип ежедневного действия</param>
        /// <param name="organizationId">Id организации</param>
        /// <returns></returns>
        [HttpGet, Route("pagination-info")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetPagination([FromQuery] string type, [FromHeader] Guid organizationId)
        {
            var entries = ControllersLogic.IsMobileDevice(Request.Headers.UserAgent) ? 5 : 10;
            var count = _db.GetDailyActions(organizationId, type)?.Count();
            return Ok(new PaginationDTO{Count = count ?? default, EntriesPerPage = entries});
        }
        
        /// <summary>
        /// Возвращение списка ежедневных действий по типу с пагинацией
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetDailyActions([FromHeader] Guid organizationId, [FromQuery] DailyActionsPaginationDTO dto)
        {
            var isMobileDevice = ControllersLogic.IsMobileDevice(Request.Headers.UserAgent);
            var dailyActions = _daService.GetDailyActionsByPage(organizationId, dto.Type, dto.SortInfo, dto.Page, isMobileDevice)?.ToList();
            return Ok(dailyActions);
        }

        /// <summary>
        /// Возвращение списка id ежедневных действий по типу
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("ids")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetDailyActionsIds([FromHeader] Guid organizationId, [FromQuery] DailyActionsDTO dto)
        {
            var ids = _daService.GetDailyActions(organizationId, dto.Type, dto.SortInfo)?
                                .Select(e => e.Id)
                                .ToList();
            return Ok(ids);
        }

        /// <summary>
        /// Возвращает список животных для ЕД, используя фильтры
        /// </summary>
        /// <param name="organizationId">Id организации</param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet, Route("animals")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetAnimalsForDA([FromHeader] Guid organizationId, [FromQuery] DailyAnimalsDTO dto)
        {
            var isMobile = true;
            return Ok(_animalService.GetAnimalsForDA(organizationId, dto, dto.Page, isMobile));
        }

         /// <summary>
        /// Возвращает список id животных для ЕД, используя фильтры
        /// </summary>
        /// <param name="organizationId">Id организации</param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet, Route("animals/ids")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetAnimalsIdsForDA([FromHeader] Guid organizationId, [FromQuery] DailyAnimalsDTO dto)
        {
            var isMobile = true;
            var ids = _animalService.GetAnimalsForDA(organizationId, dto, dto.Page, isMobile)
                                    .Select(e => e.Id)
                                    .ToList();
            return Ok(ids);
        }

        /// <summary>
        /// Информация о списке животных для пагинации
        /// </summary>
        /// <param name="type">Тип животного</param>
        /// <param name="organizationId">Id организации</param>
        /// <returns></returns>
        [HttpGet, Route("animals/pagination-info")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetPagination([FromQuery] DailyAnimalsFilterDTO dto, [FromHeader] Guid organizationId)
        {
            var entries = 5;
            var count = _animalService.GetAnimalsForDA(organizationId, new DailyAnimalsDTO { Filter = dto }).Count();
            return Ok(new PaginationDTO{Count = count, EntriesPerPage = entries});
        }

        /// <summary>
        /// Удаляет ежедневные действия
        /// </summary>
        /// <param name="organizationId">Id организации</param>
        /// <param name="actionIds"></param>
        /// <returns></returns>
        [HttpDelete]
        [OrgValidationTypeFilter(checkOrg: true, checkLocAdmin: true)]
        public IActionResult DeleteDailyAction([FromHeader] Guid organizationId, [FromBody] Guid[] actionIds)
        {
            using (var transaction = _db.Database.BeginTransaction())
            {
                foreach(var actionId in actionIds)
                {
                    if (!_orgService.CheckDailyActionById(organizationId, actionId))
                    {
                        transaction.Rollback();
                        return BadRequest(new ErrorDTO("Одно из ежедневных действий не принадлежит организации пользователя"));
                    }
                    try
                    {
                        _daService.DeleteDailyAction(actionId);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return BadRequest(new ErrorDTO(ex.Message));
                    }
                }
                transaction.Commit();
            }
            return Ok();
        }

        /// <summary>
        /// Удаляет ежедневные действия (исследования)
        /// </summary>
        /// <param name="organizationId">Id организации</param>
        /// <param name="researchIds"></param>
        /// <returns></returns>
        [HttpDelete, Route("researches")]
        [OrgValidationTypeFilter(checkOrg: true, checkLocAdmin: true)]
        public IActionResult DeleteResearch([FromHeader] Guid organizationId, [FromBody] Guid[] researchIds)
        {
            using (var transaction = _db.Database.BeginTransaction())
            {
                foreach(var researchId in researchIds)
                {
                    if (!_orgService.CheckResearchById(organizationId, researchId))
                    {
                        transaction.Rollback();
                        return BadRequest(new ErrorDTO("Одно из ежедневных действий не принадлежит организации пользователя"));
                    }
                    try
                    {
                        _daService.DeleteResearch(researchId);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return BadRequest(new ErrorDTO(ex.Message));
                    }
                }
                transaction.Commit();
            }
            return Ok();
        }

        /// <summary>
        /// Создаёт ежедневные действия
        /// </summary>
        [HttpPost]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult CreateDailyAction(
            [FromHeader] Guid organizationId,
            [FromBody] CreateDailyActionDTO[] dtoArray)
        {
            return CreateDailyActionInternal(organizationId, dtoArray);
        }

        /// <summary>
        /// БЕЗ АВТОРИЗАЦИИ. Создаёт ежедневные действия
        /// </summary>
        [HttpPost("mobile")]
        [AllowAnonymous]
        [OrgValidationTypeFilter(checkOrg: false)]
        public IActionResult CreateDailyActionMobile(
            [FromHeader] Guid organizationId,
            [FromBody] CreateDailyActionDTO[] dtoArray)
        {
            return CreateDailyActionInternal(organizationId, dtoArray);
        }

        /// <summary>
        /// Общая логика создания ежедневных действий
        /// </summary>
        private IActionResult CreateDailyActionInternal(
            Guid organizationId,
            CreateDailyActionDTO[] dtoArray)
        {
            if (dtoArray == null || dtoArray.Length == 0)
                return BadRequest(new ErrorDTO("Не переданы ежедневные действия."));

            var report = new List<DailyActionBatchItemResult>();

            for (var i = 0; i < dtoArray.Length; i++)
            {
                var dto = dtoArray[i];

                if (!_orgService.CheckAnimalById(organizationId, dto.AnimalId))
                {
                    report.Add(DailyActionBatchItemResult.CreateError(
                        i,
                        dto.AnimalId,
                        "Один из животных не принадлежит организации пользователя."));
                    continue;
                }

                if (dto.NewGroupId != null &&
                    !_orgService.CheckGroupById(organizationId, dto.NewGroupId))
                {
                    report.Add(DailyActionBatchItemResult.CreateError(
                        i,
                        dto.AnimalId,
                        "Для одного из животных указана группа, не принадлежащая организации пользователя."));
                    continue;
                }

                if (dto.OldGroupId != null &&
                    !_orgService.CheckGroupById(organizationId, dto.OldGroupId))
                {
                    report.Add(DailyActionBatchItemResult.CreateError(
                        i,
                        dto.AnimalId,
                        "Для одного из животных указана группа, не принадлежащая организации пользователя."));
                    continue;
                }

                using var transaction = _db.Database.BeginTransaction();

                try
                {
                    _daService.CreateDailyAction(organizationId, dto);
                    transaction.Commit();
                    report.Add(DailyActionBatchItemResult.CreateSuccess(i, dto.AnimalId));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    report.Add(DailyActionBatchItemResult.CreateError(i, dto.AnimalId, ex.Message));
                }
            }

            return Ok(new
            {
                Succeeded = report.Count(x => x.Success),
                Failed = report.Count(x => !x.Success),
                Items = report
            });
        }

        private sealed class DailyActionBatchItemResult
        {
            public int Index { get; init; }
            public Guid AnimalId { get; init; }
            public bool Success { get; init; }
            public string? Error { get; init; }

            public static DailyActionBatchItemResult CreateSuccess(int index, Guid animalId)
                => new()
                {
                    Index = index,
                    AnimalId = animalId,
                    Success = true
                };

            public static DailyActionBatchItemResult CreateError(int index, Guid animalId, string error)
                => new()
                {
                    Index = index,
                    AnimalId = animalId,
                    Success = false,
                    Error = error
                };
        }

        /// <summary>
        /// Создаёт ежедневные действия c препаратом
        /// </summary>
        [HttpPost, Route("with-medicine/batch")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult CreateDailyActionsWithMedicineBatch(
            [FromHeader] Guid organizationId,
            [FromBody] CreateDailyActionsWithMedicineBatchDTO dto)
        {
            if (dto.AnimalIds == null || dto.AnimalIds.Count == 0)
                return BadRequest(new ErrorDTO("Не переданы AnimalIds."));

            if (dto.Actions == null || dto.Actions.Count == 0)
                return BadRequest(new ErrorDTO("Не переданы Actions."));

            using (var transaction = _db.Database.BeginTransaction())
            {
                foreach (var animalId in dto.AnimalIds.Distinct())
                {
                    if (!_orgService.CheckAnimalById(organizationId, animalId))
                    {
                        transaction.Rollback();
                        return BadRequest(new ErrorDTO("Одно из животных не принадлежит организации пользователя."));
                    }
                }

                try
                {
                    foreach (var animalId in dto.AnimalIds.Distinct())
                    {
                        foreach (var action in dto.Actions)
                        {
                            _daService.CreateDailyActionWithMedicine(organizationId, animalId, action);
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(new ErrorDTO(ex.Message));
                }
            }

            return Ok();
        }

        /// <summary>
        /// Создает препарат
        /// </summary>
        [HttpPost("medicine")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult CreateMedicine([FromHeader] Guid organizationId, [FromBody] CreateMedicineDTO dto)
        {
            if (dto == null)
                return BadRequest(new ErrorDTO("Тело запроса пустое."));

            dto.OrganizationId = organizationId;

            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var id = _medicineService.CreateMedicine(dto);
                transaction.Commit();
                return Ok(id);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new ErrorDTO(ex.Message));
            }
        }

        /// <summary>
        /// Обновляет препарат 
        /// </summary>
        [HttpPut("medicine")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult UpdateMedicine([FromHeader] Guid organizationId, [FromBody] UpdateMedicineDTO dto)
        {
            if (dto == null)
                return BadRequest(new ErrorDTO("Тело запроса пустое."));

            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var ok = _medicineService.UpdateMedicine(dto);
                transaction.Commit();

                if (!ok)
                    return NotFound(new ErrorDTO("Препарат не найден."));

                return Ok();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new ErrorDTO(ex.Message));
            }
        }

        /// <summary>
        /// Удаляет препарат 
        /// </summary>
        [HttpDelete("medicine/{medicineId:guid}")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult DeleteMedicine([FromHeader] Guid organizationId, [FromRoute] Guid medicineId)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var ok = _medicineService.DeleteMedicine(medicineId);
                transaction.Commit();

                if (!ok)
                    return NotFound(new ErrorDTO("Препарат не найден."));

                return Ok();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new ErrorDTO(ex.Message));
            }
        }

        /// <summary>
        /// Возвращает список препаратов
        /// </summary>
        [HttpGet("medicine")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetMedicinesByOrganization([FromHeader] Guid organizationId)
        {
            try
            {
                var medicines = _medicineService.GetMedicinesByOrganization(organizationId).ToList();
                return Ok(medicines);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorDTO(ex.Message));
            }
        }

        /// <summary>
        /// Возвращает список препаратов для 
        /// </summary>
        [HttpGet("short-medicine")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetShortMedicinesByOrganization([FromHeader] Guid organizationId)
        {
            try
            {
                var medicines = _medicineService.GetMedicinesByOrganization(organizationId).Select(
                    x => new ShortMedicineDTO
                    {
                        Name = x.Name,
                        Factory = x.Factory,
                        Substance = x.Substance,
                    }
                    ).ToList();
                return Ok(medicines);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorDTO(ex.Message));
            }
        }
    }
}
