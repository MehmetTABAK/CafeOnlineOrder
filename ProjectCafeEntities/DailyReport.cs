using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
    public class DailyReport
    {
        public int Id { get; set; }
        public int CafeId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool Active { get; set; }
        public int RegistrationUser { get; set; }
        public string RegistrationUserRole { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int? CorrectionUser { get; set; }
        public string? CorrectionUserRole { get; set; }
        public DateTime? CorrectionDate { get; set; }

        public virtual Cafe? Cafe { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }
}
