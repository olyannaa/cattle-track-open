
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL;

public partial class AnimalWeightsDAL
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("animal_id")]
    public Guid? AnimalId { get; set; }

    [Column("weighing_date")]
    public DateOnly? Date { get; set; }

    [Column("birth_date")]
    public DateOnly? BirthDate { get; set; }

    [Column("age")]
    public int? Age { get; set; }

    [Column("weight")]
    public double? Weight { get; set; }

    [Column("method")]
    public string? Method { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("sup")]
    public double? SUP { get; set; }
}
