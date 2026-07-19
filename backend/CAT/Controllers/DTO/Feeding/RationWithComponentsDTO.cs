using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.Controllers.DTO.Feeding
{
    public class RationWithComponentsDTO
    {
        public Guid RationId { get; set; }
        public string RationName { get; set; }
        public string? RationDescription { get; set; }
        public DateTime CreatedAt { get; set; }

        public Guid ComponentId { get; set; }
        public string ComponentName { get; set; }
        public double Kg { get; set; }
        public double? Cost { get; set; }

        public int? SV { get; set; }
        public int? SP { get; set; }
        public float? CEP { get; set; }
        public int? NDK { get; set; }
    }


    public class RationGroupedDTO
    {
        public Guid RationId { get; set; }
        public string RationName { get; set; }
        public string? RationDescription { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RationComponentDTO> Components { get; set; }
        public double TotalCost { get; set; }
        [NotMapped]
        public string[] GroupNames { get; set; }
    }

    public class RationComponentDTO
    {
        public Guid ComponentId { get; set; }
        public string ComponentName { get; set; }
        public double Kg { get; set; }
        public double? Cost { get; set; }
        public int? SV { get; set; }
        public int? SP { get; set; }
        public float? CEP { get; set; }
        public int? NDK { get; set; }
    }



}
