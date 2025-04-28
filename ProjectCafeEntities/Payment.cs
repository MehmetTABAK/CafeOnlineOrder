using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Payment
	{
		public int Id { get; set; }
		public int TableId { get; set; }
		public double TotalPrice { get; set; }
		public byte Method { get; set; }
		public bool Active { get; set; }
		public int RegistrationUser { get; set; }
		public string RegistrationUserRole { get; set; }
		public DateTime RegistrationDate { get; set; }
		public int? CorrectionUser { get; set; }
		public string? CorrectionUserRole { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Table? Table { get; set; }
	}
}
