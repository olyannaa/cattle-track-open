using System;
using System.Collections.Generic;

namespace CAT.EF.DAL;

public partial class AnimalIdentification
{
    public Guid Id { get; set; }

    public Guid? AnimalId { get; set; }

    public Guid? FieldId { get; set; }

    public string? Value { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Animal? Animal { get; set; }

    public virtual IdentificationField? Field { get; set; }
}
