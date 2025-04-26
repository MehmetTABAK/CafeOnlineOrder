namespace ProjectCafeWeb.ViewModels
{
	public class DailyAccountViewModel
	{
		public DateTime RegistrationDate { get; set; }
		public string TableName { get; set; }
		public double TotalPrice { get; set; }
		public string PaymentMethod { get; set; }
		public string RegistrationUserFullName { get; set; }
		public string? CorrectionUserFullName { get; set; }
		public string? CorrectionDate { get; set; }
		public string Date { get; set; }
	}
}
