using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Table
	{
		public Guid Id { get; set; }
		public Guid SectionId { get; set; }
		public string Name { get; set; }
		public bool Notification { get; set; }
		public bool Active { get; set; }
		public Guid RegistrationUser { get; set; }
		public string RegistrationUserRole { get; set; }
		public DateTime RegistrationDate { get; set; }
		public Guid? CorrectionUser { get; set; }
		public string? CorrectionUserRole { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Section? Section { get; set; }
		public virtual ICollection<Order>? Orders { get; set; }
		public virtual ICollection<Payment>? Payments { get; set; }
	}
}
