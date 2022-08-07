using admin.app.Extensions;
using admin.app.Settings;
using admin.core;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using io = System.IO;

namespace admin.app.Services
{
    public class PaymentsService
    {
        private readonly IWebHostEnvironment _webhost;
        private readonly Secrets _secrets;
        private readonly StorageService _storage;

        public PaymentsService(IWebHostEnvironment webhost, IOptions<Secrets> options, StorageService storage)
        {
            _webhost = webhost ?? throw new ArgumentNullException(nameof(webhost));
            _secrets = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async Task<bool> ProcessLiveWebhook(io.Stream body)
        {
            var secret = this._secrets.stripe_webhook_live_secret.Trim();
            return await this.processWebhook(body, true, secret);
        }

        public async Task<bool> ProcessTestWebhook(io.Stream body)
        {
            var secret = this._secrets.stripe_webhook_test_secret.Trim();
            return await this.processWebhook(body, false, secret);
        }

        private async Task<bool> processWebhook(io.Stream body, bool isLive, string secret)
        {
            var json = await new io.StreamReader(body).ReadToEndAsync();

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

            return true;
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

            var date = metadata["Date"].ParseDateWithCulture();

            var entity = _storage.CreateTableEntity(getPartitionKeyFromMetadata(metadata), getRowKeyFromMetadata(metadata), session.PaymentIntentId, metadata["Name"], "Paid", metadata["TelNo"], isLive, Convert.ToDecimal(session.AmountTotal) / 100, Convert.ToInt32(metadata["People"]), date.ToUniversalTime());

            var result = await _storage.AddToStorage(entity);

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

        private async Task<bool> removeBooking(Charge charge)
        {
            var tableClient = await _storage.GetTableClient("Bookings");

            Pageable<TableEntity> queryResultsFilter = tableClient.Query<TableEntity>(filter: $"PaymentId eq '{charge.PaymentIntentId}'");

            if (queryResultsFilter != null && queryResultsFilter.Count() > 0)
            {
                TableEntity entity = queryResultsFilter.First();
                await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
            }
            return true;
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
    }
}
