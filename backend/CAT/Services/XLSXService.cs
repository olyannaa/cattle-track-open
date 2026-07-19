using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using ClosedXML.Excel;
using CAT.Services.Interfaces;
using CAT.Controllers.DTO;

namespace CAT.Services
{
    public class XLSXService : ISpreadsheetService
    {
        private readonly UserActionQueue _actionQueue;
        private readonly IHttpContextAccessor _hc;

        public XLSXService(UserActionQueue actionQueue, IHttpContextAccessor hc)
        {
            _actionQueue = actionQueue;
            _hc = hc;
        }

        public IEnumerable<T> Read<T>(Stream file)
        {
            using var workbook = new XLWorkbook(file);
            var worksheet = workbook.Worksheets.First();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // пропускаем заголовок
            var headers = worksheet.Row(1).Cells().Select(c => c.Value.ToString().Trim()).ToList();

            var records = new List<T>();
            foreach (var row in rows)
            {
                var instance = Activator.CreateInstance<T>();
                foreach (var prop in props)
                {
                    var headerName = headers.FirstOrDefault(h => 
                        string.Equals(h, prop.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (headerName != null)
                    {
                        var cellValue = row.Cell(headers.IndexOf(headerName) + 1).GetString();
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            prop.SetValue(instance, Convert.ChangeType(cellValue, prop.PropertyType, CultureInfo.InvariantCulture));
                        }
                    }
                }
                records.Add(instance);
            }
            
            return records;
        }

        public IEnumerable<AnimalInfoDTO> ReadAnimals(Stream file)
        {
            using var workbook = new XLWorkbook(file);
            var worksheet = workbook.Worksheets.First();

            var headers = worksheet.Row(1).Cells().Select(c => c.Value.ToString().Trim()).ToList();
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

            // список известных столбцов
            var knownHeaders = typeof(AnimalInfoDTO)
                .GetProperties()
                .SelectMany(prop => prop.GetCustomAttributes(true)
                    .OfType<CsvHelper.Configuration.Attributes.NameAttribute>())
                .SelectMany(attr => attr.Names)
                .ToHashSet();

            var records = new List<AnimalInfoDTO>();

            foreach (var row in rows)
            {
                var animal = new AnimalInfoDTO();
                animal.AdditionalFields = new Dictionary<string, string>();

                foreach (var header in headers)
                {
                    var cellValue = row.Cell(headers.IndexOf(header) + 1).GetString();
                    if (knownHeaders.Contains(header))
                    {
                        var prop = typeof(AnimalInfoDTO).GetProperties()
                            .FirstOrDefault(p =>
                                p.GetCustomAttributes(true)
                                 .OfType<CsvHelper.Configuration.Attributes.NameAttribute>()
                                 .Any(a => a.Names.Contains(header)));

                        if (prop != null)
                        {
                            prop.SetValue(animal, Convert.ChangeType(cellValue, prop.PropertyType, CultureInfo.InvariantCulture));
                        }
                    }
                    else
                    {
                        animal.AdditionalFields[header] = cellValue;
                    }
                }

                records.Add(animal);
            }
            
            return records;
        }

        public byte[] Write<T>(IEnumerable<T> items)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sheet1");

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Заголовки
            for (int i = 0; i < props.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = props[i].Name;
            }

            // Данные
            int row = 2;
            foreach (var item in items)
            {
                for (int col = 0; col < props.Length; col++)
                {
                    var value = props[col].GetValue(item);
                    worksheet.Cell(row, col + 1).Value = value?.ToString() ?? string.Empty;
                }
                row++;
            }

            // Авторазмер колонок
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),"export"));

            return stream.ToArray();
        }

        public string GetFileName(string input)
        {
            var time = DateTime.UtcNow.ToString("dd-MM-yyyyTHH-mm-ss");
            if (input == "Корова") input = "Cows";
            else if (input == "Бык" || input == "Бычок") input = "Bulls";
            else if (input == "Телка" || input == "Нетель") input = "Heifers";
            return $"{input} {time}.xlsx";
        }
    }
}
