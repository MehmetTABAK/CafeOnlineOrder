using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Section
	{
		public Guid Id { get; set; }
		public Guid CafeId { get; set; }
		public string Name { get; set; }
		public string? Image { get; set; }
		public bool Active { get; set; }
		public Guid RegistrationUser { get; set; }
		public string RegistrationUserRole { get; set; }
		public DateTime RegistrationDate { get; set; }
		public Guid? CorrectionUser { get; set; }
		public string? CorrectionUserRole { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Cafe? Cafe { get; set; }
		public virtual ICollection<Table>? Tables { get; set; }
	}
}
