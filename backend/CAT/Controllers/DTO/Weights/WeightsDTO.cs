using System.ComponentModel.DataAnnotations;
using CAT.Logic;

namespace CAT.Controllers.DTO;
public class WeightsDTO
{
        [Required, GreaterThan(0)]
        public int Page { get; init; }
        public WeightsSortInfoDTO? sortInfo { get; init; }
}