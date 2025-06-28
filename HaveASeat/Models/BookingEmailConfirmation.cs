namespace HaveASeat.Models
{
	public class BookingEmailNotification
	{
		public string ToEmail { get; set; } = string.Empty;
		public string CustomerName { get; set; } = string.Empty;
		public string SalonName { get; set; } = string.Empty;
		public string ServiceName { get; set; } = string.Empty;
		public DateTime AppointmentDate { get; set; }
		public string AppointmentTime { get; set; } = string.Empty;
		public string SalonAddress { get; set; } = string.Empty;
		public string SalonPhone { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public string? StaffName { get; set; }
		public string? Notes { get; set; }
		public Guid BookingId { get; set; }
		public bool IsConfirmation { get; set; } = true;
	}
}
