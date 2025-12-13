namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides helpers for converting dynamic JSON wrapper types into their
    /// raw CLR representations.
    /// </summary>
    /// <remarks>
    /// The methods in this class are used internally to transform
    /// <see cref="SafeDynamicObject"/> and <see cref="SafeDynamicList"/> instances
    /// into dictionaries and lists that mirror the original JSON structure.
    /// </remarks>
    public class Raw
    {
        /// <summary>
        /// Converts a dynamic JSON-derived value into its raw CLR representation.
        /// </summary>
        /// <param name="value">
        /// The value to convert. May be a <see cref="SafeDynamicObject"/>,
        /// a <see cref="SafeDynamicList"/>, or a primitive.
        /// </param>
        /// <returns>
        /// A raw CLR value suitable for serialization, such as a dictionary, list,
        /// or primitive. If the value is already a primitive, it is returned unchanged.
        /// </returns>
        /// <remarks>
        /// <see cref="SafeDynamicObject"/> and <see cref="SafeDynamicList"/> instances
        /// are recursively unwrapped into dictionaries and lists, preserving the
        /// structure of the original JSON.
        /// </remarks>
        public static object? ToRawValue(object? value)
        {
            if (value is SafeDynamicObject sdo)
                return sdo.ToRawObject();

            if (value is SafeDynamicList sdl)
                return sdl.ToRawArray();

            return value;
        }
    }
}
