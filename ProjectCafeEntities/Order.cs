using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Order
	{
		public Guid Id { get; set; }
		public Guid ProductId { get; set; }
		public Guid TableId { get; set; }
		public byte Status { get; set; }
		public bool Active { get; set; }
		public Guid? RegistrationUser { get; set; }
		public string? RegistrationUserRole { get; set; }
		public DateTime RegistrationDate { get; set; }
		public Guid? CorrectionUser { get; set; }
		public string? CorrectionUserRole { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Product? Product { get; set; }
		public virtual Table? Table { get; set; }
	}
}
