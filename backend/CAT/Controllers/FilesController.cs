using System.Text.Json;
using CAT.Controllers.DTO;
using CAT.EF;
using CAT.Services;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly ISpreadsheetService _csv;
        private readonly IAnimalService _animalService;
        private readonly IAuthService _authService;
        private readonly PostgresContext _db;

        public FilesController(ISpreadsheetService csv, IAnimalService animalService,
            IAuthService authService, PostgresContext postgresContext)
        {
            _csv = csv;
            _animalService = animalService;
            _authService = authService;
            _db = postgresContext;
        }

        /// <summary>
        /// Экспорт списка животных в csv
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("csv/animals"), Authorize]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetListOfCattle([FromQuery] CensusCsvDTO dto, [FromHeader] Guid organizationId)
        {
            var census = _animalService.GetAnimalCensusWithFilters(organizationId,
                filters: dto.Filters, sortInfo: dto.SortInfo);
            var res = _animalService.GetAnimalCensusByPageWithIF(census)
                .Select(e => new ExportAnimalCsvDTO(
                    e.TagNumber,
                    e.BirthDate,
                    e.Breed,
                    e.GroupName,
                    e.Status,
                    e.Origin,
                    e.OriginLocation,
                    e.MotherTagNumber,
                    e.FatherTagNumbersList,
                    e.LastVaccinationDate,
                    e.IdentificationFields
                ))
                .ToList();
            var csvFile = _csv.Write(res);

            return File(csvFile, "application/octet-stream", _csv.GetFileName(dto.Type));
        }
    }
}
