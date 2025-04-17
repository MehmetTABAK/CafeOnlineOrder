using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Section
	{
		public int Id { get; set; }
		public int CafeId { get; set; }
		public string Name { get; set; }
		public string? Image { get; set; }
		public bool Active { get; set; }
		public int RegistrationUser { get; set; }
		public DateTime RegistrationDate { get; set; }
		public int? CorrectionUser { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Cafe? Cafe { get; set; }
		public virtual ICollection<Table>? Tables { get; set; }
	}
}
