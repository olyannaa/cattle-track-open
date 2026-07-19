using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CAT.EF.DAL;

public partial class Animal
{
    [Column("id"), Key]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [Column("tag_number")]
    public string? TagNumber { get; set; }

    [Column("type")]
    public string? Type { get; set; }

    [Column("breed")]
    public string? Breed { get; set; }

    [Column("mother_id")]
    public Guid? MotherId { get; set; }

    [Column("father_id", TypeName = "jsonb")]
    public JsonElement? FatherJson { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("group_id")]
    public Guid? GroupId { get; set; }

    [Column("origin")]
    public string? Origin { get; set; }

    [Column("origin_location")]
    public string? OriginLocation { get; set; }

    [Column("birth_date")]
    public DateOnly? BirthDate { get; set; }

    [Column("date_of_receipt")]
    public DateOnly? DateOfReceipt { get; set; }

    [Column("date_of_disposal")]
    public DateOnly? DateOfDisposal { get; set; }

    [Column("reason_of_disposal")]
    public string? ReasonOfDisposal { get; set; }

    [Column("consumption")]
    public string? Consumption { get; set; }

    [Column("live_weight_at_disposal")]
    public double? LiveWeightAtDisposal { get; set; }

    [Column("last_weigh_date")]
    public DateOnly? LastWeighDate { get; set; }

    [Column("last_weight_weight")]
    public string? LastWeightWeight { get; set; }

    public virtual ICollection<AnimalIdentification> AnimalIdentifications { get; set; } = new List<AnimalIdentification>();

    public virtual Group? Group { get; set; }

    public virtual Organization? Organization { get; set; }
}
