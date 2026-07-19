using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

public partial class BarrenCowDAL
{
    [Column("animal_id")]
    public Guid Id { get; set; }

    [Column("tag_number")] 
    public string TagNumber { get; set; } = null!;

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("is_barren")]
    public bool IsBarren { get; set; }
}