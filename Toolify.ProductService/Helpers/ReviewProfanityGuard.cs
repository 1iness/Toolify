using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Toolify.ProductService.Helpers
{
    public static class ReviewProfanityGuard
    {
        public const string StandardRejectMessage =
            "В отзыве обнаружена недопустимая лексика. Измените формулировки и отправьте снова.";

        private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

        private static readonly HashSet<string> WholeWordBanned;
        private static readonly string[] StemBanned;

        static ReviewProfanityGuard()
        {
            (WholeWordBanned, StemBanned) = LoadEncodedBlocklist();
        }

        private static (HashSet<string> WholeWords, string[] Stems) LoadEncodedBlocklist()
        {
            var asm = typeof(ReviewProfanityGuard).Assembly;
            var name = Array.Find(
                asm.GetManifestResourceNames(),
                n => n.EndsWith("ReviewProfanityBlocklist.gz", StringComparison.OrdinalIgnoreCase));
            using var zipped = name != null ? asm.GetManifestResourceStream(name) : null;
            if (zipped == null)
                throw new InvalidOperationException("Не найден встроенный ресурс ReviewProfanityBlocklist.gz");

            using var gz = new GZipStream(zipped, CompressionMode.Decompress);
            using var reader = new StreamReader(gz, Encoding.UTF8);
            var whole = new HashSet<string>(StringComparer.Ordinal);
            var stems = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length < 3 || line[1] != '|')
                    continue;
                var payload = line[2..];
                if (string.IsNullOrEmpty(payload))
                    continue;
                switch (line[0])
                {
                    case 'W':
                    case 'w':
                        whole.Add(payload);
                        break;
                    case 'S':
                    case 's':
                        stems.Add(payload);
                        break;
                }
            }
            return (whole, stems.ToArray());
        }

        public static bool ContainsProfanity(params string?[] parts)
        {
            foreach (var p in parts)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                if (ContainsProfanityInText(p.AsSpan()))
                    return true;
            }
            return false;
        }

        private static bool ContainsProfanityInText(ReadOnlySpan<char> span)
        {
            var normalized = NormalizeLeetspeak(span);

            foreach (var token in TokenizeLetters(normalized))
            {
                if (token.Length == 0) continue;
                if (WholeWordBanned.Contains(token)) return true;
                foreach (var stem in StemBanned)
                {
                    if (token.Length >= stem.Length && token.AsSpan().Contains(stem.AsSpan(), StringComparison.Ordinal))
                        return true;
                }
            }

            var fusedChain = FuseLetterChain(normalized);
            if (fusedChain.Length > 0)
            {
                foreach (var stem in StemBanned)
                {
                    if (fusedChain.Length >= stem.Length &&
                        fusedChain.AsSpan().Contains(stem.AsSpan(), StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }

        private static string FuseLetterChain(string normalized)
        {
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (IsWordChar(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static string NormalizeLeetspeak(ReadOnlySpan<char> s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                sb.Append(ToPlainLetter(char.ToLower(c, Ru)));
            }
            return sb.ToString();
        }

        private static char ToPlainLetter(char c)
        {
            return c switch
            {
                '@' => 'a',
                '4' => 'a',
                '8' => 'b',
                '3' => 'e',
                '€' => 'e',
                '1' => 'i',
                '!' => 'i',
                '|' => 'l',
                '0' => 'o',
                '5' => 's',
                '$' => 's',
                '7' => 't',
                '+' => 't',
                '9' => 'g',
                _ => c
            };
        }

        private static IEnumerable<string> TokenizeLetters(string normalized)
        {
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (IsWordChar(c))
                    sb.Append(c);
                else
                {
                    if (sb.Length > 0)
                    {
                        yield return sb.ToString();
                        sb.Clear();
                    }
                }
            }
            if (sb.Length > 0)
                yield return sb.ToString();
        }

        private static bool IsWordChar(char c)
        {
            if (c >= 'a' && c <= 'z') return true;
            if (c >= 'A' && c <= 'Z') return true;
            if (c >= 'а' && c <= 'я') return true;
            if (c == 'ё' || c == 'Ё') return true;
            if (c >= 'А' && c <= 'Я') return true;
            return false;
        }
    }
}
