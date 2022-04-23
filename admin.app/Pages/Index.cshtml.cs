using admin.app.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace admin.app.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IEnumerable<ClubEvent> Events { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            this.Events = await this.getClubEvents();
            return Page();
        }

        private async Task<IEnumerable<ClubEvent>> getClubEvents()
        {
            string endpoint = "http://www.sheffieldcitykayakclub.co.uk/app/api/GetEvents?count=20";

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
    }
}
