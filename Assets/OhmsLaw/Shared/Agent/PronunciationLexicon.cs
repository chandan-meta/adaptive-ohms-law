using System.Text.RegularExpressions;

namespace OhmsLaw
{
    // Respellings applied to text on its way to text-to-speech. Subtitles keep the original spelling.
    public static class PronunciationLexicon
    {
        static readonly (Regex word, string spoken)[] Entries =
        {
            (new Regex(@"\bGeorg\b", RegexOptions.IgnoreCase), "GAY-ork"),
            (new Regex(@"\bOhm-niscope\b", RegexOptions.IgnoreCase), "OHM-nih-scope"),
            (new Regex(@"\bnichrome\b", RegexOptions.IgnoreCase), "NY-krome"),
            (new Regex(@"\bXY\b"), "X Y"),
            (new Regex(@"\bV ?/ ?I\b"), "V divided by I"),
            (new Regex("ΔV"), "delta V"),
            (new Regex("ΔI"), "delta I"),
            (new Regex("Ω"), "ohm"),
        };

        public static string Apply(string text)
        {
            foreach (var (word, spoken) in Entries) text = word.Replace(text, spoken);
            return text;
        }
    }
}
