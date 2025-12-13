namespace WilliamSmithE.DynamicJson
{
    public static class DynamicJsonListLinqExtensions
    {
        public static IEnumerable<dynamic> AsEnumerable(this DynamicJsonList list)
        {
            foreach (var item in list)
                yield return item ?? new();
        }
    }
}