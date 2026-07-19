using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL;

public partial class GetResearchDAL
{
    [Column("research_id")]
    public Guid Id { get; set; }

    [Column("animal_id")]
    public Guid? AnimalId { get; set; }

    [Column("animal_tag_number")]
    public string? TagNumber { get; set; }

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [Column("research_name")]
    public string? Name { get; set; }

    [Column("material_type")]
    public string? MaterialType { get; set; }

    [Column("collection_date")]
    public DateOnly? CollectionDate { get; set; }

    [Column("collected_by")]
    public string? CollectedBy { get; set; }

    [Column("research_result")]
    public string? Result { get; set;}

    [Column("research_notes")]
    public string? Notes { get; set;}

    [Column("created_at")]
    public DateTime? CreatedAt { get; set;}
}