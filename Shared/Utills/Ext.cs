using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shared.Utills
{
    /// <summary>
    /// Содержит разные методы расширения
    /// </summary>
    public static class Ext
    {
        public static void DoNotAwait(this Task task)
        { }

        public static T Dequeue<T>(this IList<T> list) where T : class
        {
            T obj = list[0];
            list.Remove(obj);

            return obj;
        }

        public static void AddWithParams<T>(this ICollection<T> list, params T[] objects) where T : new()
        {
            foreach (var item in objects)
                list.Add(item);
        }

        public static bool IsOutOfRange<T>(this ICollection<T> list, int i) where T : class
            => i > list.Count - 1;

        public static int AsInt(this string str)
            => Convert.ToInt32(str);

        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
        {
            if (givenType == null)
                return false;

            foreach (var it in givenType.GetInterfaces())
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            return IsAssignableToGenericType(givenType.BaseType, genericType);
        }
        unsafe public static string[] FastSplit(this string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            if (str.Length > 255)
                throw new ArgumentOutOfRangeException("Длина строки не должна превышать 255 символов");

            int len = str.Length;

            // Буфер для индексов начала слов в строке (максимальный размер)
            byte* wordIndexes = stackalloc byte[(len / 2) + 1];

            // Буфер исправленной строки (без пробелов и знаков пунктуации)
            char* buffer = stackalloc char[len + 1];
            buffer[len] = '\0'; // окончание последнего слова в строке. Иначе в конце последнего слова может попадать мусор

            char tmpChar; // Хранит очередной символ строки
            bool wordIndexFound = false; // Признак того, что в строке найдено начало слова
            int wordCount = 0; // Счётчик слов в строке

            // Создаём буферы с информацией о начале слов в строке,
            // а также избавляемся от пробелов и точек в строке, нормализуем строку
            for (byte i = 0; i < len; ++i)
            {
                tmpChar = str[i];

                switch (tmpChar)
                {
                    case ' ':
                    case '.':
                    case ',':
                    case ':':
                    case ';':
                    case '\n':
                    case '\t':
                    case '\v':
                        if (wordIndexFound)
                        {
                            wordIndexFound = false;
                        }
                        break;

                    default:
                        if (!wordIndexFound) // нашли начало слова в строке
                        {
                            wordIndexFound = true;
                            wordIndexes[wordCount++] = i;
                        }

                        buffer[i] = char.ToLowerInvariant(tmpChar);
                        break;
                }
            }

            string[] words = new string[wordCount];
            for (int w = 0; w < wordCount; ++w)
            {
                words[w] = new string((char*)(buffer + wordIndexes[w]));
            }

            return words;
        }
    }
}
