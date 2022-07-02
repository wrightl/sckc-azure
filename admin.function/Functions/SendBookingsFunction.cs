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
        public async Task Run([TimerTrigger("0 0 0/3 ? * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"SendBookings Timer trigger function executed at: {DateTime.Now}");

            // Get next 2 events, send email with bookings between {startDiffWindow} and {endDiffWindow} minutes before event
            int startDiffWindow = (13 * 60); // 13 hours
            int endDiffWindow = (16 * 60); // 16 hours
            var azure_storage_connstring = Environment.GetEnvironmentVariable("azure_storage_connstring");
            var apiBaseUrl = Environment.GetEnvironmentVariable("base_url");
            var send_apikey = Environment.GetEnvironmentVariable("sendgrid_apikey");

            var events = await Helper.GetClubEvents(apiBaseUrl, 2);

            DateTime now = DateTime.UtcNow;

            foreach (var ev in events)
            {
                var diff = ev.StartDateTime.Subtract(now);
                if (diff.TotalMinutes > startDiffWindow && diff.TotalMinutes < endDiffWindow)
                {
                    // Get bookings for this event
                    var bookings = await Helper.GetBookings(azure_storage_connstring, Helper.GetEventPartitionKey(ev));

                    await Helper.SendMessage(send_apikey,
                        $"Bookings for {ev.Summary} on {ev.StartDateTime.ToString("dd/MM/yyyy")}",
                        "coaches@sheffieldcitykayakclub.co.uk",
                        "coaches@sheffieldcitykayakclub.co.uk",
                        convertToList(bookings));
                }
            }
        }

        private string convertToList(IEnumerable<Booking> bookings)
        {
            return string.Join("\r\n\r\n", bookings.ToList().Select(b => $"<p>Name: {b.Name}\r\nEmail: <a href=\"mailto:{b.Email}\">{b.Email}</a>\r\nPeople: {b.People}</p>"));
        }
    }
}
