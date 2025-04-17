using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Admin
	{
		public int Id { get; set; }
		public string Firstname { get; set; }
		public string Lastname { get; set; }
		public string? Image { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public bool Active { get; set; }
		public int RegistrationUser { get; set; }
		public DateTime RegistrationDate { get; set; }
		public int? CorrectionUser { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual ICollection<Cafe>? Cafes { get; set; }
	}
}
