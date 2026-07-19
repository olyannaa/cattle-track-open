using CAT.Controllers.DTO;
using CAT.Filters;
using CAT.Services;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IOrganizationService _orgService;
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public UsersController(IOrganizationService orgService, IUserService userService, IAuthService authService)
        {
            _orgService = orgService;
            _userService = userService;
            _authService = authService;
        }

        /// <summary>
        /// Добавление пользователя в систему
        /// </summary>
        /// <response code="200">Успешное выполнение</response>
        [HttpPost]
        [ConfigurationExceptionFilter]
        public ActionResult RegisterUser([FromBody] RegisterUserDTO dto)
        {
            if (_authService.CheckLogin(dto.Login))
                return BadRequest(new ErrorDTO("Пользователь с таким логином уже существует."));

            if (_authService.CheckPhone(dto.PhoneNumber))
                return BadRequest(new ErrorDTO("Номер телефона уже зарегестрирован."));

            _userService.RegisterUser(dto);
            return Ok();
        }

        /// <summary>
        /// Получение информации о текущем пользователе
        /// </summary>
        /// <returns>Информация о пользователе</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("self"), Authorize]
        public ActionResult GetUserInfo()
        {
            return Ok(_userService.GetCurrentUserInfo());
        }

        /// <summary>
        /// Получение списка всех участников организации
        /// </summary>
        /// <param name="organizationId">Идентификатор организации</param>
        /// <returns>Список участников</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверный идентификатор организации</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Authorize]
        [OrgValidationTypeFilter(checkOrg: true)]
        public ActionResult GetAllEmployees([FromHeader] Guid organizationId)
        {
            return Ok(_orgService.GetEmployees(organizationId, true));
        }

        /// <summary>
        /// Получение списка ролей
        /// </summary>
        /// <returns>Информация о ролях</returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("roles"), Authorize]
        public ActionResult GetRolesInfo()
        {
            return Ok(_userService.GetRoles());
        }

    }
}
