namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides LINQ support for <see cref="DynamicJsonList"/>.
    /// </summary>
    public static class DynamicJsonListLinqExtensions
    {
        /// <summary>
        /// Returns the elements of a <see cref="DynamicJsonList"/> as a sequence of
        /// <c>dynamic</c>, so LINQ lambdas can use dynamic member access.
        /// </summary>
        /// <param name="list">The list to enumerate.</param>
        /// <returns>
        /// The list's elements in order. A JSON <c>null</c> element is returned as a new
        /// plain <see cref="object"/>, not as <c>null</c>.
        /// </returns>
        /// <remarks>
        /// Cast inside projection lambdas (for example <c>(long)x.Qty</c>), because the
        /// compiler cannot infer the numeric type of a <c>dynamic</c> value.
        /// </remarks>
        public static IEnumerable<dynamic> AsEnumerable(this DynamicJsonList list)
        {
            foreach (var item in list)
                yield return item ?? new();
        }
    }
}