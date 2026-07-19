using CAT.Controllers.DTO;

namespace CAT.Services.Interfaces
{
    public interface IStatisticsChartsService
    {
        List<ChartWeightPointDTO> GetDailyWeightGainChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo);
        List<ChartWeightPointDTO> GetWeightAt12MonthsChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo);

        List<ChartEventPointDTO> GetCalvingsChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo);
        List<ChartEventPointDTO> GetPregnancyChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo);
        List<ChartEventPointDTO> GetVaccinationsChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo);
        List<ChartEventPointDTO> GetBloodTestsChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo);
        List<ChartBirthWeightPointDTO> GetBirthWeightChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo);
        List<ChartDiagnosisPercentPointDTO> GetBloodTestsDiagnosisPercentChart(Guid organizationId, DateOnly dateFrom, DateOnly dateTo);
        Dictionary<Guid, string> GetVaccinationMedicinesMap(Guid organizationId, DateOnly dateFrom, DateOnly dateTo);
    }

}
