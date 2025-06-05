using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class SubMenuCategory
	{
		public Guid Id { get; set; }
		public Guid MenuCategoryId { get; set; }
		public string SubCategoryName { get; set; }
		public string? SubCategoryImage { get; set; }
		public bool Active { get; set; }
		public Guid RegistrationUser { get; set; }
		public string RegistrationUserRole { get; set; }
		public DateTime RegistrationDate { get; set; }
		public Guid? CorrectionUser { get; set; }
		public string? CorrectionUserRole { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual MenuCategory? MenuCategory { get; set; }
		public virtual ICollection<Product>? Products { get; set; }
	}
}
