using CAT.Logic;

namespace CAT.Controllers.DTO;
public class CensusSortInfoDTO : BaseSortInfoDTO
{
        /// <summary>
        /// Отображать ли только активных животных
        /// </summary>
        /// <example>true</example>
        public bool Active { get; init; } = default;

        /// <summary>
        /// Название колонки по сортировке
        /// </summary>
        /// <example>TagNumber</example>
        [IsIn("TagNumber", "BirthDate", "Breed", "GroupName", "Status", "Origin",
                "OriginLocation", "MotherTagNumber", "FatherTagNumber", "DateOfReceipt",
                "DateOfDisposal", "ReasonOfDisposal", "Consumption", "LiveWeihtAtDisposal",
                "LastWeightDate", "LastWeightWeight", "IdentificationFieldName", "IdentificationValue")]
        public override string? Column { get; init; } = default;
}