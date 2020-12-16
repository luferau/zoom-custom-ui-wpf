namespace zoom_custom_ui_wpf.Helpers
{
    public static class StringHelper
    {
        public static string SplitByCharacters(this string text, int substringLength, string characters)
        {
            var charactersCount = text.Length / substringLength;

            for (var i = 1; i <= charactersCount; i++)
                text = text.Insert(i * substringLength, characters);

            return text;
        }
    }
}
