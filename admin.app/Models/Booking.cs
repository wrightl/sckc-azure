namespace admin.app.Models
{
    public class Booking
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public int? People { get; set; }
        public string BookingType { get; set; }
        public bool IsLiveBooking { get; set; }
        public double? Amount { get; set; }
    }
}
