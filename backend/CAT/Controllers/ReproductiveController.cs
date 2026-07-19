using CAT.Controllers.DTO;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CAT.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class ReproductiveController : ControllerBase
    {
        private readonly IAnimalService _animalService;
        private readonly IOrganizationService _orgService;

        public ReproductiveController(IAnimalService animalService, IOrganizationService orgService)
        {
            _animalService = animalService;
            _orgService = orgService;
        }

        /// <summary>
        /// Получение списка всех коров организации
        /// </summary>
        /// <param name="organizationId">Идентификатор организации</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("cow")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAllCows([FromHeader] Guid organizationId)
        {
            return Ok(_animalService.GetCows(organizationId));
        }

        /// <summary>
        /// Получение списка всех быков организации
        /// </summary>
        /// <param name="organizationId">Идентификатор организации</param>
        /// <returns>Список быков</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("bull")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAllBulls([FromHeader] Guid organizationId)
        {
            return Ok(_animalService.GetBulls(organizationId));
        }

        /// <summary>
        /// Регистрация Множественного осеменения с автоматическим созданием записи о беременности
        /// </summary>
        /// <param name="dto">Данные осеменения</param>
        /// <returns>Результат операции</returns>
        /// <response code="200">Осеменение успешно зарегистрировано</response>
        /// <response code="400">Неверные данные осеменения</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost, Route("inseminations/batch")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> InsertInseminations(
            [FromBody] InseminationBatchDTO batchDto,
            [FromHeader] Guid organizationId)
        {
            return await InsertInseminationsInternal(batchDto, organizationId);
        }
        
        /// <summary>
        /// БЕЗ АВТОРИЗАЦИИ Регистрация Множественного осеменения с автоматическим созданием записи о беременности
        /// </summary>
        /// <param name="dto">Данные осеменения</param>
        /// <returns>Результат операции</returns>
        /// <response code="200">Осеменение успешно зарегистрировано</response>
        /// <response code="400">Неверные данные осеменения</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost, Route("mobile/inseminations/batch")]
        [AllowAnonymous]
        [OrgValidationTypeFilter(checkOrg: false)]
        public async Task<IActionResult> InsertInseminationsMobile(
            [FromBody] InseminationBatchDTO batchDto,
            [FromHeader] Guid organizationId)
        {
            return await InsertInseminationsInternal(batchDto, organizationId);
        }
        
        [NonAction]
        public async Task<IActionResult> InsertInseminationsInternal(InseminationBatchDTO batchDto, Guid organizationId)
        {
            if (batchDto == null)
                return BadRequest(new ErrorDTO("Не переданы данные осеменения."));

            var validationError = ValidateInseminationItemsOrganization(batchDto.Items, organizationId);
            if (validationError != null)
                return BadRequest(new ErrorDTO(validationError));

            var ids = _animalService.InsertInseminations(batchDto.Items);
            return Ok(new { Message = "Осеменения зарегистрированы транзакционно!", Ids = ids });
        }

        /// <summary>
        /// Регистрация ОДИНОЧНОГО осеменения с автоматическим созданием записи о беременности
        /// </summary>
        /// <param name="dto">Данные осеменения</param>
        /// <returns>Результат операции</returns>
        /// <response code="200">Осеменение успешно зарегистрировано</response>
        /// <response code="400">Неверные данные осеменения</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost, Route("insemination")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> InsertInsemination(
            [FromBody] InseminationItemDTO dto,
            [FromHeader] Guid organizationId)
        {
            return await InsertInseminationInternal(dto, organizationId);
        }
        
        /// <summary>
        /// БЕЗ АВТОРИЗАЦИИ Регистрация ОДИНОЧНОГО осеменения с автоматическим созданием записи о беременности
        /// </summary>
        /// <param name="dto">Данные осеменения</param>
        /// <returns>Результат операции</returns>
        /// <response code="200">Осеменение успешно зарегистрировано</response>
        /// <response code="400">Неверные данные осеменения</response>
        /// <response code="401">Не авторизован</response>
        
        [HttpPost, Route("mobile/insemination")]
        [AllowAnonymous]
        [OrgValidationTypeFilter(checkOrg: false)]
        public async Task<IActionResult> InsertInseminationMobile(
            [FromBody] InseminationItemDTO dto,
            [FromHeader] Guid organizationId)
        {
            return await InsertInseminationInternal(dto, organizationId);
        }
        
        [NonAction]
        public async Task<IActionResult> InsertInseminationInternal(InseminationItemDTO dto, Guid organizationId)
        {
            if (dto == null)
                return BadRequest(new ErrorDTO("Не переданы данные осеменения."));

            var validationError = ValidateInseminationItemsOrganization(new List<InseminationItemDTO> { dto }, organizationId);
            if (validationError != null)
                return BadRequest(new ErrorDTO(validationError));

            var ids = _animalService.InsertInseminations(new List<InseminationItemDTO> { dto });
            return Ok(new { Message = "Осеменение успешно зарегистрировано!", Id = ids.First() });
        }


        /// <summary>
        /// Получение списка стельностей для страницы "Стельность"
        /// </summary>
        /// <param name="organizationId">Идентификатор организации</param>
        /// <returns>Список беременностей</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("pregnancy")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetPregnanciesForInsertPregnancy([FromHeader] Guid organizationId)
        {
            return Ok(_animalService.GetPregnanciesForInsert(organizationId));
        }

        /// <summary>
        /// Получение списка стельностей для страницы "Отёлы"
        /// </summary>
        /// <param name="organizationId">Идентификатор организации</param>
        /// <returns>Список беременностей</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("calving")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetPregnanciesForInsertCalving([FromHeader] Guid organizationId)
        {
            return Ok(_animalService.GetPregnanciesForCalving(organizationId));
        }
        
        

        /// <summary>
        /// Создание записи о беременности (ручной ввод)
        /// </summary>
        /// <param name="dto">Данные беременности</param>
        /// <returns>Результат операции</returns>
        /// <response code="200">Беременность успешно зарегистрирована</response>
        /// <response code="400">Неверные данные беременности</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost, Route("pregnancy")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> InsertPregnancy(
            [FromBody] InsertPregnancyDTO dto,
            [FromHeader] Guid organizationId)
        {
            return await InsertPregnancyInternal(dto, organizationId);
        }
        
        /// <summary>
        /// БЕЗ АВТОРИЗАЦИИ Создание записи о беременности (ручной ввод)
        /// </summary>
        /// <param name="dto">Данные беременности</param>
        /// <returns>Результат операции</returns>
        /// <response code="200">Беременность успешно зарегистрирована</response>
        /// <response code="400">Неверные данные беременности</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost, Route("mobile/pregnancy")]
        [AllowAnonymous]
        [OrgValidationTypeFilter(checkOrg: false)]
        public async Task<IActionResult> InsertPregnancyMobile(
            [FromBody] InsertPregnancyDTO dto,
            [FromHeader] Guid organizationId)
        {
            return await InsertPregnancyInternal(dto, organizationId);
        }
        
        [NonAction]
        public async Task<IActionResult> InsertPregnancyInternal([FromBody] InsertPregnancyDTO dto, Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, dto.CowId))
                return BadRequest(new ErrorDTO("Корова не принадлежит организации пользователя."));

            if (!_orgService.CheckInseminationById(organizationId, dto.InseminationId))
                return BadRequest(new ErrorDTO("Осеменение не принадлежит организации пользователя."));

            dto.ExpectedCalvingDate = dto.Date.AddDays(285);
            _animalService.InsertPregnancy(dto);
            return Ok(new { Message = "Результат проверки сохранён!" });
        }

        /// <summary>
        /// Регистрация нового отёла
        /// </summary>
        /// <param name="dto">Данные отёла</param>
        /// <returns>Результат операции с информацией о матери и дате</returns>
        /// <response code="200">Отёл успешно зарегистрирован</response>
        /// <response code="400">Неверные данные отёла</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost, Route("calving")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> InsertCalving([FromBody] InsertCalvingDTO dto,
            [FromHeader] Guid organizationId)
        {
            return await InsertCalvingInternal(dto, organizationId);
        }
        
        /// <summary>
        /// БЕЗ АВТОРИЗАЦИИ Регистрация нового отёла
        /// </summary>
        /// <param name="dto">Данные отёла</param>
        /// <returns>Результат операции с информацией о матери и дате</returns>
        /// <response code="200">Отёл успешно зарегистрирован</response>
        /// <response code="400">Неверные данные отёла</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost, Route("mobile/calving")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> InsertCalvingMobile([FromBody] InsertCalvingDTO dto,
            [FromHeader] Guid organizationId)
        {
            return await InsertCalvingInternal(dto, organizationId);
        }
        

        [NonAction]
        public async Task<IActionResult> InsertCalvingInternal(InsertCalvingDTO dto,  Guid organizationId)
        {
            var id = _animalService.InsertCalving(dto, organizationId);
            return Ok(new { Message = $"✅ Отёл успешно зарегистрирован!🐮 Мать: {dto.CowTagNumber} 📅 Дата отёла: {dto.Date.ToString()}" });
        }

        [HttpGet, Route("insemination/animals")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAllAnimalsForInsemination([FromHeader] Guid organizationId)
        {
            return Ok(_animalService.GetAnimalReproductions(organizationId));
        }

        [NonAction]
        private string? ValidateInseminationItemsOrganization(IEnumerable<InseminationItemDTO>? items, Guid organizationId)
        {
            if (items == null || !items.Any())
                return "Не переданы данные осеменения.";

            foreach (var item in items)
            {
                var cowIds = GetCowIds(item).Distinct().ToList();
                if (cowIds.Count == 0)
                    return "Не переданы идентификаторы коров (CowIds/CowId).";

                foreach (var cowId in cowIds)
                {
                    if (!_orgService.CheckAnimalById(organizationId, cowId))
                        return "Одна из коров не принадлежит организации пользователя.";
                }

                foreach (var bullId in GetBullIds(item).Distinct())
                {
                    if (!_orgService.CheckAnimalById(organizationId, bullId))
                        return "Один из быков не принадлежит организации пользователя.";
                }
            }

            return null;
        }

        [NonAction]
        private static IEnumerable<Guid> GetCowIds(InseminationItemDTO item)
        {
            if (item.CowIds is { Count: > 0 })
                return item.CowIds;

            return item.CowId.HasValue
                ? new[] { item.CowId.Value }
                : Array.Empty<Guid>();
        }

        [NonAction]
        private static IEnumerable<Guid> GetBullIds(InseminationItemDTO item)
        {
            if (item.BullIds is { Count: > 0 })
                return item.BullIds;

            if (!item.BullJson.HasValue ||
                item.BullJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return Array.Empty<Guid>();

            var ids = new List<Guid>();
            var root = item.BullJson.Value;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(element.GetString(), out var id))
                        ids.Add(id);
                }
            }
            else if (root.ValueKind == JsonValueKind.Object &&
                     root.TryGetProperty("fathers", out var fathers) &&
                     fathers.ValueKind == JsonValueKind.Array)
            {
                foreach (var father in fathers.EnumerateArray())
                {
                    if (father.ValueKind == JsonValueKind.Object &&
                        father.TryGetProperty("id", out var idProp) &&
                        idProp.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(idProp.GetString(), out var id))
                        ids.Add(id);
                }
            }

            return ids;
        }

    }
}
