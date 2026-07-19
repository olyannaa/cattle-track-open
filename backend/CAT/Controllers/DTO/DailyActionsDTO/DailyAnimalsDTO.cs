using CAT.Logic;

namespace CAT.Controllers.DTO
{
    public class DailyAnimalsDTO
    {
        public DailyAnimalsFilterDTO Filter { get; set; }

        public DailyAnimalsSortInfoDTO? SortInfo { get; init; }

        public int? Page { get; init; }
    }
}