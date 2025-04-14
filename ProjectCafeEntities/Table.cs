using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Table
	{
		public int Id { get; set; }
		public int SectionId { get; set; }
		public string Name { get; set; }
		public bool Active { get; set; }
		public int RegistrationUser { get; set; }
		public DateTime RegistrationDate { get; set; }
		public int? CorrectionUser { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Section Section { get; set; }
		public virtual ICollection<Order> Orders { get; set; }
		public virtual ICollection<Payment> Payments { get; set; }
	}
}
