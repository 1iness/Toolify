namespace HouseholdStore.Helpers
{

    public static class PasswordPolicy
    {
        public const int MinLength = 8;
        public const int MinLetterCount = 2;
        public const int MinSymbolCount = 1;

        public static int CountLetters(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;
            return password.Count(char.IsLetter);
        }

        public static int CountNonLetterOrDigitSymbols(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;
            return password.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        }

        public static bool MeetsPolicy(string? password, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "Введите пароль.";
                return false;
            }
            if (password.Length < MinLength)
            {
                errorMessage = $"Пароль должен быть не короче {MinLength} символов.";
                return false;
            }
            if (CountLetters(password) < MinLetterCount)
            {
                errorMessage = "В пароле должно быть не менее двух букв.";
                return false;
            }
            if (CountNonLetterOrDigitSymbols(password) < MinSymbolCount)
            {
                errorMessage = "В пароле должен быть хотя бы один знак, отличный от букв и цифр (например !, @, #, -).";
                return false;
            }
            return true;
        }
    }
}
