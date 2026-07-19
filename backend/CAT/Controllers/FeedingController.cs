using CAT.Controllers.DTO.Feeding;
using CAT.EF.DAL;
using CAT.Services;
using CAT.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace CAT.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class FeedingController : ControllerBase
    {
        private readonly IFeedingService _feedingService;
        private readonly FeedingExportService _feedingExportService;

        public FeedingController(IFeedingService feedingService, FeedingExportService feedingExportService)
        {
            _feedingService = feedingService;
            _feedingExportService = feedingExportService;
        }

        /// <summary>
        /// Получить все компоненты РАБОТАЕТ
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet, Route("component")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAllComponents([FromHeader] Guid organizationId)
        {
            return Ok(await _feedingService.GetComponents(organizationId));
        }

        /// <summary>
        /// Создание рациона РАБОТАЕТ
        /// </summary>
        [HttpPost, Route("ration")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> CreateRation([FromHeader] Guid organizationId, [FromBody] CreateRationRequestDTO dto)
        {
            dto.OrganizationId = organizationId;
            var (id, error) = await _feedingService.CreateRation(dto);

            if (error != null)
                return BadRequest(new { errorText = error });

            return Ok(new { Message = "Рацион успешно зарегистрирован!" });
        }

        /// <summary>
        /// Получить все рационы организации РАБОТАЕТ
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet, Route("ration")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAllRations([FromHeader] Guid organizationId)
        {
            return Ok(await _feedingService.GetRations(organizationId));
        }
        
        /// <summary>
        /// Удалить компонент по Id РАБОТАЕТ
        /// </summary>
        [HttpDelete, Route("component")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> DeleteComponent(Guid componentId)
        {
            var (success, errorText) = await _feedingService.DeleteComponent(componentId);

            if (!success)
            {
                return NotFound(new { errorText });
            }

            return Ok(new { Message = "Компонент успешно удалён!" });
        }
        
        /// <summary>
        /// Добавить компонент РАБОТАЕТ
        /// </summary>
        [HttpPost, Route("component")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> CreateComponent([FromHeader] Guid organizationId, CreateComponentDTO dto)
        {
            dto.OrganizationId = organizationId;
            var id = await _feedingService.CreateComponent(dto);
            if (id == Guid.Empty) return BadRequest(new { errorText = "Компонент с таким названием уже есть в данной организации" });
            return Ok(new { Message = "Компонент успешно создан!" });
        }

        /// <summary>
        /// редактировать компонент  РАБОТАЕТ
        /// </summary>
        [HttpPatch, Route("component")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> UpdateComponent(UpdateComponentDTO dto)
        {
            await _feedingService.UpdateComponent(dto);
            return Ok(new { Message = "Компонент успешно изменён!" });
        }

        /// <summary>
        /// Получение списка групп с количеством голов и всю инфу о рационе для таблицы и всплываюего окна  РАБОТАЕТ
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet, Route("group-stats")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAllGroupsWithStats([FromHeader] Guid organizationId)
        {
            return Ok(await _feedingService.GetGroupWithStats(organizationId));
        }

        /// <summary>
        /// Получение списка групп с количеством голов, рационом и типом учета РАБОТАЕТ
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet, Route("group-rations")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAllGroupsWithRations([FromHeader] Guid organizationId)
        {
            return Ok(await _feedingService.GetGroupWithRations(organizationId));
        }

        /// <summary>
        /// Получить информацию  о рационе, стоимость рациона на 1 голову и кол-во нутриентов на одну голову НЕ РАБОТАЕТ
        /// </summary>
        /// <param name="rationId">ID рациона</param>
        [HttpGet, Route("ration-summary")]
        [OrgValidationTypeFilter()]
        public async Task<IActionResult> GetRationSummary([FromQuery] Guid rationId)
        {
            var summary = await _feedingService.GetRationSummaryEnhanced(rationId);
            if (summary == null)
                return NotFound("Рацион не найден");

            return Ok(summary);
        }

        /// <summary>
        /// Получить информацию о рационе, его компонентах и стоимости на голову РАБОТАЕТ
        /// </summary>
        [HttpGet, Route("rations-with-components")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetRationWithComponents([FromHeader] Guid organizationId)
        {
            var rations = await _feedingService.GetRationWithComponents(organizationId);
            if (rations == null || !rations.Any())
                return NotFound("Рационы не найдены");

            return Ok(rations);
        }


        /// <summary>
        /// Обновление рациона (с возможной заменой компонентов)
        /// </summary>
        /// <param name="rationId">ID рациона</param>
        /// <param name="dto">Данные для обновления</param>
        [HttpPut("ration/{rationId:guid}")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> UpdateRationFull(
            [FromRoute] Guid rationId, [FromHeader] Guid organizationId, [FromBody] UpdateRationRequestDTO dto)
        {
            await _feedingService.UpdateRationFull(
                rationId,
                organizationId,
                dto
            );

            return Ok();
        }

        /// <summary>
        /// Добавить или обновить рацион группы
        /// </summary>
        [HttpPost("assign-ration-to-group")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> AssignRationToGroup([FromHeader] Guid organizationId, [FromBody] AssignRationToGroupDTO dto)
        {
            dto.OrganizationId = organizationId;
            dto.MorningFeeding = Double.Round(dto.MorningFeeding / 100, 2);
            dto.DayFeeding = Double.Round(dto.DayFeeding / 100, 2);
            dto.NightFeeding = Double.Round(dto.NightFeeding / 100, 2);
            var id = await _feedingService.AssignRationToGroup(dto);
            return Ok(new { AssignmentId = id });
        }


        /// <summary>
        /// Получения инфомрации для графика Анализа кормления за последние 30 дней 
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet, Route("analysis/graph")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetGroupsStats([FromHeader] Guid organizationId)
        {
            return Ok(await _feedingService.GetFeedingDailyStats(organizationId));
        }

        /// <summary>
        /// Скачать таблицу Анализа кормления за последние 30 дней 
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet("analysis/export")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult ExportFeeding([FromHeader] Guid organizationId)
        {
            var fileBytes = _feedingExportService.GenerateFeedingXlsx(organizationId);
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var filename = $"Анализ_кормления_{date}.xlsx";

            return File(fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        filename);
        }


        /// <summary>
        /// Получения инфомрации для графика Анализ кормления по группе за 30 дней 
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet("group-analysis/graph")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetGroupRationGraphData([FromHeader] Guid organizationId, [FromQuery] Guid groupId)
        {
            return Ok((await _feedingService.GetGroupRationStats(organizationId, groupId)).FirstOrDefault());
        }

        /// <summary>
        /// Скачать таблицу Анализ кормления по группе за 30 дней
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet("group-analysis/export")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult ExportGroupRationFeeding([FromHeader] Guid organizationId, Guid groupId)
        {
            var fileBytes = _feedingExportService.GenerateGroupRationFeedingXlsx(organizationId, groupId);

            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var groupName = _feedingService.GetGroupRationStats(organizationId, groupId).Result.FirstOrDefault()?.GroupName ?? "Unknown";
            var filename = $"Группа_{groupName}_Анализ_кормления_{date}.xlsx";

            return File(fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        filename);
        }

        /// <summary>
        /// Данные для графика Анализ стоимости по группе за 30 дней
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet("group-ration-cost/graph")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetGroupRationFeedingCost([FromHeader] Guid organizationId, Guid groupId)
        {
            var result = await _feedingService.GetGroupRationStatsCost(organizationId, groupId);
            return Ok(result.FirstOrDefault());
        }

        /// <summary>
        /// Скачать таблицу Анализ кормления по группе за 30 дней
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet("group-ration-cost/export")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult ExportGroupRationFeedingCost([FromHeader] Guid organizationId, Guid groupId)
        {
            var fileBytes = _feedingExportService.GenerateGroupRationFeedingCostXlsx(organizationId, groupId);

            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var groupStats = _feedingService.GetGroupRationStatsCost(organizationId, groupId).Result;
            var groupName = groupStats.FirstOrDefault()?.GroupName ?? "Unknown";
            var filename = $"Группа_{groupName}_Анализ_стоимости_кормления_{date}.xlsx";

            return File(fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        filename);
        }

        /// <summary>
        /// Данные для графика Анализ стоимости по группе за год
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet("group-ration-cost-yearly/graph")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetGroupRationFeedingCostYearly([FromHeader] Guid organizationId, Guid groupId)
        {
            var result = await _feedingService.GetGroupRationStatsCostYearly(organizationId, groupId);
            return Ok(result.FirstOrDefault());
        }

        /// <summary>
        /// Скачать таблицу Анализ стоимости по группе за год
        /// </summary>
        [HttpGet("group-ration-cost-yearly/export")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult ExportGroupRationFeedingCostYearly([FromHeader] Guid organizationId, Guid groupId)
        {
            var fileBytes = _feedingExportService.GenerateGroupRationFeedingCostYearlyXlsx(organizationId, groupId);
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var groupStats = _feedingService.GetGroupRationStatsCost(organizationId, groupId).Result;
            var groupName = groupStats.FirstOrDefault()?.GroupName ?? "Unknown";
            var filename = $"Группа_{groupName}_Анализ_годовой_стоимости_кормления_{date}.xlsx";

            return File(fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        filename);
        }

        /// <summary>
        /// Данные для графика Динамика основных нутриентов за 30 дней
        /// </summary>
        [HttpGet("group-ration-nutrition/graph")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetGroupRationNutritionStats([FromHeader] Guid organizationId, Guid groupId)
        {
            var result = await _feedingService.GetGroupRationNutritionStats(organizationId, groupId);
            return Ok(result.FirstOrDefault());
        }

        /// <summary>
        /// Скачать таблицу Динамика основных нутриентов за 30 дней
        /// </summary>
        [HttpGet("group-ration-nutrition/export")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult ExportGroupRationNutrition([FromHeader] Guid organizationId, Guid groupId)
        {
            var fileBytes = _feedingExportService.GenerateGroupRationNutritionXlsx(organizationId, groupId);
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var groupStats = _feedingService.GetGroupRationStatsCost(organizationId, groupId).Result;
            var groupName = groupStats.FirstOrDefault()?.GroupName ?? "Unknown";
            var filename = $"Группа_{groupName}_Питательные_вещества_{date}.xlsx";

            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var encodedFilename = Uri.EscapeDataString(filename);
            var contentDisposition = $"attachment; filename*=UTF-8''{encodedFilename}";

            Response.Headers["Content-Disposition"] = contentDisposition;

            return File(fileBytes, contentType);
        }


        /// <summary>
        /// Скачать таблицу План кормления 
        /// </summary>
        [HttpGet("main/group-feeding-stats")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult ExportGroupFeedingStats([FromHeader] Guid organizationId, [FromQuery] DateOnly date)
        {
            var rawRecords = _feedingService.GetGroupFeedingDailyStats(organizationId, date);
            var todayPlan = _feedingService.GetGroupFeedingStats(organizationId);
            var todayPlanByGroup = todayPlan.ToDictionary(p => p.GroupId);

            var mapped = rawRecords.Select(r =>
            {
                var hasNoFeeding = r.TotalFactKg == null || (r.FeedingDetails == null || !r.FeedingDetails.Any());

                if (hasNoFeeding && todayPlanByGroup.TryGetValue(r.GroupId, out var fallback))
                {
                    return new GroupFeedingStatsDTO
                    {
                        GroupId = fallback.GroupId,
                        GroupName = fallback.GroupName,
                        AnimalCount = fallback.AnimalCount,
                        GroupRationId = fallback.GroupRationId,
                        GroupRationName = fallback.GroupRationName,
                        MorningFeeding = fallback.MorningFeeding,
                        DayFeeding = fallback.DayFeeding,
                        NightFeeding = fallback.NightFeeding,
                        TotalKg = fallback.TotalKg,
                        TotalKgForGroup = fallback.TotalKgForGroup
                    };
                }

                var feedingDetails = r.FeedingDetails?.ToList() ?? new List<FalbackFeedingDetailDTO>();

                return new GroupFeedingStatsDTO
                {
                    GroupId = r.GroupId,
                    GroupName = r.GroupName,
                    AnimalCount = r.AnimalCount,
                    GroupRationId = feedingDetails.FirstOrDefault()?.RationId,
                    GroupRationName = feedingDetails.FirstOrDefault()?.RationName,
                    MorningFeeding = feedingDetails.FirstOrDefault(f => f.FeedingTime == "morning")?.FeedingCoefficient ?? 0,
                    DayFeeding = feedingDetails.FirstOrDefault(f => f.FeedingTime == "day")?.FeedingCoefficient ?? 0,
                    NightFeeding = feedingDetails.FirstOrDefault(f => f.FeedingTime == "night")?.FeedingCoefficient ?? 0,
                    TotalKg = feedingDetails.Sum(f => f.FactKg),
                    TotalKgForGroup = r.TotalFactKg ?? 0
                };
            }).ToList();

            var fileBytes = _feedingExportService.GenerateGroupFeedingStatsXlsx(mapped);
            var filename = $"План_Кормления_{date:yyyy-MM-dd}.xlsx";

            var contentDisposition = new System.Net.Mime.ContentDisposition
            {
                FileName = $"План_Кормления_{date:yyyy-MM-dd}.xlsx",
                Inline = false
            };

            Response.Headers["Content-Disposition"] =
                $"attachment; filename=\"Plan_Feeding_{date:yyyy-MM-dd}.xlsx\"; filename*=UTF-8''{Uri.EscapeDataString(filename)}";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            );

        }


        /// <summary>
        /// Получить кормление по группам на указанную дату.
        /// Если данных нет, будет возвращён план на сегодня для соответствующих групп.
        /// </summary>
        [HttpGet("main/plan-to-date")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetGroupDailyFeeding(GetFeedingDTO dto)
        {
            var rawRecords = _feedingService.GetGroupFeedingDailyStats(dto.organizationId, dto.DateParsed);
            var todayPlan = _feedingService.GetGroupFeedingStats(dto.organizationId);
            var todayPlanByGroup = todayPlan.ToDictionary(p => p.GroupId);

            var mapped = rawRecords.Select(r =>
            {
                var hasNoFeeding = r.TotalFactKg == null || r.FeedingDetails == null || !r.FeedingDetails.Any();

                if (hasNoFeeding && todayPlanByGroup.TryGetValue(r.GroupId, out var fallback))
                {
                    return new GroupFeedingStatsDTO
                    {
                        GroupId = fallback.GroupId,
                        GroupName = fallback.GroupName,
                        AnimalCount = fallback.AnimalCount,
                        GroupRationId = fallback.GroupRationId,
                        GroupRationName = fallback.GroupRationName,
                        MorningFeeding = fallback.MorningFeeding,
                        DayFeeding = fallback.DayFeeding,
                        NightFeeding = fallback.NightFeeding,
                        TotalKg = fallback.TotalKg,
                        TotalKgForGroup = fallback.TotalKgForGroup,
                        TotalCost = fallback.TotalCost
                    };
                }

                var feedingDetails = r.FeedingDetails?.ToList() ?? new List<FalbackFeedingDetailDTO>();

                return new GroupFeedingStatsDTO
                {
                    GroupId = r.GroupId,
                    GroupName = r.GroupName,
                    AnimalCount = r.AnimalCount,
                    GroupRationId = feedingDetails.FirstOrDefault()?.RationId,
                    GroupRationName = feedingDetails.FirstOrDefault()?.RationName,
                    MorningFeeding = feedingDetails.FirstOrDefault(f => f.FeedingTime == "morning")?.FeedingCoefficient ?? 0,
                    DayFeeding = feedingDetails.FirstOrDefault(f => f.FeedingTime == "day")?.FeedingCoefficient ?? 0,
                    NightFeeding = feedingDetails.FirstOrDefault(f => f.FeedingTime == "night")?.FeedingCoefficient ?? 0,
                    TotalKg = feedingDetails.Sum(f => f.FactKg),
                    TotalKgForGroup = r.TotalFactKg ?? 0,
                    TotalCost = r.TotalFactCost ?? 0
                };
            }).ToList();

            return Ok(mapped.Where(x => x.GroupRationId != null));
        }




        /// <summary>
        /// Сохранить запись о фактическом кормлении
        /// </summary>
        [HttpPost("record-feeding")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult RecordFeeding([FromHeader] Guid organizationId, [FromBody] List<RecordFeedingDTO> dto)
        {
            try
            {
                foreach (var record in dto)
                {
                    record.OrganizationId = organizationId;
                    var id = _feedingService.RecordFeeding(record);
                }
                return Ok(new { Message = "Кормление успешно сохранено" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { errorText = ex.Message });
            }
        }

    }
}
