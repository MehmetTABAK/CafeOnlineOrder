using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class NotificationSubscription
    {
		public int Id { get; set; }
		public int WorkerId { get; set; }
        public string Endpoint { get; set; }
        public string P256dh { get; set; }
        public string Auth { get; set; }
        public bool Active { get; set; }
		public int RegistrationUser { get; set; }
		public string RegistrationUserRole { get; set; }
		public DateTime RegistrationDate { get; set; }
		public int? CorrectionUser { get; set; }
		public string? CorrectionUserRole { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Worker? Worker { get; set; }
    }
}
