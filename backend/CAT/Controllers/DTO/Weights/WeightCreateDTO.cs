using CAT.Logic;
using Swashbuckle.AspNetCore.Annotations;

namespace CAT.Controllers.DTO;

public class WeightCreateDTO : InsertAnimalWeightDTO
{
    [SwaggerIgnore]
    public override Guid Id { get; set; } = Guid.NewGuid();
}