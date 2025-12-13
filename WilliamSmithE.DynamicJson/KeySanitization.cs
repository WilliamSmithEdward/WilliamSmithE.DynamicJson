using System.Text;

namespace WilliamSmithE.DynamicJson
{
    internal static class KeySanitization
    {
        internal static string Sanitize(this string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(key.Length);

            foreach (var c in key)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}