using CAT.Controllers.DTO;
using CAT.EF;
using CAT.Services.Interfaces;

public sealed class StatisticsChartsService : IStatisticsChartsService
{
    private const string OutDateFormat = "yyyy-MM-dd";
    private readonly PostgresContext _repo;

    public StatisticsChartsService(PostgresContext repo)
    {
        _repo = repo;
    }

    public List<ChartWeightPointDTO> GetDailyWeightGainChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo)
        => _repo.GetDailyWeightGainStatistics(organizationId, dateFrom, dateTo)
            .Select(x => new ChartWeightPointDTO
            {
                Date = x.WeighDate,
                Weight = x.DailyGain.HasValue ? (double?)x.DailyGain.Value : null
            })
            .OrderBy(x => x.Date)
            .ToList();

    public List<ChartWeightPointDTO> GetWeightAt12MonthsChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo)
        => _repo.GetWeightAt12MonthsStatistics(organizationId, dateFrom, dateTo)
            .Select(x => new ChartWeightPointDTO
            {
                Date = x.TargetDate,
                Weight = x.Weight
            })
            .OrderBy(x => x.Date)
            .ToList();

    public List<ChartEventPointDTO> GetCalvingsChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo)
    => _repo.GetCalvingsWithStatistics(organizationId, dateFrom, dateTo)
        .GroupBy(x => new { x.CalvingDate, x.CalvingType })
        .Select(g => new ChartEventPointDTO
        {
            Date = g.Key.CalvingDate,
            Kind = g.Key.CalvingType,
            Value = g.LongCount()
        })
        .OrderBy(x => x.Date).ThenBy(x => x.Kind)
        .ToList();


    public List<ChartEventPointDTO> GetPregnancyChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo)
    => _repo.GetPregnancyStatistics(organizationId, dateFrom, dateTo)
        .GroupBy(x => new { x.PregnancyDate, x.Status })
        .Select(g => new ChartEventPointDTO
        {
            Date = g.Key.PregnancyDate,
            Kind = g.Key.Status,
            Value = g.LongCount()
        })
        .OrderBy(x => x.Date).ThenBy(x => x.Kind)
        .ToList();


    public List<ChartEventPointDTO> GetVaccinationsChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo)
    {
        var medicineMap = _repo.Medicinies
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => new { x.Id, x.Name })
            .ToDictionary(x => x.Id, x => x.Name);

        return _repo.GetVaccinationStatistics(organizationId, dateFrom, dateTo)
            .Select(x =>
            {
                var kind = "Не указано";

                if (!string.IsNullOrWhiteSpace(x.Medicine))
                {
                    var raw = x.Medicine.Trim();

                    if (Guid.TryParse(raw, out var medId))
                        kind = medicineMap.TryGetValue(medId, out var name) && !string.IsNullOrWhiteSpace(name) ? name : raw;
                    else
                        kind = raw;
                }

                return new { x.ActionDate, Kind = kind };
            })
            .GroupBy(e => new { e.ActionDate, e.Kind })
            .Select(g => new ChartEventPointDTO
            {
                Date = g.Key.ActionDate,
                Kind = g.Key.Kind,
                Value = g.LongCount()
            })
            .OrderBy(x => x.Date).ThenBy(x => x.Kind)
            .ToList();
    }



    public List<ChartEventPointDTO> GetBloodTestsChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo)
        => _repo.GetBloodTestStatistics(organizationId, dateFrom, dateTo)
            .Select(x => new ChartEventPointDTO
            {
                Date = x.CollectionDate,
                Kind = x.ResearchName,
                Value = 1
            })
            .OrderBy(x => x.Date).ThenBy(x => x.Kind)
            .ToList();


    public List<ChartBirthWeightPointDTO> GetBirthWeightChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo)
        => _repo.GetBirthWeightStatistics(organizationId, dateFrom, dateTo)
            .Select(x => new ChartBirthWeightPointDTO
            {
                Kind = x.Sex,
                Avg = x.AvgWeight.HasValue ? (double?)x.AvgWeight.Value : null,
                Max = x.MaxWeight.HasValue ? (double?)x.MaxWeight.Value : null
            })
            .OrderBy(x => x.Kind)
            .ToList();

    public List<ChartDiagnosisPercentPointDTO> GetBloodTestsDiagnosisPercentChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo)
    {
        var raw = _repo.GetBloodTestStatistics(organizationId, dateFrom, dateTo);

        var result = new List<ChartDiagnosisPercentPointDTO>();

        foreach (var g in raw
                     .Where(x => !string.IsNullOrWhiteSpace(x.ResearchName))
                     .GroupBy(x => x.ResearchName))
        {
            var positive = g.LongCount(x => IsPositiveResult(x.Result));
            var negative = g.LongCount(x => IsNegativeResult(x.Result));

            var total = positive + negative;
            if (total == 0)
                continue;

            var posPct = Math.Round(positive * 100.0 / total, 2);
            var negPct = Math.Round(100.0 - posPct, 2); 

            result.Add(new ChartDiagnosisPercentPointDTO
            {
                Diagnosis = g.Key,
                Kind = "Положительные",
                Value = posPct
            });

            result.Add(new ChartDiagnosisPercentPointDTO
            {
                Diagnosis = g.Key,
                Kind = "Отрицательные",
                Value = negPct
            });
        }

        return result
            .OrderBy(x => x.Diagnosis)
            .ThenBy(x => x.Kind == "Положительные" ? 0 : 1)
            .ToList();
    }

    public Dictionary<Guid, string> GetVaccinationMedicinesMap(Guid organizationId, DateOnly dateFrom, DateOnly dateTo)
        => _repo.GetVaccinationMedicines2(organizationId, dateFrom, dateTo)
            .Where(x => x.MedicineId != Guid.Empty)
            .GroupBy(x => x.MedicineId)
            .ToDictionary(g => g.Key, g => g.First().MedicineName);

    private static bool IsPositiveResult(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim().ToLowerInvariant();
        return v is "true" or "t" or "1" or "yes" or "y";
    }

    private static bool IsNegativeResult(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim().ToLowerInvariant();
        return v is "false" or "f" or "0" or "no" or "n";
    }
}