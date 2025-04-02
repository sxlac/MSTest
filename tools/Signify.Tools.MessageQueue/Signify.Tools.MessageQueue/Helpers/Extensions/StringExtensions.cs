namespace Signify.Tools.MessageQueue.Helpers.Extensions
{
    public static class StringExtensions
    {
        public static bool EqualsIgnoreCase(this string original, string comparison)
        {
            return string.Equals(original, comparison, StringComparison.OrdinalIgnoreCase);
        }
    }
}
