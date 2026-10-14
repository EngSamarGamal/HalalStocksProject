namespace HalalStocks.Helpers;

public static class TextHelper
{
    public static string Truncate(string text, int maxLength) =>
        text.Length > maxLength ? text[..maxLength] + "..." : text;
}
