using System.Text.Json;
using CAT.Controllers.DTO;
using CAT.Filters;
using CAT.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace CAT.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IDistributedCache _cache;
        public AuthController(IAuthService authService, IDistributedCache cache)
        {
            _authService = authService;
            _cache = cache;
        }

        /// <summary>
        /// Авторизация по логину и паролю на куках
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неправльно введённые данные</response>
        [HttpPost, Route("login")]
        [AuthExceptionFilter]
        public ActionResult Login([FromBody] LoginDTO body)
        {
            var userInfo = _authService.LogIn(body.Login, body.Password);
            return Ok(userInfo);
        }

        /// <summary>
        /// Авторизация через телеграм
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неправльно введённые данные</response>
        [HttpPost, Route("login/telegram")]
        [AuthTgExceptionFilter, ConfigurationExceptionFilter]
        public ActionResult LoginTG([FromBody] LoginTgDTO dto)
        {
            var userInfo = _authService.LogInTg(dto);
            return Ok(userInfo);
        }

        /// <summary>
        /// Отзыв кук с токеном
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="401">Не авторизован</response>
        [HttpPost, Route("logout"), Authorize]
        public ActionResult LogOut()
        {
            _authService.LogOut();
            return Ok();
        }

        /// <summary>
        /// Проверка, занят ли логин
        /// </summary>
        /// <response code="200">Успешное выполнение</response>
        [HttpGet, Route("login")]
        public ActionResult CheckLogin([FromQuery] string login)
        {
            return Ok(_authService.CheckLogin(login));
        }

        /// <summary>
        /// Обновляет данные логина, выдаёт новую куку
        /// </summary>
        /// <response code="200">Успешное выполнение</response>
        [HttpPost, Route("login/update"), Authorize]
        public ActionResult LoginUpdateData()
        {
            var userInfo = _authService.RefreshLoginData();
            if (userInfo == null)
                return BadRequest(new ErrorDTO("Пользователя не существует."));
            return Ok(userInfo);
        }
        

        /// <summary>
        /// Запрос от бота, начало регистрации
        /// </summary>
        /// <response code="200">Успешное выполнение</response>
        [HttpPost, Route("bot/start")]
        [RedisExceptionFilter]
        public ActionResult StartRegistration([FromBody] BotStartDTO dto)
        {
            _cache.SetString(dto.TgId, dto.SessionToken.ToString(), options: new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok();
        }

        /// <summary>
        ///  Запрос от бота, подтверждение регистрации
        /// </summary>
        /// <response code="200">Успешное выполнение</response>
        [HttpPost, Route("bot/confirm")]
        [RedisExceptionFilter]
        public ActionResult ConfirmRegistration([FromBody] BotConfirmDTO dto)
        {
            var token = _cache.GetString(dto.TgId);
            if (token == null)
                return BadRequest(new ErrorDTO("Регистрация не начата или время истекло"));

            if (dto.PhoneNumber.StartsWith('+')) dto.PhoneNumber = dto.PhoneNumber.Remove(0, 1);

            var regInfo = JsonSerializer.Serialize(dto);
            _cache.SetString(token, regInfo, options: new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            _cache.Remove(dto.TgId);

            return Ok(new BotTokenDTO(Guid.Parse(token)));
        }

        /// <summary>
        /// Получение данных регистрации через бота
        /// </summary>
        /// <response code="200">Успешное выполнение</response>
        [HttpGet, Route("bot/{token}")]
        [RedisExceptionFilter]
        public ActionResult GetRegistrationData([FromRoute] Guid token)
        {
            var regInfoString = _cache.GetString(token.ToString());
            if (regInfoString == null)
                return BadRequest(new ErrorDTO("Регистрация не подтверждена"));
            var regInfo = JsonSerializer.Deserialize<BotConfirmDTO>(regInfoString);

            _cache.Remove(token.ToString());
            return Ok(regInfo);
        }
    }
}
