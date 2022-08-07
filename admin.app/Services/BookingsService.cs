using admin.app.Extensions;
using admin.app.Settings;
using admin.core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace admin.app.Services
{
    public class BookingsService
    {
        private readonly IWebHostEnvironment _webhost;
        private readonly Secrets _secrets;
        private readonly StorageService _storage;

        public BookingsService(IWebHostEnvironment webhost, IOptions<Secrets> options, StorageService storage)
        {
            _webhost = webhost ?? throw new ArgumentNullException(nameof(webhost));
            _secrets = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async Task<bool> LogBooking(BookingDto info)
        {
            return await this.logBookingOrEnquiry(info, "Booking");
        }

        public async Task<bool> LogEnquiry(BookingDto info)
        {
            return await this.logBookingOrEnquiry(info, "Enquiry");
        }

        private async Task<bool> logBookingOrEnquiry(BookingDto info, string type)
        {
            DateTime date = info.Date.ParseDateWithCulture();

            var entity = _storage.CreateTableEntity(getPartitionKey(info.Event, info.Date), info.Email, null, info.Name, type, info.TelNo, info.isLive, null, info.Items?.Sum(item => item.Quantity), date);

            if (await _storage.AddToStorage(entity))
                return true;
            return false;
        }

        private string getPartitionKey(string Event, string Date)
        {
            return $"{Event}_{Date}".Replace("/", String.Empty).Replace(" ", string.Empty).Trim();
        }
    }
}
