using System;
using System.Collections.Generic;

namespace CAT.EF.DAL;

public partial class IdentificationField
{
    public Guid Id { get; set; }

    public string? FieldName { get; set; }


    public Guid? OrganizationId { get; set; }

    public virtual ICollection<AnimalIdentification> AnimalIdentifications { get; set; } = new List<AnimalIdentification>();

    public virtual Organization? Organization { get; set; }
}
