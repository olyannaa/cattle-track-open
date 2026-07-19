namespace CAT.Controllers.DTO.Medicine
{
    public class UpdateMedicineDTO
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Substance { get; set; }
        public string? DrugEliminationPeriod { get; set; }
        public string? ShelfLife { get; set; }
        public string? Factory { get; set; }
    }
}
