using System;
using System.Data.SqlClient;

namespace DAL.SQL
{
    public interface ITransaction : IDisposable
    {
        /// <summary>
        /// Применить транзакцию
        /// </summary>
        void Commit();

        /// <summary>
        /// Откатить транзакцию
        /// </summary>
        void Rollback();
    }

    public sealed class RepositoryTransaction : ITransaction
    {
        public SqlTransaction Transaction { get; }

        public RepositoryTransaction(SqlTransaction transaction)
		{
			Transaction = transaction;
		}

		public void Commit()
            => Transaction.Commit();

		public void Rollback()
            => Transaction.Rollback();

		public void Dispose()
            => Transaction?.Dispose();
	}
}
