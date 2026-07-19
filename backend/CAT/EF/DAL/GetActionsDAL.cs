using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL;

public partial class GetActionsDAL
{
    [Column("action_id")]
    public Guid Id { get; set; }

    [Column("animal_id")]
    public Guid? AnimalId { get; set; }

    [Column("animal_tag_number")]
    public string? TagNumber { get; set; }

    [Column("action_type")]
    public string? Type { get; set; }

    [Column("action_subtype")]
    public string? Subtype { get; set; }

    [Column("performed_by")]
    public string? PerformedBy { get; set; }

    [Column("action_result")]
    public string? Result { get; set; }

    [Column("action_medicine")]
    public string? Medicine { get; set; }

    [Column("action_dose")]
    public string? Dose { get; set; }

    [Column("action_notes")]
    public string? Notes { get; set; }
    
    [Column("old_group_id")]
    public Guid? OldGroupId { get; set; }

    [Column("old_group_name")]
    public string? OldGroupName { get; set; }

    [Column("new_group_id")]
    public Guid? NewGroupId { get; set; }

    [Column("new_group_name")]
    public string? NewGroupName { get; set; }

    [Column("old_type")]
    public string? OldType { get; set; }

    [Column("new_type")]
    public string? NewType { get; set; }
    
    [Column("action_date")]
    public DateOnly? Date { get; set; }

    [Column("next_action_date")]
    public DateOnly? NextDate { get; set;}

    [Column("created_at")]
    public DateTime? CreatedAt { get; set;}

    [NotMapped]
    public Guid? MedicineId { get; set; }         

    [NotMapped]
    public string? DrugEliminator { get; set; }

    [NotMapped]
    public DateOnly? WithdrawalUntil { get; set; }
}