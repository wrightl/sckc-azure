using admin.app.Settings;
using admin.core;
using admin.core.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Globalization;
using io = System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace admin.app.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IWebHostEnvironment _webhost;
        private readonly Secrets _secrets;

        public PaymentsController(IWebHostEnvironment webhost, IOptions<Secrets> options)
        {
            _webhost = webhost ?? throw new ArgumentNullException(nameof(webhost));
            _secrets = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        [HttpPost("livewebhook")]
        public async Task<IActionResult> LiveWebhook()
        {
            var secret = this._secrets.stripe_webhook_live_secret.Trim();
            return await this.processWebhook(true, secret);
        }

        [HttpPost("testwebhook")]
        public async Task<IActionResult> TestWebhook()
        {
            var secret = this._secrets.stripe_webhook_test_secret.Trim();

            return await this.processWebhook(false, secret);
        }

        [HttpPost("booking")]
        public async Task<IActionResult> Booking(BookingDto info)
        {
            return await logBookingOrEnquiry(info, "Booking");
        }

        [HttpPost("enquiry")]
        public async Task<IActionResult> Enquiry(BookingDto info)
        {
            return await logBookingOrEnquiry(info, "Enquiry");
        }

        private async Task<IActionResult> logBookingOrEnquiry(BookingDto info, string type)
        {
            DateTime date = ParseDate(info.Date);

            var entity = createTableEntity(getPartitionKey(info.Event, info.Date), info.Email, null, info.Name, type, info.TelNo, info.isLiveBooking, null, info.Items?.Sum(item => item.Quantity), date);

            if (await addToStorage(entity))
                return Ok();
            return BadRequest();
        }

        private async Task<IActionResult> processWebhook(bool isLive, string secret)
        {
            try
            {
                var json = await new io.StreamReader(HttpContext.Request.Body).ReadToEndAsync();

#if DEBUG
                var stripeEvent = EventUtility.ParseEvent(
                  json
                );
#else
                Request.Headers.TryGetValue("Stripe-Signature", out var signature);
                var stripeEvent = EventUtility.ConstructEvent(
                  json,
                  signature,
                  secret
                );
#endif

                // Handle the checkout.session.completed event
                switch (stripeEvent.Type)
                {
                    case Events.CheckoutSessionCompleted:
                        var session = stripeEvent.Data.Object as Session;
                        await this.confirmBooking(isLive, session);
                        break;
                    case Events.ChargeRefunded:
                        var charge = stripeEvent.Data.Object as Charge;
                        await this.removeBooking(charge);
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<bool> confirmBooking(bool isLive, Session session)
        {
            var metadata = session.Metadata;
            _ = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _ = metadata["Email"] ?? throw new ArgumentNullException("Email");
            _ = metadata["Event"] ?? throw new ArgumentNullException("Event");
            _ = metadata["Date"] ?? throw new ArgumentNullException("Date");
            _ = metadata["Name"] ?? throw new ArgumentNullException("Name");
            _ = metadata["People"] ?? throw new ArgumentNullException("People");

            var date = ParseDate(metadata["Date"]);

            var entity = createTableEntity(getPartitionKeyFromMetadata(metadata), getRowKeyFromMetadata(metadata), session.PaymentIntentId, metadata["Name"], "Paid", metadata["TelNo"], isLive, Convert.ToDecimal(session.AmountTotal) / 100, Convert.ToInt32(metadata["People"]), date.ToUniversalTime());

            var result = await addToStorage(entity);

            // Send email to sckc booking requests email
            await Helper.SendMessage(
                _secrets.sendgrid_apikey,
                $"Booking request for {metadata["Event"]} on {metadata["Date"]}",
                Constants.BookingRequestEmailAddress,
                Constants.BookingRequestEmailAddress,
                $"From: {metadata["Name"]}<br/>Email: <a href=\"mailto:{metadata["Email"]}\">{metadata["Email"]}</a><br/>Number of people: {metadata["People"]}<br/>TelNo: {metadata["TelNo"]}",
                Constants.BookingRequestEmailName,
                Constants.BookingRequestEmailName
            );

            // Reply to the person booking
            string filePath = io.Path.Combine(_webhost.WebRootPath, "Templates", $"{metadata["Event"].Split(" ")[0]}_booking_response.html");

            if (io.File.Exists(filePath))
            {
                var autoReplyContent = Helper.ReplaceTemplatePlaceholders(io.File.ReadAllText(filePath), metadata);

                await Helper.SendMessage(
                    _secrets.sendgrid_apikey,
                    $"Booking request for {metadata["Event"]} on {metadata["Date"]}",
                    metadata["Email"],
                    Constants.BookingRequestEmailAddress,
                    autoReplyContent,
                    FromName: Constants.BookingRequestEmailName
                );
            }

            return result;
        }

        private async Task<bool> addToStorage(TableEntity entity)
        {
            var tableClient = await getTableClient("Bookings");

            await tableClient.UpsertEntityAsync(entity);

            purgeOldEntries(tableClient);

            return true;
        }

        private async Task<bool> removeBooking(Charge charge)
        {
            var tableClient = await getTableClient("Bookings");

            Pageable<TableEntity> queryResultsFilter = tableClient.Query<TableEntity>(filter: $"PaymentId eq '{charge.PaymentIntentId}'");

            if (queryResultsFilter != null && queryResultsFilter.Count() > 0)
            {
                TableEntity entity = queryResultsFilter.First();
                await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
            }
            return true;
        }

        private async void purgeOldEntries(TableClient client)
        {
            // Delete entries more than 12 months old
            try
            {
                Pageable<TableEntity> queryResultsFilter = client.Query<TableEntity>(entity => this.filterQuery(entity));

                foreach (var entity in queryResultsFilter)
                {
                    await client.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                }
            }
            catch
            {
            }
        }

        private bool filterQuery(TableEntity entity)
        {
            object dateString;
            entity.TryGetValue("Date", out dateString);
            if (dateString != null)
            {
                DateTime dt = ParseDate(dateString.ToString());
                return dt.AddYears(1) > DateTime.Now;
            }
            return false;
        }

        private async Task<TableClient> getTableClient(string tableName)
        {
            var connectionString = this._secrets.azure_storage_connstring.Trim();
            var client = new TableClient(connectionString, tableName);

            await client.CreateIfNotExistsAsync();
            return client;
        }

        private string getPartitionKeyFromMetadata(Dictionary<string, string> metadata)
        {
            return getPartitionKey(metadata["Event"], metadata["Date"]);
        }

        private string getPartitionKey(string Event, string Date)
        {
            return $"{Event}_{Date}".Replace("/", String.Empty).Replace(" ", string.Empty).Trim();
        }

        private string getRowKeyFromMetadata(Dictionary<string, string> metadata)
        {
            return metadata["Email"].Trim();
        }

        private TableEntity createTableEntity(string partitionKey, string rowKey, string paymentId, string name, string bookingType, string telNo, bool isLive, decimal? amount, int? people, DateTime date)
        {
            return new TableEntity(partitionKey, rowKey)
            {
                {"PaymentId", paymentId },
                {"Name", name },
                {"BookingType", bookingType },
                {"TelNo", telNo },
                {"Type", (isLive ? "Live" : "Test") },
                {"Amount", amount },
                {"People", (people.HasValue ? people.ToString() : string.Empty) },
                {"Date", date.ToUniversalTime() }
            };
        }

        private static DateTime ParseDate(string date)
        {
            CultureInfo cultureinfo = new CultureInfo("en-GB");
            return DateTime.Parse(date, cultureinfo);
        }
    }
}
