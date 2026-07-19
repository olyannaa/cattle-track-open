using CAT.Controllers.DTO;
using CAT.Logic;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class WeightsController : ControllerBase
    {
        private readonly IWeightsService _weightsService;
        private readonly IOrganizationService _orgService;

        public WeightsController(IWeightsService weightsService, IOrganizationService orgService)
        {
            _orgService = orgService;
            _weightsService = weightsService;
        }

        /// <summary>
        /// Возвращение списка взвешиваний животного c пагинацией
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверно введённые данные</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("{animalId}")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetListOfWeights([FromRoute] Guid animalId, [FromQuery] WeightsDTO? dto, [FromHeader] Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, animalId))
                return BadRequest(new ErrorDTO("Один из животных не принадлежит организации пользователя."));

            var isMobileDevice = ControllersLogic.IsMobileDevice(Request.Headers.UserAgent);

            var weights = _weightsService.GetWeightsInfoByPage(animalId, dto.sortInfo, dto.Page, isMobileDevice);
            return Ok(weights);
        }

        /// <summary>
        /// Возвращение информации о последнем взвешивании животного
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверно введённые данные</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("{animalId}/last")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetLastWeight([FromRoute] Guid animalId, [FromHeader] Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, animalId))
                return BadRequest(new ErrorDTO("Один из животных не принадлежит организации пользователя."));

            var weight = _weightsService.GetWeightsInfo(animalId)
                            .OrderByDescending(e => e.Date)
                            .FirstOrDefault();
            return Ok(weight);
        }

        /// <summary>
        /// Возвращение статистику взвешиваний по животному
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверно введённые данные</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("{animalId}/statistics")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetStatistics([FromRoute] Guid animalId, [FromHeader] Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, animalId))
                return BadRequest(new ErrorDTO("Один из животных не принадлежит организации пользователя."));

            var statistics = _weightsService.GetWeightsStatistics(animalId);

            return Ok(statistics);
        }

        /// <summary>
        ///Информация о списке привесов для пагинации
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверно введённые данные</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("{animalId}/pagination-info")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetPagination([FromRoute] Guid animalId, [FromHeader] Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, animalId))
                return BadRequest(new ErrorDTO("Один из животных не принадлежит организации пользователя."));

            var entries = ControllersLogic.IsMobileDevice(Request.Headers.UserAgent) ? 5 : 10;

            var count = _weightsService.GetWeightsInfo(animalId).Count();
            return Ok(new PaginationDTO { Count = count, EntriesPerPage = entries });
        }

        /// <summary>
        /// Создать запись о взвешивании
        /// </summary>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверно введённые данные</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult CreateWeightEntry(
            [FromBody] WeightCreateDTO dto,
            [FromHeader] Guid organizationId)
        {
            return CreateWeightEntryInternal(dto, organizationId);
        }

        /// <summary>
        /// БЕЗ АВТОРИЗАЦИИ. Создать запись о взвешивании
        /// </summary>
        [HttpPost("mobile")]
        [AllowAnonymous]
        [OrgValidationTypeFilter(checkOrg: false)]
        public IActionResult CreateWeightEntryMobile(
            [FromBody] WeightCreateDTO dto,
            [FromHeader] Guid organizationId)
        {
            return CreateWeightEntryInternal(dto, organizationId);
        }
        
        private IActionResult CreateWeightEntryInternal(
            WeightCreateDTO dto,
            Guid organizationId)
        {
            if (!_orgService.CheckAnimalById(organizationId, dto.AnimalId))
            {
                return BadRequest(
                    new ErrorDTO("Один из животных не принадлежит организации пользователя.")
                );
            }

            var message = _weightsService.InsertWeights(dto);
            return Ok(message);
        }
    }
}
