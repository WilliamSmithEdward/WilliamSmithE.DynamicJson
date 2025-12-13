namespace WilliamSmithE.DynamicJson
{
    public static class SafeDynamicListLinqExtensions
    {
        public static IEnumerable<dynamic> AsEnumerable(this SafeDynamicList list)
        {
            foreach (var item in list)
                yield return item ?? new();
        }
    }
}