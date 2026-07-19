namespace CAT.Models
{
    public class ChartInfo<TX, TY> where TX : IComparable<TX>
    {
        public List<ChartPoint<TX, TY>> Points { get; set; } = new List<ChartPoint<TX, TY>>();
        public string Title { get; set; }
        public string XAxisLabel { get; set; }
        public string YAxisLabel { get; set; }

        public void Sort() => Points.Sort();

        public void SortDescending() => Points.Sort((a, b) => b.CompareTo(a));

        public void AddPoint(TX xValue, TY yValue) => Points.Add(new ChartPoint<TX, TY>(xValue, yValue));
    }
}