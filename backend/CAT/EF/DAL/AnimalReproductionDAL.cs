using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    public class AnimalReproductionDAL
    {
        [Column("id")]
        public Guid? Id { get; set; }

        [Column("cow_id")]
        public Guid CowId { get; set; }

        [Column("calving_date")]
        public DateOnly CalvingDate { get; set; }

        [Column("complication")]
        public string? Complication { get; set; }

        [Column("calving_type")]  
        public string? CalvingType { get; set; }

        [Column("veterinar")]
        public string? Veterinarian { get; set; }  

        [Column("treatments")]
        public string? Treatments { get; set; }

        [Column("pathology")]
        public string? Pathology { get; set; }

        [Column("calf_id")]
        public Guid? CalfId { get; set; }

        [Column("calf_tag_number")] 
        public string? CalfTagNumber { get; set; }

        [Column("insemination_id")]
        public Guid? InseminationId { get; set; }
    }
}
