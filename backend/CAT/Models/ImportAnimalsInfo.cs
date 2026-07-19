namespace CAT.Models
{
    public class ImportAnimalsInfo
    {
        public int TotalRows { get; set; } = 0;
        public int Imported { get; set; } = 0;
        public int Skipped { get; set; } = 0;
        public int Duplicates { get; set; } = 0;
        public int Errors { get; set; } = 0;
        public Dictionary<string, int> ByType { get; set; } = new Dictionary<string, int>
            {
                { "Корова", 0 },
                { "Нетель", 0 },
                { "Бык", 0 },
                { "Телка", 0 },
                { "Бычок", 0 },
                { "Неопределен", 0 }
            };
        public List<string> SkippedReasons { get; set; } = new List<string>();
        public int CreatedFields { get; set; } = 0;
        public List<string> FieldNames { get; set; } = new List<string>();
        public string Message { get; set; } = "";
    }
}
