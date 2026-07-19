using CAT.Logic;

namespace CAT.Controllers.DTO
{
    public class DailyAnimalsFilterDTO
    {
        /// <summary>
        /// Id группы животного
        /// </summary>
        /// <example>e927c543-d2f4-44d6-bd3a-2e26b41e03ef</example>
        public Guid? GroupId { get; init; }

        /// <summary>
        /// Тип животного
        /// </summary>
        /// <example>Нетель</example>
        [IsIn("Корова", "Бык", "Бычок", "Нетель", "Телка", null)]
        public string? Type { get; init; }

        /// <summary>
        /// Номер животного
        /// </summary>
        /// <example>gh678</example>
        public string? TagNumber { get; init; }

        /// <summary>
        /// Только активные животные
        /// </summary>
        /// <example>true</example>
        public bool? IsActive { get; init; }

        /// <summary>
        /// Идентификация?
        /// </summary>
        public IdentificationFieldIdDTO? IdentificationField { get; init; }
    }
}