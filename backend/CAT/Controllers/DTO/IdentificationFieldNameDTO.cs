using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO;

public class IdentificationFieldNameDTO
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    public static IdentificationFieldNameDTO[] FromDictionary(Dictionary<string, string>? dict)
    {
        if (dict == null || dict.Count == 0)
            return Array.Empty<IdentificationFieldNameDTO>();
        var list = new List<IdentificationFieldNameDTO>(dict.Count);
        foreach (var kv in dict)
            list.Add(new IdentificationFieldNameDTO { Name = kv.Key, Value = kv.Value });
        return list.ToArray();
    }
}