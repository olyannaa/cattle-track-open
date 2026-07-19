using System.ComponentModel.DataAnnotations;

namespace CAT.Controllers.DTO
{
    public class UpdateAnimalDTO
    {
        /// <example>d9776ffe-58e9-4ec2-bb03-d1a3f57942b9</example>
        [Required]
        public Guid Id { get; init; }
        /// <example>TAG123</example>
        public string? TagNumber { get; init; }

        /// <example>0fbedbab-83ea-44cc-b7a0-69768dca5750</example>
        public Guid? GroupID { get; init; }

        /// <example>2023-12-31</example>
        public DateOnly? BirthDate { get; init; }

        public string? Breed { get; init; }

        /// <example>Активное</example>
        public string? Status { get; init; }

        public string? Origin { get; init; }

        public string? OriginLocation { get; init; }

        public string? MotherTagNumber { get; init; }

        public string? FatherTagNumber { get; init; }

        public IdentificationFieldNameDTO[]? IdentificationFields{ get; init; }
    }
}
