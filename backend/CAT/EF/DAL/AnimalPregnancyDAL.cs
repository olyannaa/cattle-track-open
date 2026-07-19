using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    public class AnimalPregnancyDAL
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("cow_id")]
        public Guid CowId { get; set; }

        [Column("check_date")]
        public DateOnly? Date { get; set; }

        [Column("pregnancy_status")]
        public string? Status { get; set; }

        [Column("expected_calving_date")]
        public DateOnly? ExpectedCalvingDate { get; set; }
    }
}
