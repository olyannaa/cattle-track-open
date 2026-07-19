using CAT.Controllers.DTO;
using CAT.EF;
using CAT.Logic;
using CAT.Services;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class AnimalsController(
        IAnimalService animalService,
        PostgresContext postgresContext,
        ISpreadsheetService spreadsheetService,
        MinioS3Service s3Service,
        IOrganizationService orgService)
        : ControllerBase
    {
        private static readonly string[] extensions = { ".png", ".jpg", ".jpeg" };

        /// <summary>
        /// Возвращение списка животных по фильтрам с пагинацией
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверно введённые данные</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet]
        [Route("")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetListOfCattle([FromQuery] AnimalCensusQueryDTO dto, [FromHeader] Guid organizationId)
        {
            return GetListOfCattleInternal(dto, organizationId);
        }
        
        /// <summary>
        /// БЕЗ АВТОРИЗАЦИИ Возвращение списка животных по фильтрам с пагинацией
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверно введённые данные</response>
        [HttpGet]
        [Route("mobile/animals")]
        [OrgValidationTypeFilter(checkOrg: false)]
        [AllowAnonymous]
        public IActionResult GetListOfCattleMobile([FromQuery] AnimalCensusQueryDTO dto, [FromHeader] Guid organizationId)
        {
            return GetListOfCattleInternal(dto, organizationId);
        }
        
        private IActionResult GetListOfCattleInternal(AnimalCensusQueryDTO dto, Guid organizationId)
        {
            var isMobileDevice = ControllersLogic.IsMobileDevice(Request.Headers.UserAgent);

            var census = animalService.GetAnimalCensusByPageWithFilters(
                organizationId,
                dto.Filters,
                dto.SortInfo,
                dto.Page,
                isMobileDevice
            );

            return Ok(census);
        }


        /// <summary>
        /// Удаление яловых коров
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверно введённые данные</response>
        /// <response code="401">Не авторизован</response>
        [HttpDelete, Route("barren")]
        [OrgValidationTypeFilter(checkOrg: true, checkLocAdmin: true)]
        public IActionResult GetListOfCattle([FromBody] Guid[] animalIds, [FromHeader] Guid organizationId)
        {
            using (var transaction = postgresContext.Database.BeginTransaction())
            {
                foreach (var animalId in animalIds)
                {
                    if (!orgService.CheckAnimalById(organizationId, animalId))
                    {
                        transaction.Rollback();
                        return BadRequest(new ErrorDTO("Один из животных не принадлежит организации пользователя"));
                    }
                    try
                    {
                        animalService.RemoveCowFromBarren(animalId);
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
        /// Возвращение списка id яловых коров
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("barren/ids")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetDailyActionsIds([FromHeader] Guid organizationId, [FromQuery] bool IsActive)
        {
            var ids = animalService.GetAnimalCensus(organizationId, "Яловые", sortInfo: new CensusSortInfoDTO{ Active = IsActive })?
                                .Select(e => e.Id)
                                .ToList();
            return Ok(ids);
        }

        /// <summary>
        /// Возвращение списка пород
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("breed")]
        public IActionResult GetAllBreeds()
        {
            return Ok(animalService.GetAllBreeds());
        }

        /// <summary>
        /// Возвращение информации о животном
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="400">Неверно введённые данные</response>
        /// <response code="401">Не авторизован</response>
        [HttpGet, Route("{animalId}")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetAnimalInfo([FromRoute] Guid animalId, [FromHeader] Guid organizationId)
        {
            if (!orgService.CheckAnimalById(organizationId, animalId))
                return BadRequest(new ErrorDTO("Запрашиваемое животное не принадлежит организации пользователя"));

            var animal = animalService.GetAnimalInfo(organizationId, animalId);
            return Ok(animal);
        }
        
        /// <summary>
        /// Информация о списке животных для пагинации
        /// </summary>
        /// <param name="type">Тип животного</param>
        /// <param name="organizationId">Id организации</param>
        /// <returns></returns>
        [HttpGet, Route("pagination-info")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetPagination([FromQuery] PaginationQueryDTO dto, [FromHeader] Guid organizationId)
        {
            var entries = ControllersLogic.IsMobileDevice(Request.Headers.UserAgent) ? 5 : 10;
            var count = animalService.CountAnimalCensusWithFilters(organizationId, dto.Filters);
            return Ok(new PaginationDTO { Count = count, EntriesPerPage = entries });
        }

        /// <summary>
        /// Обновление данных о животном
        /// </summary>
        /// <param name="id">Id животного</param>
        /// <param name="dto">Редактируемые данные</param>
        /// <returns></returns>
        /// <response code="200">Успешное выполнение</response>
        /// <response code="401">Не авторизован</response>
        /// <response code="403">Пользователь не админ или не имеет доступа к организации</response>
        [HttpPut]
        [OrgValidationTypeFilter(checkLocAdmin: true, checkOrg: true)]
        public IActionResult EditCattleEntry([FromBody] UpdateAnimalDTO[] dtoArray, [FromHeader] Guid organizationId)
        {
            using (var transaction = postgresContext.Database.BeginTransaction())
            {
                foreach (var dto in dtoArray)
                {
                    if (!orgService.CheckAnimalById(organizationId, dto.Id))
                    {
                        transaction.Rollback();
                        return BadRequest(new ErrorDTO("Один из животных не принадлежит организации пользователя"));
                    }

                    if (dto.GroupID != null && !orgService.CheckGroupById(organizationId, dto.GroupID))
                    {
                        transaction.Rollback();
                        return BadRequest(new ErrorDTO("Одного из животных не возможно добавить в группу, не пренадлежащую организации пользователя"));
                    }
                    try
                    {
                        animalService.UpdateAnimal(dto);
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
        /// Регистрирует новое животное в системе.
        /// </summary>
        [HttpPost, Route("registration")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public async Task<IActionResult> RegistrationAnimal(
            [FromForm] AnimalRegistrationDTO body,
            [FromHeader] Guid organizationId)
        {
            await RegisterAnimalInternal(body, organizationId);
            return Ok(new { Message = "Животное успешно зарегистрировано!" });
        }

        /// <summary>
        /// БЕЗ АВТОРИЗАЦИИ. Регистрирует новое животное в системе.
        /// </summary>
        [HttpPost, Route("mobile/registration")]
        [AllowAnonymous]
        [OrgValidationTypeFilter(checkOrg: false)]
        public async Task<IActionResult> RegistrationAnimalMobile(
            [FromForm] AnimalRegistrationDTO body,
            [FromHeader] Guid organizationId)
        {
            await RegisterAnimalInternal(body, organizationId);
            return Ok(new { Message = "Животное успешно зарегистрировано!" });
        }
        
        private async Task RegisterAnimalInternal(
            AnimalRegistrationDTO body,
            Guid organizationId)
        {
            var id = animalService.RegisterAnimal(body, organizationId);

            if (body.Photo != null &&
                extensions.Contains(Path.GetExtension(body.Photo.FileName)))
            {
                await s3Service.UploadFileInS3Async(body.Photo, id);
            }
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var result = await s3Service.CheckS3AccessAsync();
                return Ok(new
                {
                    Success = result,
                    Message = result ? "Подключение к MinIO успешно" : "Ошибка подключения к MinIO"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Ошибка: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Импортирует данные о животных из xlsx-файла.
        /// </summary>
        [HttpPost, Route("registration/import/xlsx")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public ActionResult ImportAnimalsFromXLSX(IFormFile file, [FromHeader] Guid organizationId)
        {
            if (file == null || !new string[] { ".xlsx" }.Contains(Path.GetExtension(file.FileName)))
                return BadRequest("Формат файла должен быть .xlsx");

            var animals = spreadsheetService.ReadAnimals(file.OpenReadStream())
                                     .Select(x =>
                                     {
                                         switch (x.Type)
                                         {
                                             case "1": x.Type = "Бычок"; break;
                                             case "2": x.Type = "Телка"; break;
                                             case "3": x.Type = "Бык"; break;
                                             case "4": x.Type = "Корова"; break;
                                         }
                                         return x;
                                     })
                                     .ToList();

            if (animals.Count == 0) return StatusCode(400);
            var importInfo = animalService.ImportAnimalsFromXLSX(animals, organizationId);
            if (importInfo.Errors > 0) return BadRequest(new { ErrorText = importInfo.Message });
            return Ok(importInfo);
        }

        /// <summary>
        /// Получает информацию о группах животных.
        /// </summary>
        [HttpGet, Route("groups")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetGroups([FromHeader] Guid organizationId)
        {
            return Ok(animalService.GetGroupsInfo(organizationId));
        }

        /// <summary>
        /// Получает идентификационные поля для животных.
        /// </summary>
        [HttpGet, Route("identifications")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetIdentificationsFields([FromHeader] Guid organizationId)
        {
            return Ok(animalService.GetIdentificationsFields(organizationId));
        }

        /// <summary>
        /// Получает словарь с количеством животных каждого типа организации.
        /// </summary>
        [HttpGet, Route("main-info")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetMainPageInfo([FromHeader] Guid organizationId)
        {
            return Ok(animalService.GetMainPageInfo(organizationId));
        }
        
        /// <summary>
        /// БЕЗ АВТОРИЗАЦИИ Получает словарь с количеством животных каждого типа организации.
        /// </summary>
        [HttpGet, Route("mobile/main-info")]
        [AllowAnonymous]
        [OrgValidationTypeFilter(checkOrg: false)]
        public IActionResult GetMobileMainPageInfo([FromHeader] Guid organizationId)
        {
            return Ok(animalService.GetMainPageInfo(organizationId));
        }
        
        /// <summary>
        /// Места рождения животных в организации
        /// </summary>
        [HttpGet, Route("places-of-origin")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetPlaceOfOrigin([FromHeader] Guid organizationId)
        {
            return Ok(animalService.GetPlaceOfOrigin(organizationId));
        }
        
        /// <summary>
        /// Способы происхождения животных в организации
        /// </summary>
        [HttpGet, Route("origins")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetOrigins([FromHeader] Guid organizationId)
        {
            return Ok(animalService.GetOrigins(organizationId));
        }
    }
}
