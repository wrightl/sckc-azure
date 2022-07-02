using admin.app.Settings;
using admin.core;
using admin.core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace admin.app.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly Secrets _secrets;

        public IndexModel(ILogger<IndexModel> logger, IOptions<Secrets> options)
        {
            _secrets = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public IEnumerable<ClubEvent> Events { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            this.Events = await Helper.GetClubEvents(this._secrets.base_api_url, 20);
            return Page();
        }

        public string getEventPartitionKey(ClubEvent ev)
        {
            return Helper.GetEventPartitionKey(ev);
        }
    }
}
