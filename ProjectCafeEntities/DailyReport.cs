using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
    public class DailyReport
    {
        public Guid Id { get; set; }
        public Guid CafeId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double? TotalAmount { get; set; }
        public double? TotalCard { get; set; }
        public double? TotalCash { get; set; }
        public double? TotalBonus { get; set; }
        public double? TotalRefund { get; set; }
        public bool Active { get; set; }
        public Guid RegistrationUser { get; set; }
        public string RegistrationUserRole { get; set; }
        public DateTime RegistrationDate { get; set; }
        public Guid? CorrectionUser { get; set; }
        public string? CorrectionUserRole { get; set; }
        public DateTime? CorrectionDate { get; set; }

        public virtual Cafe? Cafe { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }
}
