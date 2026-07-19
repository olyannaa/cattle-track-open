using System.ComponentModel.DataAnnotations;
using CAT.Logic;

namespace CAT.Controllers.DTO
{
    public class DailyActionsDTO
    {
        /// <summary>
        /// Тип ежедневного действия
        /// </summary>
        /// <example>Осмотры</example>
        [Required]
        [IsIn("Осмотры", "Вакцинации и обработки", "Лечение", "Перевод", "Выбытие",
         "Исследования", "Присвоение номеров", "Изменение половозрастной группы", "Обработка")]
        public string? Type { get; init; }

        public DailyActionsSortInfoDTO? SortInfo { get; init; }
    }
}