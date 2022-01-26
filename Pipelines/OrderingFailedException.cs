using System;

namespace Pipelines
{
	internal class OrderingFailedException : Exception
	{
		public int OrderID { get; set; }
		public int ID_Request { get; set; }

		public OrderingFailedException()
		{ }

		public OrderingFailedException(string message)
			: base(message)	{ }

		public OrderingFailedException(string message, Exception innerException)
			: base(message, innerException)	{ }

		public OrderingFailedException(int orderID)
		{
			OrderID = orderID;
		}

		public OrderingFailedException(int orderID, int iD_Request)
		{
			OrderID = orderID;
			ID_Request = iD_Request;
		}
	}
}
