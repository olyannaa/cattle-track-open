using CAT.Controllers.DTO;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Controllers
{
    using CAT.Services;
    using Microsoft.AspNetCore.Mvc;
    using System.Globalization;

    [ApiController]
    [Route("api/statistics/charts")]
    public sealed class StatisticsChartsController : ControllerBase
    {
        private readonly IStatisticsChartsService _service;
        private readonly IMedicineService _medicineService;

        public StatisticsChartsController(IStatisticsChartsService service, IMedicineService medicineService)
        {
            _service = service;
            _medicineService = medicineService;
        }

        /// <summary>
        /// Суточный привес
        /// </summary>
        [HttpGet("daily-weight-gain")]
        public ActionResult<List<ChartWeightPointDTO>> GetDailyWeightGain(
            [FromHeader] Guid organizationId,
            [FromQuery] string dateFrom,
            [FromQuery] string dateTo)
        {
            if (!TryParseDateOnly(dateFrom, out var from))
                return BadRequest($"Invalid dateFrom: '{dateFrom}'");

            if (!TryParseDateOnly(dateTo, out var to))
                return BadRequest($"Invalid dateTo: '{dateTo}'");

            return Ok(_service.GetDailyWeightGainChart(organizationId, from, to));
        }

        /// <summary>
        /// Вес в 12 месяцев
        /// </summary>
        [HttpGet("weight-at-12-months")]
        public ActionResult<List<ChartWeightPointDTO>> GetWeightAt12Months(
            [FromHeader] Guid organizationId,
            [FromQuery] string dateFrom,
            [FromQuery] string dateTo)
        {
            if (!TryParseDateOnly(dateFrom, out var from))
                return BadRequest($"Invalid dateFrom: '{dateFrom}'");

            if (!TryParseDateOnly(dateTo, out var to))
                return BadRequest($"Invalid dateTo: '{dateTo}'");

            return Ok(_service.GetWeightAt12MonthsChart(organizationId, from, to));
        }

        /// <summary>
        /// Отёлы
        /// </summary>
        [HttpGet("calvings")]
        public ActionResult<List<ChartEventPointDTO>> GetCalvings(
            [FromHeader] Guid organizationId,
            [FromQuery] string dateFrom,
            [FromQuery] string dateTo)
        {
            if (!TryParseDateOnly(dateFrom, out var from))
                return BadRequest($"Invalid dateFrom: '{dateFrom}'");

            if (!TryParseDateOnly(dateTo, out var to))
                return BadRequest($"Invalid dateTo: '{dateTo}'");

            return Ok(_service.GetCalvingsChart(organizationId, from, to));
        }

        /// <summary>
        /// Стельности
        /// </summary>
        [HttpGet("pregnancy")]
        public ActionResult<List<ChartEventPointDTO>> GetPregnancy(
            [FromHeader] Guid organizationId,
            [FromQuery] string dateFrom,
            [FromQuery] string dateTo)
        {
            if (!TryParseDateOnly(dateFrom, out var from))
                return BadRequest($"Invalid dateFrom: '{dateFrom}'");

            if (!TryParseDateOnly(dateTo, out var to))
                return BadRequest($"Invalid dateTo: '{dateTo}'");

            return Ok(_service.GetPregnancyChart(organizationId, from, to));
        }

        /// <summary>
        /// Вакцинация
        /// </summary>
        [HttpGet("vaccinations")]
        public ActionResult<List<ChartEventPointDTO>> GetVaccinations(
            [FromHeader] Guid organizationId,
            [FromQuery] string dateFrom,
            [FromQuery] string dateTo)
        {
            if (!TryParseDateOnly(dateFrom, out var from))
                return BadRequest($"Invalid dateFrom: '{dateFrom}'");

            if (!TryParseDateOnly(dateTo, out var to))
                return BadRequest($"Invalid dateTo: '{dateTo}'");

            return Ok(_service.GetVaccinationsChart(organizationId, from, to));
        }

        /// <summary>
        /// Взятие крови
        /// </summary>
        [HttpGet("blood-tests")]
        public ActionResult<List<ChartEventPointDTO>> GetBloodTests(
            [FromHeader] Guid organizationId,
            [FromQuery] string dateFrom,
            [FromQuery] string dateTo)
        {
            if (!TryParseDateOnly(dateFrom, out var from))
                return BadRequest($"Invalid dateFrom: '{dateFrom}'");

            if (!TryParseDateOnly(dateTo, out var to))
                return BadRequest($"Invalid dateTo: '{dateTo}'");

            return Ok(_service.GetBloodTestsDiagnosisPercentChart(organizationId, from, to));
        }

        /// <summary>
        /// Вес при рождении
        /// </summary>
        [HttpGet("birth-weight")]
        public ActionResult<List<ChartBirthWeightPointDTO>> GetBirthWeight(
            [FromHeader] Guid organizationId,
            [FromQuery] string dateFrom,
            [FromQuery] string dateTo)
        {
            if (!TryParseDateOnly(dateFrom, out var from))
                return BadRequest($"Invalid dateFrom: '{dateFrom}'");

            if (!TryParseDateOnly(dateTo, out var to))
                return BadRequest($"Invalid dateTo: '{dateTo}'");

            return Ok(_service.GetBirthWeightChart(organizationId, from, to));
        }

        [HttpGet("medicine-map")]
        [OrgValidationTypeFilter(checkOrg: true)]
        public IActionResult GetVaccinationMedicinesMap(
            [FromHeader] Guid organizationId,
            [FromQuery] string dateFrom,
            [FromQuery] string dateTo)
        {
            try
            {
                if (!TryParseDateOnly(dateFrom, out var from))
                    return BadRequest(new ErrorDTO($"Invalid dateFrom: '{dateFrom}'"));

                if (!TryParseDateOnly(dateTo, out var to))
                    return BadRequest(new ErrorDTO($"Invalid dateTo: '{dateTo}'"));

                var map = _service.GetVaccinationMedicinesMap(organizationId, from, to);
                return Ok(map);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorDTO(ex.Message));
            }
        }



        private static readonly string[] DateFormats =
        [
            "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd.MM.yyyy",
        "dd/MM/yyyy",
        "MM/dd/yyyy",

        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "dd.MM.yyyy HH:mm:ss",
        "dd.MM.yyyy HH:mm",
        "dd.MM.yyyyTHH:mm:ss",
        "dd.MM.yyyyTHH:mm:ss.fff"
        ];

        private static bool TryParseDateOnly(string input, out DateOnly date)
        {
            input = (input ?? string.Empty).Trim();

            foreach (var culture in new[] { CultureInfo.InvariantCulture, new CultureInfo("ru-RU") })
            {
                if (DateOnly.TryParseExact(input, DateFormats, culture, DateTimeStyles.AllowWhiteSpaces, out date))
                    return true;
            }

            if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var dto))
            {
                date = DateOnly.FromDateTime(dto.DateTime);
                return true;
            }

            if (DateTime.TryParse(input, new CultureInfo("ru-RU"), DateTimeStyles.AllowWhiteSpaces, out var dtRu))
            {
                date = DateOnly.FromDateTime(dtRu);
                return true;
            }

            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dtInv))
            {
                date = DateOnly.FromDateTime(dtInv);
                return true;
            }

            date = default;
            return false;
        }
    }

}
