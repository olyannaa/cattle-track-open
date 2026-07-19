using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    public class AnimalWeightDAL
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("animal_id")]
        public Guid AnimalId { get; set; }

        [Column("tag_number")]
        public string TagNumber { get; set; }

        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("birth_date")]
        public DateOnly? BirthDate { get; set; }

        [Column("age")]
        public int? AgeInMonths { get; set; }

        [Column("weighing_date")]
        public DateOnly WeighingDate { get; set; }

        [Column("weight")]
        public float Weight { get; set; }

        [Column("method")]
        public string? MeasurementMethod { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }
    }
}
