namespace admin.app.Settings
{
    public class Secrets
    {
        public string stripe_webhook_live_secret {  get; set; }

        public string stripe_webhook_test_secret { get; set; }  

        public string azure_storage_connstring { get; set; }

        public string sendgrid_apikey { get; set; }
    }
}
