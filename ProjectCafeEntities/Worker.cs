using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Worker
	{
		public Guid Id { get; set; }
		public Guid CafeId { get; set; }
		public string Firstname { get; set; }
		public string Lastname { get; set; }
		public string? Image { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public string? RolePermissions { get; set; }
		public bool Active { get; set; }
		public Guid RegistrationUser { get; set; }
		public string RegistrationUserRole { get; set; }
		public DateTime RegistrationDate { get; set; }
		public Guid? CorrectionUser { get; set; }
		public string? CorrectionUserRole { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Cafe? Cafe { get; set; }
        public virtual ICollection<NotificationSubscription>? NotificationSubscriptions { get; set; }
    }
}
