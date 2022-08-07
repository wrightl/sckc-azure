using admin.app.Extensions;
using admin.app.Settings;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace admin.app.Services
{
    public class StorageService
    {
        private readonly IWebHostEnvironment _webhost;
        private readonly Secrets _secrets;

        public StorageService(IWebHostEnvironment webhost, IOptions<Secrets> options)
        {
            _webhost = webhost ?? throw new ArgumentNullException(nameof(webhost));
            _secrets = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<bool> AddToStorage(TableEntity entity)
        {
            var tableClient = await GetTableClient("Bookings");

            await tableClient.UpsertEntityAsync(entity);

            purgeOldEntries(tableClient);

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
                DateTime dt = dateString.ToString().ParseDateWithCulture();
                return dt.AddYears(1) > DateTime.Now;
            }
            return false;
        }

        public async Task<TableClient> GetTableClient(string tableName)
        {
            var connectionString = this._secrets.azure_storage_connstring.Trim();
            var client = new TableClient(connectionString, tableName);

            await client.CreateIfNotExistsAsync();
            return client;
        }

        public TableEntity CreateTableEntity(string partitionKey, string rowKey, string paymentId, string name, string bookingType, string telNo, bool isLive, decimal? amount, int? people, DateTime date)
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
    }
}
