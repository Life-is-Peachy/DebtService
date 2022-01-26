using DAL.SQL;
using Ionic.Zlib;
using Shared;
using Shared.Utills;
using Shared.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
	public static class Repository
	{
		public static SqlConnection GetDebtConnection(bool needOpen = true)
		{
			string str = SETTINGS.PIPELINE_DB_CONNECTION;
			SqlConnection connection = new SqlConnection(str);

			if (needOpen)
				connection.Open();

			return connection;
		}

		public static RepositoryTransaction BeginTransaction()
		{
			SqlConnection connection = GetDebtConnection();
			return new RepositoryTransaction(connection.BeginTransaction());
		}

		public static string ResolveKey(string sourceKey)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT [Source] FROM [dbo].[Sources]
                                    WHERE  [Key] = @Key";

				cmd.Parameters.AddWithValue("@Key", sourceKey);

				return (string)cmd.ExecuteScalar();
			}
		}

		public static void QueueUpOrder(UnpreparedOrder order, string source, SqlTransaction tr)
		{
			using (SqlCommand cmd = new SqlCommand(string.Empty, tr.Connection, tr))
			{
				cmd.Parameters.Clear();
				cmd.CommandText = @"INSERT INTO [dbo].[Pipeline]
                                           (
                                              [ID_Request],
                                              [Source],
                                              [AddressRecivedAt],
                                              [Address],
                                              [Square],
                                              [District],
                                              [City],
                                              [Town],
                                              [Street],
                                              [Home],
                                              [Corp],
                                              [Flat]
                                           )
                                           VALUES 
                                           (
                                              @ID_Request,
                                              @Source,
                                              @AddressRecivedAt,
                                              @Address,
                                              @Square,
                                              @District,
                                              @City,
                                              @Town,
                                              @Street,
                                              @Home,
                                              @Corp,
                                              @Flat
                                           )";

				cmd.Parameters.AddWithValue("@ID_Request", order.ID_Request);
				cmd.Parameters.AddWithValue("@Source", source);
				cmd.Parameters.AddWithValue("@AddressRecivedAt", DateTime.Now);
				cmd.Parameters.AddWithValue("@Address", order.Address);
				cmd.Parameters.AddWithValue("@Square", order.Square, true);
				cmd.Parameters.AddWithValue("@District", order.District, true);
				cmd.Parameters.AddWithValue("@City", order.Town == "Ростов-на-Дону" ? order.Town : order.City, true);
				cmd.Parameters.AddWithValue("@Town", order.Town == "Ростов-на-Дону" ? string.Empty : order.Town, true);
				cmd.Parameters.AddWithValue("@Street", order.Street);
				cmd.Parameters.AddWithValue("@Home", order.Home, true);
				cmd.Parameters.AddWithValue("@Corp", order.Corp, true);
				cmd.Parameters.AddWithValue("@Flat", order.Flat, true);
				cmd.ExecuteNonQuery();
			}
		}

		public static bool CheckUnpreparedQueue()
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT COUNT(*) FROM [dbo].[Pipeline]
                                    WHERE [CadastralNumber] IS NULL
                                    AND   [NotFound] = 0";

				return (int)cmd.ExecuteScalar() == 0;
			}
		}

		public static UnpreparedOrder GetUnpreparedOrder()
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT TOP 1 * FROM [dbo].[Pipeline]
                                    WHERE [CadastralNumber] is NULL
                                    AND   [NotFound] = 0
                                    ORDER BY [AddressRecivedAt]";

				using (SqlDataReader reader = cmd.ExecuteReader())
				{
					reader.Read();
					return new UnpreparedOrder
					{
						ID = reader.GetData<int>(0),
						ID_Request = reader.GetData<int>(1),
						RecivedAt = DateTime.Now,
						Source = reader.GetData<string>(2),
						Square = reader.GetData<string>(8),
						District = reader.GetData<string>(14),
						City = reader.GetData<string>(15),
						Town = reader.GetData<string>(16),
						Street = reader.GetData<string>(17),
						Home = reader.GetData<string>(18),
						Corp = reader.GetData<string>(19),
						Flat = reader.GetData<string>(20)
					};
				}
			}
		}

		public static ObjPair<string, string> GetUnpreparedSessionCredentials()
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT TOP 1
                                        [LoginKey],
                                        [Value]
                                        FROM [dbo].[RosrKeys]";

				using (SqlDataReader reader = cmd.ExecuteReader())
				{
					reader.Read();
					return new ObjPair<string, string>(reader.GetData<string>(0), reader.GetData<string>(1));
				}
			}
		}

		public static bool CheckPreparedQueue()
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT COUNT(*) FROM [dbo].[Pipeline]
                                    WHERE [IsChecked] = 1
                                    AND   [NumRequest] is NULL
                                    AND   [ChkAnnul] = 0";

				return (int)cmd.ExecuteScalar() == 0;
			}
		}

		public static PreparedOrder GetPreparedOrder()
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT TOP 1
                                    [Pipeline].[ID],
                                    [Pipeline].[Source],
                                    [Pipeline].[CadastralNumber]
                                    FROM  [dbo].[Pipeline]
                                    WHERE [IsChecked] = 1 
                                    AND   [NumRequest] is null 
                                    AND   [ChkAnnul] != 1
                                    AND   [CadastralNumber] not IN (SELECT [InOrdering] FROM [RosrKeys] WHERE [InOrdering] != '' or [InOrdering] is NOT NULL)
                                    ORDER BY [Priority] DESC";

				using (SqlDataReader reader = cmd.ExecuteReader())
				{
					reader.Read();
					return new PreparedOrder
					{
						ID = reader.GetData<int>(0),
						Source = reader.GetData<string>(1),
						CadastralNumber = reader.GetData<string>(2)
					};
				}
			}
		}

		public static ObjPair<string, string> GetPreparedSessionCredentials()
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT TOP 1
                                    [LoginKey],
                                    [Value]
                                    FROM  [dbo].[RosrKeys]
                                    WHERE [InOrdering] = '' 
                                    or	  [InOrdering] is NULL";

				using (SqlDataReader reader = cmd.ExecuteReader())
				{
					reader.Read();
					return new ObjPair<string, string>(reader.GetData<string>(0), reader.GetData<string>(1));
				}
			}
		}

		public static void SetBusyOrder(string loginKey, string cadastral)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[RosrKeys]
                                    SET	   [InOrdering] = @Cadastral
                                    WHERE  [LoginKey] = @loginKey";

				cmd.Parameters.AddWithValue("@Cadastral", cadastral);
				cmd.Parameters.AddWithValue("@loginKey", loginKey);
				cmd.ExecuteNonQuery();
			}
		}

		public static void SetFreeOrder(string loginKey)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[RosrKeys]
                                    SET    [InOrdering] = ''
                                    WHERE  [LoginKey] = @loginKey";

				cmd.Parameters.AddWithValue("@loginKey", loginKey);
				cmd.ExecuteNonQuery();
			}
		}

		public static bool CheckLoadQueue()
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT count(*)
                                    FROM  [Pipeline]
                                    WHERE [HasXml] = 0 AND [NumRequest] is NOT NULL
									AND   [NumRequest] NOT in (SELECT [InLoading] FROM [RosrKeys] WHERE [InLoading] != '' or [InLoading] is NOT NULL) AND 
                                    CASE
                                    WHEN  [LastUploadAttempt] is NOT NULL 
                                    THEN  Dateadd(hour, 1, [LastUploadAttempt]) 
                                    ELSE  DateAdd(hour, 24, [RequestRecivedAt])
                                    END   <GETDATE()";

				return (int)cmd.ExecuteScalar() == 0;
			}
		}

		public static LoadOrder GetLoadableOrder()
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT TOP (1) [Pipeline].[ID], [Pipeline].[Source], [Pipeline].[NumRequest], [RosrKeys].[Value]
                                        FROM       [DebtPipeline].[dbo].[Pipeline]
                                        JOIN	   [RosrKeys] ON [RosrKeys].[LoginKey] = [Pipeline].[Worker]
                                        WHERE	   [HasXml] = 0 AND [NumRequest] is NOT NULL
									    AND		   [NumRequest] NOT in (SELECT [InLoading] FROM [RosrKeys] WHERE [InLoading] != '' or [InLoading] is NOT NULL) AND
                                        CASE WHEN  [LastUploadAttempt] is NOT NULL
                                        THEN       Dateadd(hour, 1, LastUploadAttempt) 
                                        else	   DateAdd(hour, 24, RequestRecivedAt)
                                        END        <GETDATE()
                                        ORDER BY   [Priority], [LastUploadAttempt], [RequestRecivedAt]";

				cmd.Parameters.AddWithValue("@PickUpAttemptDelay", SETTINGS.PICKUP_ATTEMPT_DELAY);
				cmd.Parameters.AddWithValue("@FirstPickUpAttemptDelay", SETTINGS.FIRST_PICKUP_ATTEMPT_DELAY);

				using (SqlDataReader reader = cmd.ExecuteReader())
				{
					reader.Read();
					return new LoadOrder
					{
						ID = reader.GetData<int>(0),
						Source = reader.GetData<string>(1),
						NumRequest = reader.GetData<string>(2),
						SessionKey = reader.GetData<string>(3)
					};
				}
			}
		}

		public static void SetBusyLoader(string Key, string numRequest)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[RosrKeys]
                                    SET    [InLoading] = @NumRequest
                                    WHERE  [Value] =  @Key";

				cmd.Parameters.AddWithValue("@NumRequest", numRequest);
				cmd.Parameters.AddWithValue("@Key", Key);
				cmd.ExecuteNonQuery();
			}
		}

		public static void SetFreeLoader(string key)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[RosrKeys]
                                    SET    [InLoading] = ''
                                    WHERE  [Value] = @key";

				cmd.Parameters.AddWithValue("@key", key);
				cmd.ExecuteNonQuery();
			}
		}

		public static void SetFreeOnStart()
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[RosrKeys]
                                    SET    [InOrdering] = ''

                                    UPDATE [dbo].[RosrKeys]
                                    SET    [InLoading] = ''";

				cmd.ExecuteNonQuery();
			}
		}

		public static void SetAddressNotFound(UnpreparedOrder order)
		{
			using (SqlConnection connection = Repository.GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                    SET    [Result] = 'Адрес не найден'
                                    WHERE  [ID] =  @ID
                                    AND    [Source] = @Source";

				cmd.Parameters.AddWithValue("@Source", order.Source);
				cmd.Parameters.AddWithValue("@ID", order.ID);

				cmd.ExecuteNonQuery();
			}
		}

		public static void SetNotFoundData(UnpreparedOrder original)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                    SET    [NotFound] = 1
                                    WHERE  [ID] =  @ID
                                    AND    [Source] = @Source";

				cmd.Parameters.AddWithValue("@Source", original.Source);
				cmd.Parameters.AddWithValue("@ID", original.ID);
				cmd.ExecuteNonQuery();
			}
		}

		public static void SetIncorrect(PreparedOrder order, string worker)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                    SET    [Worker] = @Worker,
                                           [Result] = 'Не корректный кадастровый',
                                           [IsChecked] = 0
                                    WHERE  [ID] = @id
                                    AND    [Source] = @Source";

				cmd.Parameters.AddWithValue("@Worker", worker);
				cmd.Parameters.AddWithValue("@id", order.ID);
				cmd.Parameters.AddWithValue("@Source", order.Source, true);

				cmd.ExecuteNonQuery();
			}
		}

		public static void SetNoAddressesFound(PreparedOrder order, string worker)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                    SET    [Worker] = @Worker,
                                           [Result] = 'Не корректный кадастровый. Адресса не найдены',
                                           [IsChecked] = 0
                                    WHERE  [ID] = @id
                                    AND    [Source] = @Source";

				cmd.Parameters.AddWithValue("@Worker", worker);
				cmd.Parameters.AddWithValue("@id", order.ID);
				cmd.Parameters.AddWithValue("@Source", order.Source, true);

				cmd.ExecuteNonQuery();
			}
		}

		public static void SetMoreThanOneAddressesFound(PreparedOrder order, string worker)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                    SET    [Worker] = @Worker,
                                           [Result] = 'Не корректный кадастровый. Больше одного адреса',
                                           [IsChecked] = 0
                                    WHERE  [ID] = @id
                                    AND    [Source] = @Source";

				cmd.Parameters.AddWithValue("@Worker", worker);
				cmd.Parameters.AddWithValue("@id", order.ID);
				cmd.Parameters.AddWithValue("@Source", order.Source, true);

				cmd.ExecuteNonQuery();
			}
		}

		public static void SetAsPrepared(PreparedOrder order, DateTime time, string numReq, string worker)
		{
			numReq = numReq.Insert(2, "-");

			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                    SET    [Worker] = @Worker,
                                           [Result] = 'В ожидании XML',
                                           [NumRequest] = @NumRequest,
                                           [RequestRecivedAt] = @DateSend
                                    WHERE  [ID] = @id
                                    AND    [Source] = @Source";

				cmd.Parameters.AddWithValue("@Worker", worker);
				cmd.Parameters.AddWithValue("@id", order.ID);
				cmd.Parameters.AddWithValue("@DateSend", time, true);
				cmd.Parameters.AddWithValue("@NumRequest", numReq, true);
				cmd.Parameters.AddWithValue("@Source", order.Source, true);

				cmd.ExecuteNonQuery();
			}
		}

		public static void AddPreparedData(UnpreparedOrder original, ICollection<AddressSearchInfo> addrs)
		{
			if (addrs.Count == 0)
				return;

			using (RepositoryTransaction tr = BeginTransaction())
			{
				try
				{
					foreach (var item in addrs)
						AddPreparedData(original, item, tr.Transaction);

					tr.Commit();
				}
				catch
				{
					tr.Rollback();
					throw;
				}
			}
		}

		public static void AddPreparedData(UnpreparedOrder original, AddressSearchInfo addr, SqlTransaction tr)
		{
			using (SqlCommand cmd = new SqlCommand(string.Empty, tr.Connection, tr))
			{
				cmd.CommandText = @"INSERT INTO [dbo].[Pipeline]
                                    (
                                        [ID_Request],
                                        [Source],
                                        [AddressRecivedAt],
                                        [CadastralNumber],
                                        [Address],
                                        [Square],
                                        [District],
                                        [City],
                                        [Town],
                                        [Street],
                                        [Home],
                                        [Flat],
                                        [R_FullAddress],
                                        [R_ObjType],
                                        [R_Square],
                                        [R_SteadCategory],
                                        [R_SteadKind],
                                        [R_FuncName],
                                        [R_Status],
                                        [R_CadastralCost],
                                        [R_CadastralCostDate],
                                        [R_NumStoreys],
                                        [R_UpdateInfoDate],
                                        [R_LiterBTI],
                                        [NotFound],
                                        [ChkAnnul]
                                    )
                                    VALUES
                                    (
                                        @ID_Request,
                                        @Source,
                                        @RecivedAt,
                                        @CadastralNumber,
                                        @Address,
                                        @Square,
                                        @District,
                                        @City,
                                        @Town,
                                        @Street,
                                        @Home,
                                        @Flat,
                                        @Ros_FullAddress,
                                        @Ros_ObjType,
                                        @Ros_Square,
                                        @Ros_SteadCategory,
                                        @Ros_SteadKind,
                                        @Ros_FuncName,
                                        @Ros_Status,
                                        @Ros_CadastralCost,
                                        @Ros_CadastralCostDate,
                                        @Ros_NumStoreys,
                                        @Ros_UpdateInfoDate,
                                        @Ros_LiterBTI,
                                        0,
                                        @ChkAnnul
                                    )";

				cmd.Parameters.AddWithValue("@ID_Request", original.ID_Request);
				cmd.Parameters.AddWithValue("@RecivedAt", original.RecivedAt);
				cmd.Parameters.AddWithValue("@Source", original.Source);
				cmd.Parameters.AddWithValue("@CadastralNumber", addr.CadastralNumber);
				cmd.Parameters.AddWithValue("@Address", original.Address);
				cmd.Parameters.AddWithValue("@Square", original.Square);
				cmd.Parameters.AddWithValue("@District", original.District, true);
				cmd.Parameters.AddWithValue("@City", original.Town == "Ростов-на-Дону" ? original.Town : original.City, true);
				cmd.Parameters.AddWithValue("@Town", original.Town == "Ростов-на-Дону" ? string.Empty : original.Town, true);
				cmd.Parameters.AddWithValue("@Street", original.Street);
				cmd.Parameters.AddWithValue("@Home", original.Home, true);
				cmd.Parameters.AddWithValue("@Flat", original.Flat, true);
				cmd.Parameters.AddWithValue("@Ros_FullAddress", addr.FullAddress);
				cmd.Parameters.AddWithValue("@Ros_ObjType", addr.ObjType, true);
				cmd.Parameters.AddWithValue("@Ros_Square", addr.Square, true);
				cmd.Parameters.AddWithValue("@Ros_SteadCategory", addr.SteadCategory, true);
				cmd.Parameters.AddWithValue("@Ros_SteadKind", addr.SteadKind, true);
				cmd.Parameters.AddWithValue("@Ros_FuncName", addr.FuncName, true);
				cmd.Parameters.AddWithValue("@Ros_Status", addr.Status, true);
				cmd.Parameters.AddWithValue("@Ros_CadastralCost", addr.CadastralCost, true);
				cmd.Parameters.AddWithValue("@Ros_CadastralCostDate", addr.CadastralCostDate, true);
				cmd.Parameters.AddWithValue("@Ros_NumStoreys", addr.NumStoreys, true);
				cmd.Parameters.AddWithValue("@Ros_UpdateInfoDate", addr.UpdateInfoDate, true);
				cmd.Parameters.AddWithValue("@Ros_LiterBTI", addr.LiterBTI, true);
				cmd.Parameters.AddWithValue("@ChkAnnul", addr.ChkAnnul);
				cmd.ExecuteNonQuery();
			}
		}

		public static void RemoveSuccessedPreparedOrder(UnpreparedOrder order)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"DELETE [Pipeline] 
                                    WHERE  [ID] = @ID
                                    AND    [Source] = @Source";

				cmd.Parameters.AddWithValue("@ID", order.ID);
				cmd.Parameters.AddWithValue("@Source", order.Source);
				cmd.ExecuteScalar();
			}
		}

		public static void MarkOrderAsSent(PreparedOrder order, DateTime time, string numReq, string worker)
		{
			numReq = numReq.Insert(2, "-");

			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                    SET    [Worker] = @Worker,
                                           [NumRequest] = @NumRequest,
                                           [RequestRecivedAt] = @DateSend
                                    WHERE  [ID] = @id
                                    AND    [Source] = @Source";

				cmd.Parameters.AddWithValue("@Worker", worker);
				cmd.Parameters.AddWithValue("@id", order.ID);
				cmd.Parameters.AddWithValue("@DateSend", time, true);
				cmd.Parameters.AddWithValue("@NumRequest", numReq, true);
				cmd.Parameters.AddWithValue("@Source", order.Source, true);
				cmd.ExecuteNonQuery();
			}
		}

		public static IEnumerable<ObjPair<string, string>> GetXmlHrefs()
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT [Href],
                                           [FactoryType]
                                    FROM   [dbo].[xmlhref]";

				using (SqlDataReader reader = cmd.ExecuteReader())
				{
					while (reader.Read()) yield return new ObjPair<string, string>(reader.GetData<string>(0), reader.GetData<string>(1));
				}
			}
		}

		public static void AddXmlData(int id, string xml, string html, string href)
		{
			byte[] xmlBytes = GZipStream.CompressString(xml);
			byte[] htmlBytes = !string.IsNullOrWhiteSpace(html) ? GZipStream.CompressString(html) : null;

			using (SqlCommand cmd = new SqlCommand(string.Empty, GetDebtConnection()))
			{
				cmd.CommandText = @"INSERT INTO [dbo].[Xml]
                                        (
                                            [ID_Pipeline],
                                            [XmlData],
                                            [HtmlData],
                                            [XslPath],
                                            [XmlSize],
                                            [HtmlSize]
                                        )
                                        VALUES
                                        (
                                             @ID,
                                             @XmlData,
                                             @HtmlData,
                                             @XslPath,
                                             @XmlSize,
                                             @HtmlSize
                                        )

                                        IF 
                                        (SELECT COUNT(*) FROM [Pipeline] WHERE [ID_Request] = (SELECT [ID_Request] FROM [Pipeline] WHERE [ID] = @ID)) = 1
	                                    UPDATE [Pipeline]
	                                    SET    [IsResult] = 1
	                                    where  [ID] = @ID";

				cmd.Parameters.AddWithValue("@ID", id);
				cmd.Parameters.AddWithValue("@XmlData", xmlBytes).SqlDbType = SqlDbType.VarBinary;
				cmd.Parameters.AddWithValue("@HtmlData", htmlBytes, true).SqlDbType = SqlDbType.VarBinary;
				cmd.Parameters.AddWithValue("@XslPath", href);
				cmd.Parameters.AddWithValue("@XmlSize", (short)(xmlBytes.Length >> 10)); // размер в Кб
				cmd.Parameters.AddWithValue("@HtmlSize", htmlBytes != null ? (short)(htmlBytes.Length >> 10) : (short?)null, true); // размер в Кб
				cmd.ExecuteNonQuery();
			}
		}

		public static void CreateEGRP(IEnumerable<EGRP> EGRPs)
		{
			using (RepositoryTransaction tr = BeginTransaction())
			{
				try
				{
					foreach (var item in EGRPs)
						CreateEGRP(item, tr.Transaction);

					tr.Commit();
				}
				catch
				{
					tr.Rollback();
					throw;
				}
			}
		}

		public static void CreateEGRP(EGRP egrp, SqlTransaction tr)
		{
			using (SqlCommand cmd = new SqlCommand(string.Empty, tr.Connection, tr))
			{
				cmd.CommandText = @"INSERT INTO [dbo].[EGRP]
                                        (
                                            [ID_Pipeline],
                                            [FIO],
                                            [RegDate],
                                            [Numerator],
                                            [Denominator]
                                        )
                                        VALUES
                                        (
                                            @ID_Pipeline,
                                            @FIO,
                                            @RegDate,
                                            @Numerator,
                                            @Denominator
                                        )

                                        UPDATE [Pipeline]
                                        SET    [Result] = 'Получены сведения о собственнике',
                                               [HasXml] = 1
                                        WHERE  [ID] = @ID_Pipeline";

				cmd.Parameters.AddWithValue("@ID_Pipeline", egrp.ID_Pipeline);
				cmd.Parameters.AddWithValue("@FIO", egrp.FIO);
				cmd.Parameters.AddWithValue("@RegDate", egrp.DateReg);
				cmd.Parameters.AddWithValue("@Numerator", egrp.Fraction.Numerator);
				cmd.Parameters.AddWithValue("@Denominator", egrp.Fraction.Denominator);
				cmd.ExecuteNonQuery();
			}
		}

		public static void SetGovResult(int id)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [Pipeline]
                                    SET    [Result] = 'Собственник ДИЗО',
                                           [HasXml] = 1
                                    WHERE  [ID] = @ID";

				cmd.Parameters.AddWithValue("@ID", id);
				cmd.ExecuteScalar();
			}
		}

		public static void SetOrganizationResult(int id)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [Pipeline]
                                    SET    [Result] = 'Собственник ЮЛ',
                                           [HasXml] = 1
                                    WHERE  [ID] = @ID";

				cmd.Parameters.AddWithValue("@ID", id);
				cmd.ExecuteScalar();
			}
		}

		public static void SetNoXmlData(int id)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [Pipeline]
                                    SET    [Result] = 'Отсутствуют сведения о собственнике',
                                           [HasXml] = 1
                                    WHERE  [ID] = @ID";

				cmd.Parameters.AddWithValue("@ID", id);
				cmd.ExecuteScalar();
			}
		}

		public static void UpdateLastUploadAttempt(int id)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [Pipeline]
                                    SET    [LastUploadAttempt] = @time
                                    WHERE  [ID] = @ID";

				cmd.Parameters.AddWithValue("@ID", id);
				cmd.Parameters.AddWithValue("@time", DateTime.Now);
				cmd.ExecuteNonQuery();
			}
		}

		public static void SetAnul(PreparedOrder order)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [Pipeline]
                                    SET    [ChkAnnul] = 1
                                    WHERE  [ID] = @ID
                                    AND    [Source] = @Source";

				cmd.Parameters.AddWithValue("@ID", order.ID);
				cmd.Parameters.AddWithValue("@Source", order.Source);
				cmd.ExecuteScalar();
			}
		}

		public static void RollBackToPrepared(int id)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"UPDATE [Pipeline]
                                    SET    [Worker] = null,
                                           [NumRequest] = null,
                                           [RequestRecivedAt] = null,
                                    WHERE  [ID] = @ID
                                    AND    [Source] = @Source";

				cmd.Parameters.AddWithValue("@ID", id);
				cmd.ExecuteScalar();
			}
		}

		public static IEnumerable<string> TryGetCorrectStreet(string failedStreet)
		{
			using (SqlConnection connection = GetDebtConnection())
			using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
			{
				cmd.CommandText = @"SELECT [Correct]
									FROM   [Dictionary] 
                                    WHERE  [Wrong] = @Value";

				cmd.Parameters.AddWithValue("@Value", failedStreet);

				using (SqlDataReader reader = cmd.ExecuteReader())
					while (reader.Read())
						yield return reader.GetData<string>(0);
			}
		}
	}
}