using System.Security.Claims;
using CAT.Controllers.DTO;
using CAT.Controllers.DTO.Feeding;
using CAT.EF;
using ClosedXML.Excel;

namespace CAT.Services
{
    public class FeedingExportService
    {
        private readonly PostgresContext _context;
        private readonly IHttpContextAccessor _hc;
        private readonly UserActionQueue _actionQueue;

        public FeedingExportService(PostgresContext context, IHttpContextAccessor hc, UserActionQueue actionQueue)
        {
            _context = context;
            _hc = hc;
            _actionQueue = actionQueue;
        }

        public byte[] GenerateFeedingXlsx(Guid organizationId)
        {
            var records = _context.GetFeedingDailyRecords(organizationId);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetFeedingDailyRecords))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "export",
                    dbMethod,
                    recordId: organizationId
                ));
            
            var dataByDate = new Dictionary<DateTime, Dictionary<string, double>>();
            var allGroupNames = new HashSet<string>();

            foreach (var record in records)
            {
                var date = record.EventDate.Date;
                var groupName = record.GroupName ?? "Без группы";

                if (!dataByDate.TryGetValue(date, out var groupDict))
                {
                    groupDict = new Dictionary<string, double>();
                    dataByDate[date] = groupDict;
                }

                if (!groupDict.ContainsKey(groupName))
                {
                    groupDict[groupName] = 0;
                }

                groupDict[groupName] += record.FeedingDetails?.Sum(d => d.FactKg) ?? 0;

                allGroupNames.Add(groupName);
            }

            var sortedGroupNames = allGroupNames.OrderBy(x => x).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Feeding");

            // Заголовки
            worksheet.Cell(1, 1).Value = "Дата";
            for (int i = 0; i < sortedGroupNames.Count; i++)
            {
                worksheet.Cell(1, i + 2).Value = sortedGroupNames[i];
            }

            // Заполнение строк
            int row = 2;
            foreach (var (date, groups) in dataByDate.OrderBy(d => d.Key))
            {
                worksheet.Cell(row, 1).Value = date.ToString("yyyy-MM-dd");

                for (int col = 0; col < sortedGroupNames.Count; col++)
                {
                    var groupName = sortedGroupNames[col];
                    worksheet.Cell(row, col + 2).Value = groups.ContainsKey(groupName) ? groups[groupName] : 0;
                }

                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] GenerateGroupRationFeedingXlsx(Guid organizationId, Guid groupId)
        {
            var records = _context.GetGroupFeedingStats(organizationId, groupId);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Group Ration Feeding");
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupFeedingStats))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "export",
                    dbMethod,
                    recordId: organizationId
                ));
            
            // Заголовки
            worksheet.Cell(1, 1).Value = "Дата";
            worksheet.Cell(1, 2).Value = "Потребление";
            worksheet.Cell(1, 3).Value = "Рацион";

            int row = 2;
            foreach (var record in records.OrderBy(r => r.EventDate))
            {
                worksheet.Cell(row, 1).Value = record.EventDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 3).Value = record.RationName ?? "Без рациона";
                worksheet.Cell(row, 2).Value = record.DailyFactKg + "кг";
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] GenerateGroupRationFeedingCostXlsx(Guid organizationId, Guid groupId)
        {
            var records = _context.GetGroupFeedingStatsCost(organizationId, groupId);

            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupFeedingStatsCost))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "export",
                    dbMethod,
                    recordId: organizationId
                ));
            
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("FeedingCost");

            
            worksheet.Cell(1, 1).Value = "Дата";
            worksheet.Cell(1, 2).Value = "Рацион";
            worksheet.Cell(1, 3).Value = "Стоимость";
            worksheet.Cell(1, 4).Value = "Общая стоимость";

            int row = 2;
            foreach (var record in records.OrderBy(r => r.EventDate))
            {
                worksheet.Cell(row, 1).Value = record.EventDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 2).Value = record.GroupRationName;
                worksheet.Cell(row, 3).Value = record.RationCost;
                worksheet.Cell(row, 4).Value = record.TotalRationCost;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] GenerateGroupRationFeedingCostYearlyXlsx(Guid organizationId, Guid groupId)
        {
            var records = _context.GetGroupFeedingStatsCostYearly(organizationId, groupId);

            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupFeedingStatsCostYearly))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "export",
                    dbMethod,
                    recordId: organizationId
                ));
            
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("YearlyCost");

            worksheet.Cell(1, 1).Value = "Месяц";
            worksheet.Cell(1, 2).Value = "Рацион";
            worksheet.Cell(1, 3).Value = "Стоимость рациона (за голову)";
            worksheet.Cell(1, 4).Value = "Общая стоимость";

            int row = 2;
            foreach (var group in records)
            {
                worksheet.Cell(row, 1).Value = group.MonthYear;
                worksheet.Cell(row, 2).Value = group.GroupRationName;
                worksheet.Cell(row, 3).Value = group.RationCost;
                worksheet.Cell(row, 4).Value = group.TotalRationCost;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] GenerateGroupRationNutritionXlsx(Guid organizationId, Guid groupId)
        {
            var records = _context.GetGroupFeedingStatsNutrition(organizationId, groupId);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupFeedingStatsNutrition))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "export",
                    dbMethod,
                    recordId: organizationId
                ));
            
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Nutrition");

            worksheet.Cell(1, 1).Value = "Дата";
            worksheet.Cell(1, 2).Value = "Рацион";
            worksheet.Cell(1, 3).Value = "SV";
            worksheet.Cell(1, 4).Value = "SP";
            worksheet.Cell(1, 5).Value = "CEP";
            worksheet.Cell(1, 6).Value = "NDK";

            int row = 2;
            foreach (var record in records)
            {
                worksheet.Cell(row, 1).Value = record.EventDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 2).Value = record.GroupRationName;
                worksheet.Cell(row, 3).Value = record.TotalSv;
                worksheet.Cell(row, 4).Value = record.TotalSp;
                worksheet.Cell(row, 5).Value = record.TotalCep;
                worksheet.Cell(row, 6).Value = record.TotalNdk;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] GenerateGroupFeedingStatsXlsx(List<GroupFeedingStatsDTO> records)
        {
            using var workbook = new XLWorkbook();
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupFeedingStatsNutrition))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "export",
                    dbMethod
                ));
            
            var worksheet = workbook.Worksheets.Add("Group Stats");
            
            worksheet.Cell(1, 1).Value = "Группа";
            worksheet.Cell(1, 2).Value = "Кол-во животных";
            worksheet.Cell(1, 3).Value = "Рацион";
            worksheet.Cell(1, 4).Value = "Утро";
            worksheet.Cell(1, 5).Value = "День";
            worksheet.Cell(1, 6).Value = "Ночь";
            worksheet.Cell(1, 7).Value = "Кг на голову";
            worksheet.Cell(1, 8).Value = "Кг на группу";

            int row = 2;
            foreach (var record in records)
            {
                worksheet.Cell(row, 1).Value = record.GroupName;
                worksheet.Cell(row, 2).Value = record.AnimalCount;
                worksheet.Cell(row, 3).Value = record.GroupRationName ?? "-";
                worksheet.Cell(row, 4).Value = record.MorningFeeding;
                worksheet.Cell(row, 5).Value = record.DayFeeding;
                worksheet.Cell(row, 6).Value = record.NightFeeding;
                worksheet.Cell(row, 7).Value = record.TotalKg;
                worksheet.Cell(row, 8).Value = record.TotalKgForGroup;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
