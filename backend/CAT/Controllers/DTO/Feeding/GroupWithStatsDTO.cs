using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class GroupWithStatsDTO
    {
        public Guid GroupId { get; set; }
        public string? GroupName { get; set; }
        public long? ActiveAnimalsCount { get; set; }
        public double? MorningFeeding { get; set; }
        public double? DayFeeding { get; set; }
        public double? NightFeeding { get; set; }

        public Guid? RationId { get; set; }              
        public string? RationName { get; set; }

        public double? RationCostPerHead { get; set; }
        public double? TotalRationCost { get; set; }
        public int? SvPerHead { get; set; }
        public int? SpPerHead { get; set; }
        public double? CepPerHead { get; set; }
        public int? NdkPerHead { get; set; }
        public int? TotalSv { get; set; }
        public int? TotalSp { get; set; }
        public double? TotalCep { get; set; }
        public int? TotalNdk { get; set; }
    }

}
