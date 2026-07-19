using System.ComponentModel.DataAnnotations;
using CAT.Logic;

namespace CAT.Controllers.DTO
{
    public class CensusCsvDTO
    {
        /// <summary>
        /// Тип животного
        /// </summary>
        /// <example>Корова</example>
        [IsIn("Корова", "Бык", "Бычок", "Нетель", "Телка", null)]
        public string? Type { get; init; }

        public CensusSortInfoDTO SortInfo { get; init; }
        
        public AnimalFiltersDTO? Filters { get; init;  }
    }
}
