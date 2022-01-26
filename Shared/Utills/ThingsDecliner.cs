namespace Shared.Utills
{
    public static class ThingsDecliner
    {
        /// <summary>
        /// Возвращает строку в заданном склонении и падеже, которая соответствует
        /// количеству штук чего-либо.
        /// Например вызов GetByCount(24, "работник", "работника", "работников") вернёт строку "работника"
        /// </summary>
        /// <param name="count">Количество штук некоторого объекта</param>
        /// <param name="oneNumber">Единственное число</param>
        /// <param name="manyNumber">Множественное число</param>
        /// <param name="genitive">Родительный падеж</param>
        public static string GetByCount(long count, string oneNumber, string manyNumber, string genitive)
        {
            long mod = 0L;
            long d = count;

            if (count >= 100L && count < 1000L)
                d = count % 10L;
            else if (count >= 1000L && count < 10000L)
                d = count % 100L;
            else if (count >= 10000L && count < 100000L)
                d = count % 1000L;
            else if (count >= 100000L && count < 1000000L)
                d = count % 10000L;
            else if (count >= 1000000L && count < 10000000L)
                d = count % 100000L;
            else if (count >= 10000000L && count < 100000000L)
                d = count % 1000000L;

            if (d < 10L)
                mod = d;
            else if (d >= 20L)
                mod = d % 10L;

            if (mod == 1L)
                return oneNumber;
            else
                return (mod == 2L || mod == 3L || mod == 4L) ? manyNumber : genitive;
        }

        /// <summary>
        /// Возвращает строку в заданном склонении и падеже, которая соответствует
        /// количеству штук чего-либо.
        /// Например вызов GetByCount(24, "работник", "работника", "работников") вернёт строку "работника"
        /// </summary>
        /// <param name="count">Количество штук некоторого объекта</param>
        /// <param name="oneNumber">Единственное число</param>
        /// <param name="manyNumber">Множественное число</param>
        /// <param name="genitive">Родительный падеж</param>
        public static string GetByCount(int count, string oneNumber, string manyNumber, string genitive)
            => GetByCount((long)count, oneNumber, manyNumber, genitive);

        /// <summary>
        /// Возвращает строку в заданном склонении и падеже, которая соответствует
        /// количеству штук чего-либо, включая указанное количество.
        /// Например вызов GetByCount(24, " работник", " работника", " работников") вернёт строку "24 работника"
        /// </summary>
        /// <param name="count">Количество штук некоторого объекта</param>
        /// <param name="oneNumber">Единственное число</param>
        /// <param name="manyNumber">Множественное число</param>
        /// <param name="genitive">Родительный падеж</param>
        public static string GetThingCount(int count, string oneNumber, string manyNumber, string genitive)
            => count + GetByCount(count, oneNumber, manyNumber, genitive);
    }
}
