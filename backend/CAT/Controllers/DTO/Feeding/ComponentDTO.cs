using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace CAT.Controllers.DTO.Feeding
{
    public class ComponentDTO
    {
        [Column("component_id")]
        public Guid Id { get; set; }
        [Column("component_name")]
        public string Name { get; set; }
        public double? Cost { get; set; }
        public double? SV { get; set; }
        public double? SP { get; set; }
        public double? CEP { get; set; }
        public double? NDK { get; set; }
        [NotMapped]
        public bool? InRation { get; set; }
    }



}
