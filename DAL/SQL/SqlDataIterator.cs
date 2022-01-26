namespace System.Data.SqlClient
{
    /// <summary>
    /// Упрощает работу по чтению строк из базы данных
    /// </summary>
    public class SqlDataIterator : IDisposable
    {
        private readonly SqlCommand _cmd;
        private SqlDataReader _reader;
        private int _index;

        public SqlDataIterator(SqlCommand cmd)
        {
            _cmd = cmd;
        }

        /// <summary>
        /// Инициализирует начало чтения данных из базы
        /// </summary>
        public void InitReader()
        {
            _reader?.Dispose();

            _reader = _cmd.ExecuteReader();
        }

        /// <summary>
        /// Получает следующий по порядку элемент в строке данных
        /// </summary>
        /// <typeparam name="T">Тип объекта в строке данных</typeparam>
        public T GetData<T>(bool repeat = false)
        {
            object obj = repeat ? _reader.GetValue(_index - 1) : _reader.GetValue(_index++);

            if (obj == null || obj is DBNull)
                return default;

            return (T)obj;
        }

        /// <summary>
        /// Получает следующий по порядку элемент в строке данных
        /// </summary>
        public object GetValue(bool repeat = false)
        {
            object obj = repeat ? _reader.GetValue(_index - 1) : _reader.GetValue(_index++);

            if (obj == null || obj is DBNull)
                return null;

            return obj;
        }

        /// <summary>
        /// Получает элемент в строке данных по указанному индексу
        /// </summary>
        /// <typeparam name="T">Тип объекта в строке данных</typeparam>
        public T GetData<T>(int index)
        {
            object obj = _reader.GetValue(index);

            if (obj == null || obj is DBNull)
                return default;

            return (T)obj;
        }

        /// <summary>
        /// Перемещает SqlDataReader к следующей записи
        /// </summary>
        public bool Read()
        {
            if (!_reader.Read())
                return false;

            _index = 0;
            return true;
        }

		public void Dispose()
            => _reader?.Dispose();
	}
}
