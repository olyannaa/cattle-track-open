using CAT.Logic;

namespace CAT.Controllers.DTO;
public class DailyActionsSortInfoDTO : BaseSortInfoDTO
{      
        [IsIn("TagNumber", "Subtype", "Type", "PerformedBy", "Result", "Medicine",
                "Dose", "Notes", "OldGroupName", "NewGroupName", "Date", "NextDate",
                "MaterialType", "CollectionDate", "CollectedBy")]
        public override string? Column { get; init; } = default;
}