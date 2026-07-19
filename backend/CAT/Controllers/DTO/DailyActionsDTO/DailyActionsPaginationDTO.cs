using System.ComponentModel.DataAnnotations;
using CAT.Logic;

namespace CAT.Controllers.DTO
{
    public class DailyActionsPaginationDTO : DailyActionsDTO
    {
        /// <summary>
        /// Номер страницы
        /// </summary>
        /// <example>1</example>
        [Required, GreaterThan(0)]
        public int Page { get; init; }
    }
}