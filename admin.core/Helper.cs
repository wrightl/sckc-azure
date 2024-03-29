﻿using admin.core.Models;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace admin.core
{
    public static class Helper
    {
        public static async Task<IEnumerable<ClubEvent>> GetClubEvents(string baseApiUrl, int count)
        {
            string endpoint = $"{baseApiUrl}GetEvents?count={count}";

            using (HttpClient client = new HttpClient())
            {
                using (var Response = await client.GetAsync(endpoint))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return JsonConvert.DeserializeObject<IEnumerable<ClubEvent>>(await Response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        throw new Exception("Failed to load Events");
                    }
                }

            }
        }

        public static async Task<IEnumerable<Booking>> GetBookings(string connectionString, string Id)
        {
            var client = new TableClient(connectionString, "bookings");

            var table = await client.CreateIfNotExistsAsync();

            Pageable<TableEntity> queryResultsFilter = client.Query<TableEntity>(filter: $"PartitionKey eq '{Id}'");

            List<Booking> bookings = new List<Booking>();
            foreach (var booking in queryResultsFilter)
            {
                int? people = null;
                if (!string.IsNullOrWhiteSpace(booking.GetString("People")))
                    people = int.Parse(booking.GetString("People"));

                bookings.Add(new Booking()
                {
                    Name = booking.GetString("Name"),
                    Email = booking.RowKey,
                    BookingType = booking.GetString("BookingType"),
                    IsLiveBooking = "Live".Equals(booking.GetString("Type"), StringComparison.InvariantCultureIgnoreCase),
                    People = people,
                    Amount = booking["Amount"] != null ? (double?)Convert.ToDouble(booking["Amount"].ToString()) : null,
                });
            }

            return bookings;
        }

        public static string GetEventPartitionKey(ClubEvent ev)
        {
            return $"{ev.Summary.Replace(" ", string.Empty)}_{ev.StartDateTime.ToString("ddMMyy")}";
        }

        public static async Task<bool> SendMessage(string ApiKey, string Subject, string To, string From, string Message, string ToName = null, string FromName = null, string ReployTo = null)
        {
            var from = new EmailAddress(From, FromName ?? "SCKC");
            var to = new EmailAddress(To, ToName ?? "SCKC");
            var msg = MailHelper.CreateSingleEmail(from, to, Subject, Message, Message);
            msg.ReplyTo = new EmailAddress(ReployTo ?? Constants.AdminEmailAddress);

            var client = new SendGridClient(ApiKey);
            var response = await client.SendEmailAsync(msg);

            if (response?.IsSuccessStatusCode == true)
                return true;

            throw new Exception(await response.Body.ReadAsStringAsync());
        }

        public static string ReplaceTemplatePlaceholders(string template, Dictionary<string, string> info)
        {
            var s = template;

            foreach (var kvp in info)
            {
                s = s.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            return s;
        }
    }
}
