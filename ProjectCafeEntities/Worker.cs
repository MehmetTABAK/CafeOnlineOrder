using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Worker
	{
		public int Id { get; set; }
		public int CafeId { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public bool Active { get; set; }
		public int RegistrationUser { get; set; }
		public DateTime RegistrationDate { get; set; }
		public int? CorrectionUser { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Cafe Cafe { get; set; }
	}
}
