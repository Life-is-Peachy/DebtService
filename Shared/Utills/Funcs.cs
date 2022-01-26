using Shared.Classes;
using System;
using System.Globalization;
using System.Text;

namespace Shared.Utills
{
    /// <summary>
    /// Сборник различный функций для обработки информациии выписок Росреестра
    /// </summary>
    public static class Funcs
    {
        private static readonly char[] _standardSeparator = new char[] { ' ' };

        /// <summary>
        /// Вычисляет расстояние левенштейна между двумя строками
        /// </summary>
        /// <param name="s1">Первая строка</param>
        /// <param name="s2">Вторая строка</param>
        /// <returns>Число символов, которое нужно добавить, изменить или удалить,
        /// чтобы получить из строки №1 строку №2</returns>
        public static int LevenshteinDistance(string s1, string s2)
        {
            int diff;
            int[,] m = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; ++i)
                m[i, 0] = i;

            for (int j = 1; j <= s2.Length; ++j)
                m[0, j] = j;

            for (int i = 1; i <= s1.Length; ++i)
                for (int j = 1; j <= s2.Length; ++j)
                {
                    diff = char.ToLowerInvariant(s1[i - 1]) == char.ToLowerInvariant(s2[j - 1]) ? 0 : 1;

                    int min = m[i - 1, j] + 1;
                    int a = m[i, j - 1] + 1;
                    int b = m[i - 1, j - 1] + diff;

                    if (a < min)
                        min = a;

                    m[i, j] = b < min ? b : min;
                }

            return m[s1.Length, s2.Length];
        }

        /// <summary>
        /// Получает число, которое показывает степень похожести
        /// двух строк между собой. Чем больше это число, тем более похожи строки
        /// </summary>
        /// <param name="words">Слова из основной строки</param>
        /// <param name="s2">Сравниваемая строка</param>
        /// <returns>Степень похожести</returns>
        public static double ApproximateDifference(string[] words, string s2)
            => ApproximateDifference(words, s2.FastSplit());

        /// <summary>
        /// Получает число, которое показывает степень похожести
        /// двух строк между собой. Чем больше это число, тем более похожи строки
        /// </summary>
        /// <param name="s1">Основная строка</param>
        /// <param name="s2">Сравниваемая строка</param>
        /// <returns>Степень похожести</returns>
        public static double ApproximateDifference(string s1, string s2)
            => ApproximateDifference(s1.FastSplit(), s2.FastSplit());

        /// <summary>
        /// Получает число, которое показывает степень похожести
        /// двух строк между собой. Чем больше это число, тем более похожи строки
        /// </summary>
        /// <param name="words1">Слова из основной строки</param>
        /// <param name="words2">Слова из сравниваемой строки</param>
        /// <returns>Степень похожести</returns>
        unsafe public static double ApproximateDifference(string[] words1, string[] words2)
        {
            if (words1.Length == 0 || words2.Length == 0)
                return 0.0;

            int len1 = words1.Length;
            int len2 = words2.Length;
            int totalLen = len1 * len2;
            int wordIndex;

            double max;
            double resultRank = 0.0;

            // Массив рангов похожести для каждой пары слов
            double* ranks = stackalloc double[totalLen];
            for (int i = 0; i < totalLen; ++i)
                ranks[i] = 0.0;

            // Определяем коэффициенты для каждой пары слов
            for (int i = 0; i < len2; ++i)
            {
                for (int j = 0; j < len1; ++j)
                {
                    ranks[i * len1 + j] = GetModifiedTanimotoRank(words2[i], words1[j]);
                }
            }

            // массив признаков того, что слово уже имеет максимальное совпадение
            // с одним из заданных слов.
            bool* usedWords = stackalloc bool[len1];
            for (int i = 0; i < len1; ++i)
                usedWords[i] = false;

            // Ищем максимумы с учётом того, что если слово уже имеет максимальное
            // совпадение с одним из заданных слов, то его нельзя повторно учитывать,
            // даже если оно так же имеет максимальное совпадение с другим заданным словом.
            for (int i = 0; i < len2; ++i)
            {
                max = 0.0;
                wordIndex = 0;
                for (int k = 0; k < len2; ++k)
                {
                    for (int m = 0; m < len1; ++m)
                    {
                        if (ranks[k * len1 + m] > max && !usedWords[m])
                        {
                            max = ranks[k * len1 + m];
                            wordIndex = m;
                        }
                    }
                }

                usedWords[wordIndex] = true;
                resultRank += max;

                // Для слова, которое имеет максимальную похожесть:
                // если первые буквы слов совпадают, то даём дополнительные 0.1 балла
                if (words2[i][0] == words1[wordIndex][0])
                    resultRank += 0.1;
            }

            return resultRank;
        }

        /// <summary>
        /// Возвращает модифицированный коэффициент Танимото для нечеткого сравнения слов.
        /// При полном совпадении коэффициент равен 1.0
        /// </summary>
        /// <param name="firstToken">Первое слово.</param>
        /// <param name="secondToken">Второе слово.</param>
        /// <returns>Коэффициент Танимото</returns>
        unsafe public static double GetModifiedTanimotoRank(string firstToken, string secondToken)
        {
            const int SUB_TOKEN_LENGTH = 2;
            int equalSubtokensCount = 0;
            int firstCycleLength = firstToken.Length - SUB_TOKEN_LENGTH + 1;
            int secondCycleLength = secondToken.Length - SUB_TOKEN_LENGTH + 1;

            bool* usedTokens = stackalloc bool[secondCycleLength];
            for (int i = 0; i < secondCycleLength; ++i)
                usedTokens[i] = false;

            for (int i = 0; i < firstCycleLength; ++i)
            {
                for (int j = 0; j < secondCycleLength; ++j)
                {
                    if (usedTokens[j] || firstToken[i] != secondToken[j])
                        continue;

                    // Сравнение токенов (в качестве подстрок)
                    for (int k = 1; k < SUB_TOKEN_LENGTH; ++k)
                    {
                        if (firstToken[i + k] != secondToken[j + k])
                            goto CONTINUE;
                    }

                    ++equalSubtokensCount;
                    usedTokens[j] = true;

                CONTINUE: ;
                }
            }

            //      Формула Танимото
            return (double)equalSubtokensCount / (firstCycleLength + secondCycleLength - equalSubtokensCount);
        }

        /// <summary>
        /// Проверяет, является ли заданный тип обобщением указанного типа.
        /// Например: является ли тип typeof(List<AnyType>) обобщением typeof(List<>) - Да.
        /// </summary>
        /// <param name="givenType">Тип для проверки</param>
        /// <param name="genericType">Тип обобщения</param>
        public static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            if (givenType == null)
                return false;

            Type[] interfaceTypes = givenType.GetInterfaces();
            for (int i = 0; i < interfaceTypes.Length; i++)
            {
                if (interfaceTypes[i].IsGenericType && interfaceTypes[i].GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            return IsAssignableToGenericType(givenType.BaseType, genericType);
        }

        /// <summary>
        /// Пытается преобразовать указанный объект в целое число
        /// </summary>
        /// <param name="obj">Объект для преобразования</param>
        /// <returns>Целое число или null в случае неудачи</returns>
        public static int? TryConvert(object obj)
        {
            if (obj == null)
                return null;

			if (int.TryParse(obj.ToString(), out int res))
				return res;

			try
            {
                return Convert.ToInt32(obj);
            }
            catch
            {

            }

            return null;
        }

        /// <summary>
        /// Пытается преобразовать указанный объект в заданный тип
        /// </summary>
        /// <param name="obj">Объект для преобразования</param>
        public static T? TryConvert<T>(object obj)
            where T : struct
        {
            if (obj == null)
                return null;

            if (obj is T t)
                return t;

            try
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            catch
            {

            }

            return null;
        }

        /// <summary>
        /// Пытается преобразовать указанный объект в заданный тип
        /// </summary>
        /// <param name="obj">Объект для преобразования</param>
        /// <param name="formatProvider">Объект, предоставляющий сведения о форматировании для
        /// определенного языка и региональных параметров.</param>
        public static T? TryConvert<T>(object obj, IFormatProvider formatProvider)
            where T : struct
        {
            if (obj == null)
                return null;

            if (obj is T t)
                return t;

            try
            {
                return (T)Convert.ChangeType(obj, typeof(T), formatProvider);
            }
            catch
            {

            }

            return null;
        }

        #region Преобразование числа в словесное представление

        /// <summary>
        /// Переводит число в его словесное представление
        /// </summary>
        /// <param name="number">Число</param>
        public static string NumberInWords(long number, Gender gender = Gender.Masculine)
        {
            if (number == 0)
                return "ноль";

            StringBuilder res = new StringBuilder(200);

            if (number < 0)
            {
                number = -number;
                res.Append("минус");
            }

            long _18 = Math.Abs(number / 1000000000000000000L); // квинтиллионы
            long _15 = Math.Abs((number % 1000000000000000000L) / 1000000000000000L); // квадриллионы
            long _12 = Math.Abs((number % 1000000000000000L) / 1000000000000L); // триллионы
            long _09 = Math.Abs((number % 1000000000000L) / 1000000000L); // миллиарды
            long _06 = Math.Abs((number % 1000000000L) / 1000000L); // миллионы
            long _03 = Math.Abs((number % 1000000L) / 1000L); // тысячи
            long _00 = Math.Abs(number % 1000L); // сотни, десятки и единицы

            if (_18 != 0)
            {
                res.Append(' ');
                res.Append(GetHundredNumberInWord((int)_18, Gender.Masculine));
                res.Append(ThingsDecliner.GetByCount((int)_18, " квинтиллион", " квинтиллиона", " квинтиллионов"));
            }

            if (_15 != 0)
            {
                res.Append(' ');
                res.Append(GetHundredNumberInWord((int)_15, Gender.Masculine));
                res.Append(ThingsDecliner.GetByCount((int)_15, " квадриллион", " квадриллиона", " квадриллионов"));
            }

            if (_12 != 0)
            {
                res.Append(' ');
                res.Append(GetHundredNumberInWord((int)_12, Gender.Masculine));
                res.Append(ThingsDecliner.GetByCount((int)_12, " триллион", " триллиона", " триллионов"));
            }

            if (_09 != 0)
            {
                res.Append(' ');
                res.Append(GetHundredNumberInWord((int)_09, Gender.Masculine));
                res.Append(ThingsDecliner.GetByCount((int)_09, " миллиард", " миллиарда", " миллиардов"));
            }

            if (_06 != 0)
            {
                res.Append(' ');
                res.Append(GetHundredNumberInWord((int)_06, Gender.Masculine));
                res.Append(ThingsDecliner.GetByCount((int)_06, " миллион", " миллиона", " миллионов"));
            }

            if (_03 != 0)
            {
                res.Append(' ');
                res.Append(GetHundredNumberInWord((int)_03, Gender.Feminine));
                res.Append(ThingsDecliner.GetByCount((int)_03, " тысяча", " тысячи", " тысяч"));
            }

            if (_00 != 0)
            {
                res.Append(' ');
                res.Append(GetHundredNumberInWord((int)_00, gender));
            }

            return res.ToString().TrimStart(' ');
        }

        /// <summary>
        /// Получает имя заданной цифры в указанном роде
        /// </summary>
        /// <param name="digit">Цифра от 0 до 9</param>
        /// <param name="gender"></param>
        /// <returns></returns>
        private static string GetDigitInWord(int digit, Gender gender = Gender.Masculine)
        {
            switch (digit)
            {
                case 0: return "ноль";

                case 1:
                    switch (gender)
                    {
                        case Gender.Neuter: return "одно";
                        case Gender.Masculine: return "один";
                        case Gender.Feminine: return "одна";
                        default: throw new ArgumentOutOfRangeException(gender.ToString());
                    }

                case 2:
                    switch (gender)
                    {
                        case Gender.Neuter:
                        case Gender.Masculine: return "два";
                        case Gender.Feminine: return "две";
                        default: throw new ArgumentOutOfRangeException(gender.ToString());
                    }

                case 3: return "три";

                case 4: return "четыре";

                case 5: return "пять";

                case 6: return "шесть";

                case 7: return "семь";

                case 8: return "восемь";

                case 9: return "девять";

                default: throw new ArgumentOutOfRangeException(digit.ToString());
            }
        }

        /// <summary>
        /// Получает имя десятков для заданной цифры
        /// </summary>
        /// <param name="digit">Цифра от 0 до 9</param>
        private static string GetTensInWord(int digit1, int digit2)
        {
            switch (digit1)
            {
                case 1:
                    switch (digit2)
                    {
                        case 0: return "десять";
                        case 1: return "одиннадцать";
                        case 2: return "двенадцать";
                        case 3: return "тринадцать";
                        case 4: return "четырнадцать";
                        case 5: return "пятнадцать";
                        case 6: return "шестнадцать";
                        case 7: return "семнадцать";
                        case 8: return "восемнадцать";
                        case 9: return "девятнадцать";
                        default: throw new ArgumentOutOfRangeException();
                    }

                case 2: return "двадцать";

                case 3: return "тридцать";

                case 4: return "сорок";

                case 5: return "пятьдесят";

                case 6: return "шестьдесят";

                case 7: return "семьдесят";

                case 8: return "восемьдесят";

                case 9: return "девяносто";

                default: throw new ArgumentOutOfRangeException(digit1.ToString());
            }
        }

        /// <summary>
        /// Получает имя сотен для заданной цифры
        /// </summary>
        /// <param name="digit">Цифра от 0 до 9</param>
        private static string GetHundredsInWord(int digit)
        {
            switch (digit)
            {
                case 1: return "сто";

                case 2: return "двести";

                case 3: return "триста";

                case 4: return "четыреста";

                case 5: return "пятьсот";

                case 6: return "шестьсот";

                case 7: return "семьсот";

                case 8: return "восемьсот";

                case 9: return "девятьсот";

                default: throw new ArgumentOutOfRangeException(digit.ToString());
            }
        }

        /// <summary>
        /// Получает словесное представление для числа, которое меньше тысячи, но больше нуля
        /// </summary>
        /// <param name="num">Число от 1 до 999</param>
        private static string GetHundredNumberInWord(int num, Gender gender = Gender.Masculine)
        {
            int h = num / 100; // сотни
            int t = (num % 100) / 10; // десятки
            int d = num % 10; // единицы

            string hs = null;
            if (h != 0)
                hs = GetHundredsInWord(h);

            string ts = null;
            if (t != 0)
                ts = GetTensInWord(t, d);

            string ds = null;
            if (d != 0 && t != 1)
                ds = GetDigitInWord(d, gender);

            if (hs != null && ts != null && ds != null)
                return hs + " " + ts + " " + ds;

            if (hs != null && ts == null && ds != null)
                return hs + " " + ds;

            if (hs != null && ts != null)
                return hs + " " + ts;

            if (hs != null)
                return hs;

            if (ts != null && ds != null)
                return ts + " " + ds;

			return ts ?? ds;
		}

        #endregion

        /// <summary>
        /// Вычисляет наибольший общий делитель двух чисел
        /// </summary>
        /// <param name="a">Первое число</param>
        /// <param name="b">Второе число</param>
        /// <returns>НОД</returns>
        public static int GreatestCommonDivisor(int a, int b)
        {
            if (a == 0)
                return b < 0 ? -b : b;

            if (a < 0)
                a = -a;

            if (b < 0)
                b = -b;

            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a == 0 ? b : a;
        }

        /// <summary>
        /// Вычисляет наибольший общий делитель двух чисел
        /// </summary>
        /// <param name="a">Первое число</param>
        /// <param name="b">Второе число</param>
        /// <returns>НОД</returns>
        public static long GreatestCommonDivisor(long a, long b)
        {
            if (a == 0L)
                return b < 0L ? -b : b;

            if (a < 0L)
                a = -a;

            if (b < 0L)
                b = -b;

            while (a != 0L && b != 0L)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a == 0L ? b : a;
        }

        /// <summary>
        /// Делит вещественное число на целую и дробную части
        /// с помощью форматирования
        /// </summary>
        /// <param name="val">Число</param>
        /// <param name="decimals">Кол-во знаков после запятой</param>
        /// <param name="basePart">Целая часть</param>
        /// <param name="fractPart">Дробная часть</param>
        public static void CutRealNumber(double val, int decimals, out long basePart, out long fractPart)
        {
            string fmt = "{0:#.0";
            for (int i = 1; i < decimals; i++)
                fmt += "0";

            fmt += "}";

            string[] parts = string.Format(fmt, val).Split('.');
            string p1 = parts[0];
            string p2 = parts[1];

            basePart = long.Parse(p1, CultureInfo.InvariantCulture);
            fractPart = long.Parse(p2, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Делит вещественное число на целую и дробную части
        /// с помощью функции форматирования
        /// </summary>
        /// <param name="val">Число</param>
        /// <param name="decimals">Кол-во знаков после запятой</param>
        /// <param name="basePart">Целая часть</param>
        /// <param name="fractPart">Дробная часть</param>
        public static void CutRealNumber(decimal val, int decimals, out long basePart, out long fractPart)
        {
            string fmt = "{0:0.0";
            for (int i = 1; i < decimals; i++)
                fmt += "0";

            fmt += "}";

            string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string[] parts = string.Format(CultureInfo.CurrentCulture, fmt, val).
                                    Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            string p1 = parts[0];
            string p2 = parts[1];

            basePart = long.Parse(p1, CultureInfo.InvariantCulture);
            fractPart = long.Parse(p2, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Получает короткое Ф. И. О.
        /// </summary>
        /// <param name="fullName">Полное Ф. И. О.</param>
        public static string GetShortName(string fullName)
        {
            string shortName = string.Empty;
            if (!string.IsNullOrEmpty(fullName))
            {
                string[] tokens = fullName.Split(_standardSeparator, StringSplitOptions.RemoveEmptyEntries);
                shortName += tokens[0];

                for (int i = 1; i < tokens.Length; i++)
                    shortName += " " + tokens[i][0] + ".";
            }

            return shortName;
        }
    }
}
