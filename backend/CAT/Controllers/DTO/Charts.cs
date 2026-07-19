namespace CAT.Controllers.DTO
{
    public sealed class ChartWeightPointDTO
    {
        public DateOnly Date { get; set; }
        public double? Weight { get; set; } 
    }

    public sealed class ChartEventPointDTO
    {
        public DateOnly Date { get; set; }         
        public string Kind { get; set; } = null!;  
        public long Value { get; set; }            
    }

    public sealed class ChartBirthWeightPointDTO
    {
        public string Kind { get; set; } = null!; 
        public double? Avg { get; set; }          // средний вес
        public double? Max { get; set; }          // максимальный вес
    }

    public sealed class ChartDiagnosisPercentPointDTO
    {
        public string Diagnosis { get; set; } = null!; // research_name
        public string Kind { get; set; } = null!;      // "Положительные" / "Отрицательные"
        public double Value { get; set; }              // процент 0..100
    }
    public sealed class VaccinationMedicineDTO
    {
        public Guid MedicineId { get; set; }
        public string MedicineName { get; set; } = null!;
    }



    public enum ChartGrouping
    {
        Week,
        Month,
        Quarter,
        Year
    }

}
