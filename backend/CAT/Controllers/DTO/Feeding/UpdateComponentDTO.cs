namespace CAT.Controllers.DTO.Feeding
{
    public class UpdateComponentDTO
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public double? Cost { get; set; }
        public int? SV { get; set; }
        public int? SP { get; set; }
        public float? CEP { get; set; }
        public int? NDK { get; set; }
    }

}
