using System.ComponentModel.DataAnnotations;
using CAT.Logic;

namespace CAT.Controllers.DTO;

public class AnimalCensusQueryDTO
{
    [Required, GreaterThan(0)]
    public int Page { get; init; }

    public CensusSortInfoDTO SortInfo { get; init; } = new();
    
    public AnimalFiltersDTO Filters { get; init; } = new();
}