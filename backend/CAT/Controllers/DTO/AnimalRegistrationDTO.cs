using CAT.Controllers.DTO.Attributes;
using CAT.EF.DAL;
using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CAT.Controllers.DTO
{
    public class AnimalRegistrationDTO
    {
        [Required]
        public string TagNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateOnly BirthDate { get; set; }

        [Required]
        public string Type { get; set; }
        public string? Breed { get; set; } = null!;
        public string? Status { get; set; } = null!;
        public Guid? MotherTag { get; set; } = null!;
        public Guid? FatherTag { get; set; } = null!;
        public Guid? MotherId { get; set; } = null!;
        public Guid? FatherId { get; set; } = null!;
        public Guid? GroupId { get; set; } = null!;
        public string? Origin { get; set; } = null!;
        public string? OriginLocation { get; set; } = null!;
        public IFormFile? Photo { get; set; } = null!;
        public Dictionary<Guid, string>? AdditionalFields { get; set; } = new();

        //if netel
        [Format("dd.MM.yyyy")]
        public DateOnly InseminationDate { get; set; } = new DateOnly();
        [Format("dd.MM.yyyy")]
        public DateOnly? ExpectedCalvingDate { get; set; } = new DateOnly();
        public string? InseminationType { get; set; } = null;
        public string? SpermBatch { get; set; } = null;
        public string? Technician { get; set; } = null;
        public string?  Notes { get; set; } = null;
        /// <summary>
        /// Айди быка
        /// </summary>
        public Guid? BullId { get; set; } = null;
        /// <summary>
        /// Тэг ембриона
        /// </summary>
        public string? EmbryoTag { get; set; } = null;
        /// <summary>
        /// Производитель эмбриона
        /// </summary>
        public string? EmbryoManufacturer { get; set; } = null;
        /// <summary>
        /// Производитель
        /// </summary>
        public string? SpermManufacturer { get; set; } = null;

    }
}
