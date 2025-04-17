using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Cafe
	{
		public int Id { get; set; }
		public int AdminId { get; set; }
		public string Name { get; set; }
		public string? Image { get; set; }
		public string Location { get; set; }
		public bool Active { get; set; }
		public int RegistrationUser { get; set; }
		public DateTime RegistrationDate { get; set; }
		public int? CorrectionUser { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Admin? Admin { get; set; }
		public virtual ICollection<MenuCategory>? MenuCategories { get; set; }
		public virtual ICollection<Worker>? Workers { get; set; }
		public virtual ICollection<Section>? Sections { get; set; }
	}
}
