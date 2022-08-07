using System.Collections.Generic;

namespace admin.core.Models
{
    public class BookingDto
    {
        public string Names { get; set; }

        public int Number { get; set; }

        public string Message { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string TelNo { get; set; }

        public string Event { get; set; }

        public string Date { get; set; }

        public bool payNow { get; set; }

        public bool isLive { get; set; }

        public List<BookingItemDto> Items { get; set; }
    }
}
