using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Order
	{
		public int Id { get; set; }
		public int ProductId { get; set; }
		public int TableId { get; set; }
		public short Status { get; set; }
		public bool Active { get; set; }
		public DateTime RegistrationDate { get; set; }
		public int? CorrectionUser { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Product Product { get; set; }
		public virtual Table Table { get; set; }
	}
}
