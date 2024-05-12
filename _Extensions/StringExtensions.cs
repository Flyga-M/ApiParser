namespace ApiParser
{
    public static class StringExtensions
    {
        public static bool EndsWith(this string @string, char value)
        {
            return @string.EndsWith(value.ToString());
        }
    }
}
