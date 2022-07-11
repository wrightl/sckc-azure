using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using admin.core;
using admin.core.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace admin.function
{
    public class SendBookingsFunction
    {
        [FunctionName("SendBookings")]
        public async Task Run([TimerTrigger("%TimerPeriod%")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"SendBookings Timer trigger function executed at: {DateTime.Now}");

            var azure_storage_connstring = Environment.GetEnvironmentVariable("azure_storage_connstring");
            var apiBaseUrl = Environment.GetEnvironmentVariable("base_url");
            var send_apikey = Environment.GetEnvironmentVariable("sendgrid_apikey");

            var _event = (await Helper.GetClubEvents(apiBaseUrl, 2)).FirstOrDefault();

            DateTime now = DateTime.UtcNow;

            if (_event != null && now.Hour == 17 && now.AddHours(24) > _event.StartDateTime)
            {
                // Get bookings for this event
                var bookings = await Helper.GetBookings(azure_storage_connstring, Helper.GetEventPartitionKey(_event));

                await Helper.SendMessage(send_apikey,
                    $"Bookings for {_event.Summary} on {_event.StartDateTime.ToString("dd/MM/yyyy")}",
                    "coaches@sheffieldcitykayakclub.co.uk",
                    "coaches@sheffieldcitykayakclub.co.uk",
                    convertToList(bookings));
            }
        }

        private string convertToList(IEnumerable<Booking> bookings)
        {
            if (bookings.Count() == 0)
                return "No bookings";
            return string.Join("\r\n\r\n", bookings.ToList().Select(b => $"<p>Name: {b.Name}\r\nEmail: <a href=\"mailto:{b.Email}\">{b.Email}</a>\r\nPeople: {b.People}</p>"));
        }
    }
}
