using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProjectCafeEntities
{
	public class MenuCategory
	{
		public int Id { get; set; }
		public int CafeId { get; set; }
		public string CategoryName { get; set; }
		public string? CategoryImage { get; set; }
		public bool Active { get; set; }
		public int RegistrationUser { get; set; }
		public DateTime RegistrationDate { get; set; }
		public int? CorrectionUser { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Cafe? Cafe { get; set; }
		public virtual ICollection<SubMenuCategory>? SubMenuCategories { get; set; }
	}
}
