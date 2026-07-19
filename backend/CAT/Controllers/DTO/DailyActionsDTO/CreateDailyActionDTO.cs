using System.ComponentModel.DataAnnotations;
using CAT.Logic;

namespace CAT.Controllers.DTO
{
    public class CreateDailyActionDTO
    {
        public Guid AnimalId { get; init; }

        /// <summary>
        /// Тип ежедневного действия
        /// </summary>
        /// <example>Осмотры</example>
        [IsIn("Осмотры", "Обработка", "Вакцинации и обработки", "Лечение", "Перевод", "Выбытие",
         "Исследования", "Присвоение номеров", "Изменение половозрастной группы")]
        public string? Type { get; init; }

        public string? Subtype { get; init; }

        public string? PerformedBy { get; init; }

        public string? Result { get; init; }

        public string? Medicine { get; init; }

        public string? Dose { get; init; }

        public string? Notes { get; init; }

        public Guid? OldGroupId { get; init; }

        public Guid? NewGroupId { get; init; }

        public string? OldType { get; init; }

        public string? NewType { get; init; }

        public DateOnly? Date { get; init; }

        public DateOnly? NextDate { get; init; }

        public string? ResearchName { get; init; }

        public string? MaterialType { get; init; }

        public string? IdentificationValue { get; init; }

    }

    public class CreateDailyActionsWithMedicineBatchDTO
    {
        public List<Guid> AnimalIds { get; init; } = new();
        public List<DailyActionMedicineItemDTO> Actions { get; init; } = new();
    }

    public class DailyActionMedicineItemDTO
    {
        [IsIn("Осмотры", "Обработка", "Вакцинации и обработки", "Лечение", "Перевод", "Выбытие",
            "Исследования", "Присвоение номеров", "Изменение половозрастной группы")]
        public string? Type { get; init; }

        public string? Subtype { get; init; }
        public string? PerformedBy { get; init; }
        public string? Result { get; init; }
        public string? Medicine { get; init; }
        public string? Dose { get; init; }
        public string? Notes { get; init; }
        public DateOnly? Date { get; init; }
        public DateOnly? NextDate { get; init; }
        public string? MaterialType { get; init; }

        public string? DrugEliminationPeriod { get; init; }
    }
}