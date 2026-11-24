using System.Text;

public static class WmsCallsignGenerator
{
    private static readonly Dictionary<char, string> TranslitMap = new()
    {
        {'а', "A"}, {'б', "B"}, {'в', "V"}, {'г', "G"}, {'д', "D"},
        {'е', "E"}, {'ё', "E"}, {'ж', "ZH"}, {'з', "Z"}, {'и', "I"},
        {'і', "I"}, {'й', "Y"}, {'к', "K"}, {'л', "L"}, {'м', "M"},
        {'н', "N"}, {'о', "O"}, {'п', "P"}, {'р', "R"}, {'с', "S"},
        {'т', "T"}, {'у', "U"}, {'ў', "U"}, {'ф', "F"}, {'х', "KH"},
        {'ц', "TS"}, {'ч', "CH"}, {'ш', "SH"}, {'щ', "SHCH"}, {'ъ', ""},
        {'ы', "Y"}, {'ь', ""}, {'э', "E"}, {'ю', "YU"}, {'я', "YA"}
    };

    public static string Generate(string firstName, string lastName, string ulid)
    {
        string fTranslit = Transliterate(firstName);
        string lTranslit = Transliterate(lastName);

        char fInit = fTranslit.Length > 0 ? fTranslit[0] : 'X';
        char lInit = lTranslit.Length > 0 ? lTranslit[0] : 'X';

        string ulidTail = ulid.Trim().ToUpperInvariant()[^4..];

        return $"WH-{fInit}{lInit}-{ulidTail}";
    }
  
    private static string Transliterate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder();
        foreach (char c in text.ToLowerInvariant().Trim())
        {
            if (TranslitMap.TryGetValue(c, out string? lat))
                sb.Append(lat); // If russian or other translate to lat
            else if (char.IsLetterOrDigit(c))
                sb.Append(char.ToUpperInvariant(c)); // If lat makes upper case
        }
        return sb.ToString();
    }
}