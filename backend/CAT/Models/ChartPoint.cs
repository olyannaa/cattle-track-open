namespace CAT.Models
{
    public class ChartPoint<TX, TY> : IComparable<ChartPoint<TX, TY>> where TX : IComparable<TX>
    {
        public TX X { get; set; }
        public TY Y { get; set; }

        public ChartPoint(TX x, TY y)
        {
            X = x;
            Y = y;
        }

        public int CompareTo(ChartPoint<TX, TY> other)
        {
            if (other == null) return 1;
            return X.CompareTo(other.X);
        }
    }
}
