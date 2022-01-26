using Shared.Utills;
using System;

namespace Shared.Classes
{
    public class EGRP
    {
        public int ID_Pipeline { get; set; }

        public string FIO { get; set; }

        public DateTime DateReg { get; set; }

        public Fraction Fraction { get; set; }

		public string FullFraction { get; set; }
	}
}