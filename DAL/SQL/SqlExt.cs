using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace System.Data.SqlClient
{
	public static class SqlExt
	{
		public static string ToSqlString(this SqlCommand command)
		{
			string query = command.CommandText;
			foreach (SqlParameter parameter in command.Parameters)
			{
				query = query.Replace(parameter.ParameterName, (parameter.Value ?? "").ToString());
			}

			return query;
		}

		public static T ExecuteScalar<T>(this SqlCommand cmd)
		{
			object res = cmd.ExecuteScalar();
			if (res == null || res == DBNull.Value)
				return default;

			return (T)res;
		}

		public static T GetData<T>(this SqlDataReader reader, int index)
			=> reader.IsDBNull(index) ? default : (T)reader.GetValue(index);

		public static T GetData<T>(this DbDataReader reader, int index)
			=> reader.IsDBNull(index) ? default : (T)reader.GetValue(index);

		public static SqlParameter AddWithValue(this SqlParameterCollection collection, string name, object value, bool isNullable)
		{
			SqlParameter p = collection.AddWithValue(name, value ?? DBNull.Value);
			p.IsNullable = isNullable;

			return p;
		}

		public static SqlParameter AddWithValue(this SqlParameterCollection collection, string name, object value, bool isNullable, SqlDbType sqlType)
		{
			SqlParameter p = collection.AddWithValue(name, value ?? DBNull.Value);
			p.IsNullable = isNullable;
			p.SqlDbType = sqlType;

			return p;
		}

		public static SqlParameter[] AddArrayParameters<T>(this SqlCommand cmd, string name, IEnumerable<T> values, string pattern = "{0}")
		{
			ICollection<SqlParameter> parameters = new List<SqlParameter>();
			ICollection<string> parameterExprList = new List<string>();
			int num = 0;
			foreach (T value in values)
			{
				string paramName = name + num++;
				parameters.Add(cmd.Parameters.AddWithValue(paramName, value));
				parameterExprList.Add(string.Format(pattern, paramName));
			}

			cmd.CommandText = cmd.CommandText.Replace(name, string.Join(", ", parameterExprList));

			return parameters.ToArray();
		}

		public static void AddArrayParameter<T>(this SqlCommand cmd, string name, IEnumerable<T> values, string pattern = "({0})")
		{
			IEnumerable<string> formattedValues = values.Select(x => string.Format(pattern, x));

			cmd.CommandText = cmd.CommandText.Replace(name, string.Join(", ", formattedValues));
		}

		public static string ToSqlString(this DbCommand command)
		{
			string query = command.CommandText;
			foreach (DbParameter parameter in command.Parameters)
			{
				query = query.Replace(parameter.ParameterName, (parameter.Value ?? "").ToString());
			}

			return query;
		}

		public static T ExecuteScalar<T>(this DbCommand command)
		{
			object res = command.ExecuteScalar();
			if (res == null || res == DBNull.Value)
				return default;

			return (T)res;
		}

		public static DbParameter AddWithValue(this DbCommand command, string name, object value, bool isNullable)
		{
			DbParameter p = command.CreateParameter();
			p.ParameterName = name;
			p.Value = value ?? DBNull.Value;
			p.SourceColumnNullMapping = isNullable;
			command.Parameters.Add(p);

			return p;
		}

		public static DbParameter AddWithValue(this DbCommand command, string name, object value, bool isNullable, DbType dbType)
		{
			DbParameter p = command.CreateParameter();
			p.ParameterName = name;
			p.Value = value ?? DBNull.Value;
			p.SourceColumnNullMapping = isNullable;
			p.DbType = dbType;
			command.Parameters.Add(p);

			return p;
		}

		public static T GetData<T>(this SqlDataReader reader, string name)
		{
			object obj = reader[name];
			return obj is DBNull ? default : (T)obj;
		}

		public static int ExecuteNonQueryThrowable(this SqlCommand cmd)
		{
			int c = cmd.ExecuteNonQuery();

			if (c == 0)
				throw new DataException("Выполнение команды sql-сервера не задействовало ни одной строки");

			return c;
		}
	}
}
