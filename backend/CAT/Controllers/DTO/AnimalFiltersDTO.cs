public class AnimalFiltersDTO
{
    public string? TagNumber { get; init; }

    // Типы: если null/empty => не фильтруем (то есть "все")
    public List<string>? Types { get; init; }

    public DateOnly? BirthDateFrom { get; init; }
    public DateOnly? BirthDateTo { get; init; }

    // Порода/группа у тебя строками
    public List<string>? Breeds { get; init; }
    public List<string>? GroupNames { get; init; }

    // Статусы строками (Активное/Неактивное/Выбывшее)
    public List<string>? Statuses { get; init; }

    public List<string>? Origins { get; init; }
    public List<string>? OriginLocations { get; init; }

    public string? MotherTagNumber { get; init; }

    // "№ отца" — у тебя список в JSON -> фильтруем после группировки по FatherTagNumbersList
    public string? FatherTagNumber { get; init; }
}