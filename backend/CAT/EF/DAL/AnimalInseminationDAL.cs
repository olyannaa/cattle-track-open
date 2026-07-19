using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    public class AnimalInseminationDAL
    {
        [Column("id")]
        public Guid Id { get; set; }  // Идентификатор осеменения

        [Column("cow_id")]
        public Guid CowId { get; set; }  // Идентификатор коровы

        [Column("date")]
        public DateOnly Date { get; set; }  // Дата осеменения

        [Column("insemination_type")]
        public string? InseminationType { get; set; }  // Тип осеменения

        [Column("sperm_batch")]
        public string? SpermBatch { get; set; }  // Номер партии спермы

        [Column("sperm_manufacturer")]
        public string? SpermManufacturer { get; set; }  // Производитель спермы

        [Column("bull_id")]
        public Guid? BullId { get; set; }  // Идентификатор быка

        [Column("bull_tag_number")]
        public string? BullTagNumber { get; set; }  // Тег быка

        [Column("embryo_id")]
        public string? EmbryoId { get; set; }  // Идентификатор эмбриона

        [Column("embryo_manufacturer")]
        public string? EmbryoManufacturer { get; set; }  // Производитель эмбриона

        [Column("technician")]
        public string? Technician { get; set; }  // Техник

        [Column("notes")]
        public string? Notes { get; set; }  // Примечания

        // Дополнительные свойства, если необходимо для вашей логики
    }

}
