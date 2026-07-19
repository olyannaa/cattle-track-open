using CAT.EF.DAL;
using CAT.Logic;

namespace CAT.Controllers.DTO;

public class WeightInfoDTO
{
    public static List<WeightInfoDTO> Parse(IEnumerable<AnimalWeightsDAL> weights)
    {
        return weights.Select(e =>
                    {
                        return new WeightInfoDTO()
                        {
                            Id = e.Id,
                            Date = e.Date,
                            Weight = e.Weight,
                            Age = e.Age,
                            SUP = e.SUP,
                        };
                    })
                    .ToList();
    }

    public Guid Id { get; init; }

    public DateOnly? Date { get; init; }

    public int? Age { get; init; }

    public double? Weight { get; init; }

    public double? SUP { get; init; }
}