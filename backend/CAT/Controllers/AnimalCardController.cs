using CAT.Controllers.DTO;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class AnimalCardController : ControllerBase
    {
        private readonly IAnimalCardService _animalCardService;
        private readonly IOrganizationService _orgService;
        public AnimalCardController(IAnimalCardService animalService, IOrganizationService orgService)
        {
            _animalCardService = animalService;
            _orgService = orgService;
        }

        /// <summary>
        /// Получение списка всех активных животных организации
        /// </summary>
        /// <param name="organizationId">Идентификатор организации</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("animal/active")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAllAnimals([FromHeader] Guid organizationId)
        {
            return await GetAllAnimalsInternal(organizationId);
        }
        
        /// <summary>
        /// БЕЗ РЕГИСТРАЦИИ Получение списка всех активных животных организации
        /// </summary>
        /// <param name="organizationId">Идентификатор организации</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        [HttpGet, Route("animal/mobile/active")]
        [OrgValidationTypeFilter(checkOrg: true)]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllAnimalsMobile([FromHeader] Guid organizationId)
        {
            return await GetAllAnimalsInternal(organizationId);
        }
        
        [NonAction]
        private async Task<IActionResult> GetAllAnimalsInternal([FromHeader] Guid organizationId)
        {
            return Ok(_animalCardService.GetAcviteAnimals(organizationId));
        }

        /// <summary>
        /// Получение всей информации о животном по id
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("animal/detail")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAnimalDetail(Guid animalId, [FromHeader] Guid organizationId)
        {
            return await GetAnimalDetailInternal(animalId, organizationId);
        }
        
        /// <summary>
        /// БЕЗ РЕГИСТРАЦИИ Получение всей информации о животном по id
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        [HttpGet, Route("animal/mobile/detail")]
        [OrgValidationTypeFilter(checkOrg: false)]
        [AllowAnonymous]
        public async Task<IActionResult> GetAnimalDetailMobile(Guid animalId, [FromHeader] Guid organizationId)
        {
            return await GetAnimalDetailInternal(animalId, organizationId);
        }
        
        [NonAction]
        private async Task<IActionResult> GetAnimalDetailInternal(Guid animalId, Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, animalId))
                return BadRequest(new ErrorDTO("Запрашиваемое животное не принадлежит организации пользователя"));

            return Ok(_animalCardService.GetAnimalDetail2(animalId));
        }

        /// <summary>
        /// Получение всей информации о событиях животного по id
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("animal/actions")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAnimalActions(Guid animalId, [FromHeader] Guid organizationId)
        {
            return await GetAnimalActionsInternal(animalId, organizationId);
        }
        
        /// <summary>
        /// БЕЗ РЕГИСТРАЦИИ Получение всей информации о событиях животного по id
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        [HttpGet, Route("animal/mobile/actions")]
        [OrgValidationTypeFilter(checkOrg: false)]
        [AllowAnonymous]
        public async Task<IActionResult> GetAnimalActionsMobile(Guid animalId, [FromHeader] Guid organizationId)
        {
            return await GetAnimalActionsInternal(animalId, organizationId);
        }
        
        [NonAction]
        private async Task<IActionResult> GetAnimalActionsInternal(Guid animalId, Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, animalId))
                return BadRequest(new ErrorDTO("Запрашиваемое животное не принадлежит организации пользователя"));

            return Ok(_animalCardService.GetAllAnimalActions(animalId));
        }

        /// <summary>
        /// Получение данные для графика событий
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("animal/chart/action")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetAnimalChart(
            Guid animalId,
            DateOnly? startDate,
            DateOnly? endDate,
            [FromHeader] Guid organizationId)
        {
            return await GetAnimalChartInternal(animalId, startDate, endDate, organizationId);
        }
        
        /// <summary>
        /// БЕЗ РЕГИСТРАЦИИ Получение данные для графика событий
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        [HttpGet, Route("animal/mobile/chart/action")]
        [OrgValidationTypeFilter(checkOrg: false)]
        [AllowAnonymous]
        public async Task<IActionResult> GetAnimalChartMobile(
            Guid animalId,
            DateOnly? startDate,
            DateOnly? endDate,
            [FromHeader] Guid organizationId)
        {
            return await GetAnimalChartInternal(animalId, startDate, endDate, organizationId);
        }
        
        [NonAction]
        public async Task<IActionResult> GetAnimalChartInternal(
            Guid animalId,
            DateOnly? startDate,
            DateOnly? endDate,
            Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, animalId))
                return BadRequest(new ErrorDTO("Запрашиваемое животное не принадлежит организации пользователя"));

            startDate = startDate ?? DateOnly.MinValue;
            endDate = endDate ?? DateOnly.MaxValue;
            return Ok(_animalCardService.GetActionChartData(animalId, (DateOnly)startDate, (DateOnly)endDate));
        }

        /// <summary>
        /// Получение данные для графика изменения веса
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("animal/chart/weight")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetWeightChart(
            Guid animalId,
            DateOnly? startDate,
            DateOnly? endDate,
            [FromHeader] Guid organizationId)
        {
            return await GetWeightChartInternal(animalId, startDate, endDate, organizationId);
        }
        
        /// <summary>
        /// БЕЗ РЕГИСТРАЦИИ Получение данные для графика изменения веса
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        [HttpGet, Route("animal/mobile/chart/weight")]
        [OrgValidationTypeFilter(checkOrg: false)]
        [AllowAnonymous]
        public async Task<IActionResult> GetWeightChartMobile(
            Guid animalId,
            DateOnly? startDate,
            DateOnly? endDate,
            [FromHeader] Guid organizationId)
        {
            return await GetWeightChartInternal(animalId, startDate, endDate, organizationId);
        }
        
        [NonAction]
        public async Task<IActionResult> GetWeightChartInternal(
            Guid animalId,
            DateOnly? startDate,
            DateOnly? endDate,
            Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, animalId))
                return BadRequest(new ErrorDTO("Запрашиваемое животное не принадлежит организации пользователя"));

            startDate = startDate ?? DateOnly.MinValue;
            endDate = endDate ?? DateOnly.MaxValue;
            return Ok(_animalCardService.GetWeightChartData(animalId, (DateOnly)startDate, (DateOnly)endDate));
        }

        /// <summary>
        /// Получение всех родителей животного
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("animal/parent")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetWeightChart(Guid animalId, [FromHeader] Guid organizationId)
        {
            return await GetWeightChartInternal(animalId, organizationId);
        }
        
        /// <summary>
        /// Получение всех родителей животного
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <returns>Список коров</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        [HttpGet, Route("animal/mobile/parent")]
        [OrgValidationTypeFilter(checkOrg: false)]
        [AllowAnonymous]
        public async Task<IActionResult> GetWeightChartMobile(Guid animalId, [FromHeader] Guid organizationId)
        {
            return await GetWeightChartInternal(animalId, organizationId);
        }
        
        [NonAction]
        public async Task<IActionResult> GetWeightChartInternal(Guid animalId, Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, animalId))
                return BadRequest(new ErrorDTO("Запрашиваемое животное не принадлежит организации пользователя"));

            return Ok(_animalCardService.GetAnimalParentsDetail(animalId));
        }
        
        /// <summary>
        /// Обновление карточки животного
        /// </summary>
        /// <param name="animalId">Идентификатор животного</param>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="409">Пол, мать, отец или порода некорректны</response>
        /// <response code="401">Не авторизован</response>
        [HttpPatch, Route("animal/update-animal-card")]
        public async Task<IActionResult> UpdateAnimalCard([FromBody] UpdateAnimalCardDTO dto)
        {
            var result =  _animalCardService.UpdateAnimalCard(dto);
            if (result != null)
            {
                return Conflict(new ErrorDTO("Некорректные данные для изменения: " + result));
            }
            return Ok();
        }
    }
}
