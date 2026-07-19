using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.Controllers.DTO
{
    public class DailyActionAnimalCardDTO
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("animal_id")]
        public Guid AnimalId { get; set; }

        //Тип события
        [Column("action_type")]
        public string ActionType { get; set; }

        //Подтип события
        [Column("action_subtype")]
        public string? ActionSubtype { get; set; }

        [Column("action_date")]  
        public DateOnly ActionDate { get; set; }

        [Column("performed_by")]
        public string? PerformedBy { get; set; }

        [Column("result")]
        public string? Result { get; set; }

        [Column("medicine")]
        public string? Medicine { get; set; }

        [Column("dose")]
        public string? Dose { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("next_action_date")]
        public DateOnly? NextActionDate { get; set; }

        [Column("old_group_id")]
        public Guid? OldGroupId { get; set; }

        [Column("new_group_id")]
        public Guid? NewGroupId { get; set; }
    }
}
