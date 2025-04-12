using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class SubMenuCategory
	{
		public int Id { get; set; }
		public int MenuCategoryId { get; set; }
		public string SubCategoryName { get; set; }
		public string? SubCategoryImage { get; set; }
		public bool Active { get; set; }
		public int RegistrationUser { get; set; }
		public DateTime RegistrationDate { get; set; }
		public int? CorrectionUser { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual MenuCategory? MenuCategory { get; set; }
		public virtual ICollection<Product>? Products { get; set; }
	}
}
