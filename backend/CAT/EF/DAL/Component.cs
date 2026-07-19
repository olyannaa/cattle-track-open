using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    [Table("components")]
    public class Component
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Column("cost")]
        public double? Cost { get; set; }

        [Column("sv")]
        public int? SV { get; set; }

        [Column("sp")]
        public int? SP { get; set; }

        [Column("cep")]
        public float? CEP { get; set; }

        [Column("ndk")]
        public int? NDK { get; set; }
    }
}
