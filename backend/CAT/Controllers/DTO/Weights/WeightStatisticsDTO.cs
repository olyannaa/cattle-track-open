using System.ComponentModel.DataAnnotations;
using CAT.Logic;

namespace CAT.Controllers.DTO;

public class WeightStatisticsDTO
{
    public IEnumerable<AgeNodeDTO>? DataByAge { get; set; }
    public IEnumerable<DateNodeDTO>? DataByDate { get; set; }
    public IEnumerable<SUPNodeDTO>? DataBySUP { get; set; }
    public double? MinSUP { get; set; }
    public double? MaxSUP { get; set; } 
    public double? MeanSUP { get; set; }
}

public class AgeNodeDTO
{
    public int Age { get; set; }

    public double Weight { get; set; }
}

public class DateNodeDTO
{
    public DateOnly Date { get; set; }

    public double Weight { get; set; }
}

public class SUPNodeDTO
{
    public DateOnly Date { get; set; }

    public double SUP { get; set; }
}