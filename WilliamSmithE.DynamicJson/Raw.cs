namespace WilliamSmithE.DynamicJson
{
    public class Raw
    {
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
