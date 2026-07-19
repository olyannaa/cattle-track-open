using CAT.Logic;

namespace CAT.Controllers.DTO;
public class BaseSortInfoDTO
{
        /// <summary>
        /// Название колонки по сортировке
        /// </summary>
        /// <example>TagNumber</example>
        public virtual string? Column { get; init; } = default;

        /// <summary>
        /// Сортировать ли по убыванию
        /// </summary>
        /// <example>true</example>
        public virtual bool Descending { get; init; } = default;
}