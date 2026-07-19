using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL;

[Table("daily_actions")]
public partial class DailyAction
{
    [Column("id"), Key]
    public Guid Id { get; set; }

    [Column("animal_id")]
    public Guid? AnimalId { get; set; }

    [Column("action_type")]
    public string? Type { get; set; }

    [Column("action_subtype")]
    public string? Subtype { get; set; }

    [Column("performed_by")]
    public string? PerformedBy { get; set; }

    [Column("result")]
    public string? Result { get; set; }

    [Column("medicine")]
    public string? Medicine { get; set; }

    [Column("dose")]
    public string? Dose { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
    
    [Column("old_group_id")]
    public Guid? OldGroupId { get; set; }

    [Column("new_group_id")]
    public Guid? NewGroupId { get; set; }

    [Column("date")]
    public DateOnly? Date { get; set; }

    [Column("next_action_date")]
    public DateOnly? NextDate { get; set;}

    [Column("created_at")]
    public DateTime? CreatedAt { get; set;}

    [ForeignKey("AnimalId")]
    public virtual Animal? Animal { get; set; }

    [ForeignKey("OldGroupId")]
    public virtual Group? OldGroup { get; set; }

    [ForeignKey("NewGroupId")]
    public virtual Group? NewGroup { get; set; }
}