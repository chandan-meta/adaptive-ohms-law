using System.Text.RegularExpressions;

namespace OhmsLaw
{
    // Text on its way to text-to-speech. Subtitles keep the original text.
    // Apply: respellings and symbol readings, for baked lines and live answers alike ("2 V" -> "2 volts").
    // ForSpeech: Apply plus light delivery punctuation for the baked scripted lines (pauses before a reveal, after an
    // opener, at a "..." inside a line). Both are idempotent: running them twice gives the same text.
    public static class PronunciationLexicon
    {
        static Regex R(string pattern, bool ignoreCase = false) => new Regex(pattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

        static readonly (Regex pattern, string spoken)[] Entries =
        {
            (R(@"\bGeorg\b", true), "Gay-ork"),
            (R(@"\bOhm-niscope\b", true), "Ohm-nih-scope"),
            (R(@"\bnichrome\b", true), "Nye-krome"),
            (R(@"\bcoulomb(s?)\b", true), "Koo-lom$1"),
            (R(@"\bXY\b"), "X Y"),
            (R(@"\bIR\b"), "I R"),
            // Units after a number: "1 V" -> "1 volt", "0.40 A" -> "0.40 amperes", "5 Ω" -> "5 ohms".
            (R(@"(?<![\d.])1\s?V\b"), "1 volt"),
            (R(@"(\d)\s?V\b"), "$1 volts"),
            (R(@"(?<![\d.])1\s?A\b"), "1 ampere"),
            (R(@"(\d)\s?A\b"), "$1 amperes"),
            (R(@"(?<![\d.])1\s?Ω"), "1 ohm"),
            (R(@"(\d)\s?Ω"), "$1 ohms"),
            (R("Ω"), "ohm"),
            (R("[Δ∆]V"), "delta V"),
            (R("[Δ∆]I"), "delta I"),
            (R(@"\bV ?/ ?I\b"), "V divided by I"),
            (R(@"\s/\s"), " divided by "),
            (R(@"\s*÷\s*"), " divided by "),
            (R(@"\s*×\s*"), " times "),
            (R(@"\s*∝\s*"), " is proportional to "),
            (R(@"\s*=\s*"), " equals "),
            (R(@"\s*→\s*"), " to "),
            // Battery ends: "the − end" -> "the negative end"; a lone sign elsewhere is read as minus / plus.
            (R(@"−\s?(?=(end|terminal|side|plate)s?\b)"), "negative "),
            (R(@"(?<![\w+])\+\s?(?=(end|terminal|side|plate)s?\b)"), "positive "),
            (R(@"\s*−\s*"), " minus "),
            (R(@"(?<![\w+])\+(?![\w+])"), "plus"),
            (R(@"[ \t]{2,}"), " "),
            (R(@"[ \t]+(?=[)\].,!?;:])"), ""),
        };

        static readonly (Regex pattern, string spoken)[] Delivery =
        {
            (R(@"^\s*(\.\.\.|…)\s*"), ""),                          // "...we call that" continues the line before: no pause up front
            (R(@"\s*(\.\.\.|…)\s*(?=\S)"), "... "),                 // "wire...measure" -> "wire... measure": a clear beat
            (R(@"\s*—\s*"), " — "),                                  // a spaced dash reads as a short breath
            (R(@"^(So|Hmm|Well|Okay|OK)\s+(?=[a-z])"), "$1, "),      // a conversational opener gets its beat
            (R(@"(?<=[a-z])\s+(detectives?)(?=[.!?]$)"), ", $1"),   // "Case closed detective." -> a beat before the address
            // A beat before a newly named idea, so the term lands: "is called, electrical resistance", "we call it, OHM'S LAW".
            (R(@"\b(called|call (that|it)( constant value)?)\s+(?=(electric(al)? )?(current|resistance)\b|OHM'S LAW)"), "$1, "),
        };

        public static string Apply(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            foreach (var (pattern, spoken) in Entries) text = pattern.Replace(text, spoken);
            return text.Trim();
        }

        public static string ForSpeech(string text)
        {
            text = Apply(text);
            if (string.IsNullOrEmpty(text)) return text;
            foreach (var (pattern, spoken) in Delivery) text = pattern.Replace(text, spoken);
            return text.Trim();
        }
    }
}
