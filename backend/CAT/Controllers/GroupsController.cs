using CAT.Controllers.DTO;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupsController : Controller
    {
        private readonly IGroupService _groupService;

        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        /// <summary>
        /// Получить список всех типов групп по организации.
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet, Route("type")]
        [OrgValidationTypeFilter()]
        public async Task<IActionResult> GetAllGroupTypes([FromHeader] Guid organizationId)
        {
            return Ok(_groupService.GetGroupTypes(organizationId));
        }

        /// <summary>
        /// Создать новый тип группы.
        /// </summary>
        /// <param name="dto">Данные нового типа группы</param>
        /// <param name="organizationId">ID организации</param>
        [HttpPost, Route("type")]
        [OrgValidationTypeFilter()]
        public async Task<IActionResult> CreateGroupType([FromBody] CreateGroupTypeDTO dto, [FromHeader] Guid organizationId)
        {
            var ans = _groupService.CreateGroupType(dto, organizationId);
            if (!ans) return BadRequest(new { ErrorText = "Тип группы с таким названием уже существует в данной организации" });
            return Ok(new { Message = "Тип группы успешно создан" });
        }

        /// <summary>
        /// Удалить тип группы.
        /// </summary>
        /// <param name="typeId">ID типа группы</param>
        [HttpDelete, Route("type")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> DeleteGroupType([FromHeader] Guid organizationId, [FromQuery] Guid typeId)
        {
            var ans = _groupService.DeleteGroupType(typeId, organizationId);
            if (!ans) return BadRequest(new { ErrorText = "Тип группы не найден в организации или не может быть удалён, т.к. есть группы с данным типом." });
            return Ok(new { Message = "Тип группы успешно удалён" });
        }

        /// <summary>
        /// Получить список всех групп в организации.
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet, Route("")]
        [OrgValidationTypeFilter()]
        public async Task<IActionResult> GetAllGroup([FromHeader] Guid organizationId)
        {
            return Ok(_groupService.GetGroupsByOrganization(organizationId));
        }

        /// <summary>
        /// Создать новую группу.
        /// </summary>
        /// <param name="dto">Данные новой группы</param>
        /// <param name="organizationId">ID организации</param>
        [HttpPost, Route("")]
        [OrgValidationTypeFilter()]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDTO dto, [FromHeader] Guid organizationId)
        {
            var ans = _groupService.CreateGroup(dto, organizationId);
            if (!ans) return BadRequest(new { ErrorText = "Группа с таким названием уже существует в данной организации" });
            return Ok(new { Message = "Группа успешно создана" });
        }

        /// <summary>
        /// Удалить группу.
        /// </summary>
        /// <param name="groupId">ID группы</param>
        [HttpDelete, Route("")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> DeleteGroup([FromHeader] Guid organizationId, [FromQuery] Guid groupId)
        {
            var ans = _groupService.DeleteGroup(groupId, organizationId);
            if (!ans) return BadRequest(new { ErrorText = "Группа не найдена в организации или не может быть удалёна, т.к в ней есть животные." });
            return Ok(new { Message = "Группа успешно удалёна" });
        }

        /// <summary>
        /// Обновить данные группы.
        /// </summary>
        /// <param name="dto">Данные для обновления</param>
        /// <param name="organizationId">ID организации</param>
        [HttpPut, Route("")]
        [OrgValidationTypeFilter()]
        public async Task<IActionResult> EditGroup([FromBody] EditGroupDTO dto, [FromHeader] Guid organizationId)
        {
            _groupService.EditGroup(dto, organizationId);
            return Ok(new { Message = "Группа успешно изменена" });
        }

        /// <summary>
        /// Получить список всех полей идентификации по организации.
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet, Route("identification")]
        [OrgValidationTypeFilter()]
        public async Task<IActionResult> GetAllIdentificationFields([FromHeader] Guid organizationId)
        {
            return Ok(_groupService.GetIdentificationsByOrganization(organizationId));
        }

        /// <summary>
        /// Получить список всех значений по полю идентификации организации.
        /// </summary>
        /// <param name="organizationId">ID организации</param>
        [HttpGet, Route("identification/values")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> GetIdentificationValues([FromHeader] Guid organizationId, [FromQuery] IdentificationValuesDTO dto)
        {
            return Ok(_groupService.GetIdentificationValues(dto.IdentificationId, organizationId, dto.Filter));
        }

        /// <summary>
        /// Создать новое поле идентификации.
        /// </summary>
        /// <param name="dto">Данные нового поля</param>
        /// <param name="organizationId">ID организации</param>
        [HttpPost, Route("identification")]
        [OrgValidationTypeFilter()]
        public async Task<IActionResult> CreateIdentificationField([FromBody] CreateIdentificationDTO dto, [FromHeader] Guid organizationId)
        {
            var ans = _groupService.CreateIdentification(dto, organizationId);
            if (!ans) return BadRequest(new { ErrorText = "Поле идентификации с таким названием уже существует в данной организации" });
            return Ok(new { Message = "Поле идентификации успешно создано" });
        }

        /// <summary>
        /// Удалить поле идентификации.
        /// </summary>
        /// <param name="identificationId">ID поля</param>
        [HttpDelete, Route("identification")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> DeleteIdentification([FromHeader] Guid organizationId, [FromQuery] Guid identificationId)
        {
            var ans = _groupService.DeleteIdentification(identificationId, organizationId);
            if (!ans) return BadRequest(new { ErrorText = "Поле идентификации не найдено в данной организации." });
            return Ok(new { Message = "Поле идентификации успешно удалёно" });
        }
    }
}
