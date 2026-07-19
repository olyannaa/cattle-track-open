using CAT.Logic;

namespace CAT.Controllers.DTO;
public class WeightsSortInfoDTO : BaseSortInfoDTO
{
        [IsIn("Date", "Weight", "SUP", "Age")]
        public override string? Column { get; init; } = default;
}