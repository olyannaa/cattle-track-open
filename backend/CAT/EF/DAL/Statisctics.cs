namespace CAT.EF.DAL
{
    public sealed class CalvingWithStatisticsDTO
    {
        public Guid CalvingId { get; set; }
        public Guid CowId { get; set; }
        public Guid? CalfId { get; set; } 
        public DateOnly CalvingDate { get; set; }
        public string CalvingType { get; set; } = null!;

        public long TotalCalvings { get; set; }
        public long LiveCount { get; set; }
        public long AbortCount { get; set; }
        public long StillbornCount { get; set; }

        public decimal? LiveRatio { get; set; }
        public decimal? AbortRatio { get; set; }
        public decimal? StillbornRatio { get; set; }
    }

    public sealed class BirthWeightStatisticsDTO
    {
        public string Sex { get; set; } = null!; 
        public long AnimalCount { get; set; }
        public decimal? AvgWeight { get; set; }
        public decimal? MinWeight { get; set; }
        public decimal? MaxWeight { get; set; }
        public long TotalAnimals { get; set; }
        public decimal? Ratio { get; set; }
    }

    public sealed class DailyWeightGainStatisticsDTO
    {
        public Guid AnimalId { get; set; }
        public DateOnly WeighDate { get; set; }
        public double? Weight { get; set; }
        public double? PrevWeight { get; set; }
        public int? DaysDiff { get; set; }
        public decimal? DailyGain { get; set; }

        public decimal? AvgDailyGain { get; set; }
        public decimal? MinDailyGain { get; set; }
        public decimal? MaxDailyGain { get; set; }
    }

    public sealed class WeightAt12MonthsStatisticsDTO
    {
        public Guid AnimalId { get; set; }
        public DateOnly BirthDate { get; set; }
        public DateOnly TargetDate { get; set; }
        public DateOnly WeighDate { get; set; }
        public double? Weight { get; set; }

        public long AnimalCount { get; set; }
        public decimal? AvgWeight { get; set; }
        public decimal? MinWeight { get; set; }
        public decimal? MaxWeight { get; set; }
    }

    public sealed class PregnancyStatisticsDTO
    {
        public Guid PregnancyId { get; set; }
        public Guid CowId { get; set; }
        public DateOnly PregnancyDate { get; set; }
        public string Status { get; set; } = null!;
        public DateOnly? ExpectedCalvingDate { get; set; }

        public long TotalRecords { get; set; }
        public long StatusCount { get; set; }
        public decimal? StatusRatio { get; set; }
    }

    public sealed class VaccinationStatisticsDTO
    {
        public Guid ActionId { get; set; }
        public Guid AnimalId { get; set; }
        public DateOnly ActionDate { get; set; }
        public string? Medicine { get; set; }
        public string? PerformedBy { get; set; }
        public string? Notes { get; set; }

        public long TotalVaccinations { get; set; }
        public long MedicineCount { get; set; }
        public decimal? MedicineRatio { get; set; }
    }

    public sealed class BloodTestStatisticsDTO
    {
        public Guid ResearchId { get; set; }
        public Guid AnimalId { get; set; }
        public string ResearchName { get; set; } = null!;
        public DateOnly CollectionDate { get; set; }
        public string? Result { get; set; } 

        public long TotalTests { get; set; }
        public long PositiveCount { get; set; }
        public long NegativeCount { get; set; }
        public decimal? PositiveRatio { get; set; }
        public decimal? NegativeRatio { get; set; }
    }

}
