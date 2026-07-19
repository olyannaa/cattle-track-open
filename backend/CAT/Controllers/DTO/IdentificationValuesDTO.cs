using CAT.Logic;

namespace CAT.Controllers.DTO
{
    public class IdentificationValuesDTO
    {
        /// <summary>
        /// Id группы животного
        /// </summary>
        public Guid IdentificationId { get; init; }

        public IdentificationValuesFilterDTO? Filter { get; init; }
    }
}