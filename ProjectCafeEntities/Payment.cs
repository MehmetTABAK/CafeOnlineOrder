using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Payment
	{
		public Guid Id { get; set; }
		public Guid TableId { get; set; }
        public Guid? DailyReportId { get; set; }
        public double TotalPrice { get; set; }
		public byte Method { get; set; }
        public string? Comment { get; set; }
        public bool Active { get; set; }
		public Guid RegistrationUser { get; set; }
		public string RegistrationUserRole { get; set; }
		public DateTime RegistrationDate { get; set; }
		public Guid? CorrectionUser { get; set; }
		public string? CorrectionUserRole { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual Table? Table { get; set; }
        public virtual DailyReport? DailyReport { get; set; }
    }
}
