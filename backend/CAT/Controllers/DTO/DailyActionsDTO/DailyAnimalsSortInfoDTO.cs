using CAT.Logic;

namespace CAT.Controllers.DTO;
public class DailyAnimalsSortInfoDTO : BaseSortInfoDTO
{
        [IsIn("TagNumber", "Type", "Status", "GroupName")]
        public override string? Column { get; init; } = default;
}