using admin.app.Extensions;
using admin.app.Models;
using admin.app.Settings;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace admin.app.Pages
{
    public class BookingsModel : PageModel
    {
        private readonly ILogger<BookingsModel> _logger;
        private readonly IWebHostEnvironment _webhost;
        private readonly Secrets _secrets;

        public BookingsModel(ILogger<BookingsModel> logger, IWebHostEnvironment webhost, IOptions<Secrets> options)
        {
            _logger = logger;
            _webhost = webhost ?? throw new ArgumentNullException(nameof(webhost));
            _secrets = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public IEnumerable<Booking> Bookings { get; set; }
        public string EventDate { get; set; }

        public async Task<IActionResult> OnGetAsync(string id, string EventDate)
        {
            this.EventDate = EventDate;
            this.Bookings = await this.getBookings(id);
            return Page();
        }

        private async Task<IEnumerable<Booking>> getBookings(string Id)
        {
            var connectionString = this._secrets.azure_storage_connstring.Trim();

            var client = new TableClient(connectionString, "bookings");

            var table = await client.CreateIfNotExistsAsync();

            Pageable<TableEntity> queryResultsFilter = client.Query<TableEntity>(filter: $"PartitionKey eq '{Id}'");

            List<Booking> bookings = new List<Booking>();
            foreach (var booking in queryResultsFilter)
            {
                bookings.Add(new Booking()
                {
                    Name = booking.GetString("Name"),
                    Email = booking.RowKey,
                    BookingType = booking.GetString("BookingType"),
                    IsLiveBooking = "Live".Equals(booking.GetString("Type"), StringComparison.InvariantCultureIgnoreCase),
                    People = booking.GetInt32("People"),
                    Amount = booking["Amount"] != null ? (double?)Convert.ToDouble(booking["Amount"].ToString()) : null,
                });
            }

            return bookings;
        }
    }
}
