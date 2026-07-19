using System.ComponentModel.DataAnnotations;
using CAT.Logic;

namespace CAT.Controllers.DTO
{
    public class CensusQueryDTO
    {
        /// <summary>
        /// Тип животного
        /// </summary>
        /// <example>Корова</example>
        [Required]
        [IsIn("Корова", "Бык", "Бычок", "Нетель", "Телка", "Яловые")]
        public string Type { get; init; }

        /// <summary>
        /// Номер страницы
        /// </summary>
        /// <example>Корова</example>
        [Required, GreaterThan(0)]
        public int Page { get; init; }

        public CensusSortInfoDTO SortInfo { get; init; }

        public string? Search { get; init; }
    }
}
