using System.Text.Json;
using CAT.Controllers.DTO;
using CAT.Filters;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace CAT.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class OrganizationsController : ControllerBase
    {
        private readonly IOrganizationService _orgService;
        private readonly IUserService _userService;
        private readonly IConfiguration _config;
        private readonly IDistributedCache _cache;

        public OrganizationsController(IConfiguration config, IOrganizationService orgService,
            IUserService userService, IDistributedCache cache)
        {
            _orgService = orgService;
            _userService = userService;
            _config = config;
            _cache = cache;
        }

        /// <summary>
        /// Создание организации
        /// </summary>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Пользователь не принадлежит организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost]
        public ActionResult CreateOrganization([FromBody] CreateOrgDTO dto)
        {
            var orgId = _orgService.Create(dto);
            var userId = _userService.GetCurrentUserInfo()!.Id;
            var roleId = _config.GetValue<Guid>("Enviroment:OrgAdminId");

            _orgService.AddEmployee(orgId, userId, roleId);
            return Ok();
        }

        /// <summary>
        /// Удаление пользователя из организации
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Пользователь не принадлежит организации</response>
        /// <response code="401">Не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        [HttpDelete, Route("users/{userId}")]
        [OrgValidationTypeFilter(checkOrg: true, checkOrgAdmin: true)]
        public ActionResult DeleteEmployee([FromHeader] Guid organizationId, [FromRoute] Guid userId)
        {
            if (!_orgService.CheckEmployeeById(organizationId, userId))
                return BadRequest(new ErrorDTO(@"Пользователь, которого вы пытаетесь удалить,
                                                не принадлежит вашей организации"));

            _orgService.DeleteEmployee(organizationId, userId);
            return Ok();
        }

        /// <summary>
        /// Получение списка всех участников организации для админа
        /// </summary>
        /// <param name="organizationId">Идентификатор организации</param>
        /// <returns>Список участников</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("users")]
        [OrgValidationTypeFilter(checkOrg: true, checkLocAdmin: true)]
        public ActionResult GetAllEmployees([FromHeader] Guid organizationId)
        {
            return Ok(_orgService.GetEmployees(organizationId));
        }

        /// <summary>
        /// Генерация ссылки-приглашения в организацию
        /// </summary>
        /// <param name="organizationId">Идентификатор организации</param>
        /// <returns>Список участников</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        /// <response code="403">Не достаточно прав</response>
        [HttpPost, Route("invite")]
        [OrgValidationTypeFilter(checkOrg: true, checkLocAdmin: true)]
        [RedisExceptionFilter]
        public ActionResult CreateInvitationLink([FromHeader] Guid organizationId, [FromBody] CreateInvDTO dto)
        {
            if (organizationId != dto.OrgId)
                return BadRequest(new ErrorDTO("Невозможно создать ссылку-приглашение не в свою организацию"));

            var token = Guid.NewGuid().ToString();
            var tokenInfo = JsonSerializer.Serialize(new InvitationDTO { OrgId = dto.OrgId, RoleId = dto.RoleId, Usages = dto.UsageLimit });
            var baseUrl = _config.GetValue<string>("Enviroment:BaseFrontUrl");

            _cache.SetString(token, tokenInfo, options: new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = dto.ExpireTime
            });

            return Ok(new InviteLinkDTO(baseUrl + $"/invite/{token}"));
        }

        /// <summary>
        /// Использование ссылки-приглашения авторизованным пользователем
        /// </summary>
        /// <param name="organizationId">Идентификатор организации</param>
        /// <returns>Список участников</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Невалидная ссылка-приглашение</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost, Route("invite/{token}")]
        [RedisExceptionFilter]
        public ActionResult UseInvitationLink([FromRoute] Guid token)
        {
            var cachedInviteInfo = _cache.GetString(token.ToString());
            if (cachedInviteInfo == null)
                return BadRequest(new ErrorDTO("Срок действия ссылки-приглашения истёк."));

            var inviteInfo = JsonSerializer.Deserialize<InvitationDTO>(cachedInviteInfo);

            if (!_orgService.IsOrgExist(inviteInfo!.OrgId))
                return BadRequest(new ErrorDTO("Невалидная ссылка-приглашение. Организации не существует."));

            if (!_userService.IsUserRoleExist(inviteInfo.RoleId))
                return BadRequest(new ErrorDTO("Невалидная ссылка-приглашение. Роль, на которую Вас пригласили, больше не существует."));

            var userId = _userService.GetCurrentUserInfo()!.Id;
            _orgService.AddEmployee(inviteInfo.OrgId, userId, inviteInfo.RoleId);

            inviteInfo.Usages -= 1;
            if (inviteInfo.Usages <= 0)
            {
                _cache.Remove(token.ToString());
            }
            else
            {
                var refreshedInviteInfo = JsonSerializer.Serialize(inviteInfo);
                _cache.SetString(token.ToString(), refreshedInviteInfo);
            }

            return Ok();
        }

        /// <summary>
        /// Изменение роли сотрудника
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Пользователь не принадлежит организации</response>
        /// <response code="401">Не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        [HttpPatch, Route("users/{userId}/role")]
        [OrgValidationTypeFilter(checkOrg: true, checkOrgAdmin: true)]
        public ActionResult ChangeEmployeeRole([FromHeader] Guid organizationId, [FromRoute] Guid userId, [FromBody] ChangeUserRoleDTO dto)
        {
            if (!_orgService.CheckEmployeeById(organizationId, userId))
                return BadRequest(new ErrorDTO("Пользователь, которого вы пытаетесь изменить, не принадлежит вашей организации"));

            _userService.ChangeUserRole(userId, dto.RoleId);
            return Ok();
        }

        /// <summary>
        /// Сброс пароля сотрудника
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Пользователь не принадлежит организации</response>
        /// <response code="401">Не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        [HttpPatch, Route("users/{userId}/password")]
        [OrgValidationTypeFilter(checkOrg: true, checkOrgAdmin: true)]
        public ActionResult ResetEmployeePassword([FromHeader] Guid organizationId, [FromRoute] Guid userId, [FromBody] ResetUserPasswordDTO dto)
        {
            if (!_orgService.CheckEmployeeById(organizationId, userId))
                return BadRequest(new ErrorDTO("Пользователь, которому вы пытаетесь изменить пароль, не принадлежит вашей организации"));

            _userService.ResetUserPassword(userId, dto.Password);
            return Ok();
        }
    }
}
