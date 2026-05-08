namespace HouseholdStore.Helpers
{

    public static class PasswordPolicy
    {
        public const int MinLength = 8;
        public const int MinLetterCount = 2;
        public const int MinSymbolCount = 1;
        public const string CompactRequirementsMessage = "Пароль: от 8 символов, без пробелов, 2 буквы и 1 спецзнак (!@#-).";
        public const string LettersAndSymbolPattern = @"^(?!.*\s)(?=(?:.*[A-Za-zА-Яа-яЁё]){2,})(?=.*[^A-Za-zА-Яа-яЁё0-9]).*$";

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
                errorMessage = CompactRequirementsMessage;
                return false;
            }
            if (password.Any(char.IsWhiteSpace))
            {
                errorMessage = CompactRequirementsMessage;
                return false;
            }
            if (CountLetters(password) < MinLetterCount)
            {
                errorMessage = CompactRequirementsMessage;
                return false;
            }
            if (CountNonLetterOrDigitSymbols(password) < MinSymbolCount)
            {
                errorMessage = CompactRequirementsMessage;
                return false;
            }
            return true;
        }
    }
}
