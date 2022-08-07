using admin.app.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Stripe;
using System;
using System.Threading.Tasks;

namespace admin.app.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentsService _payments;

        public PaymentsController(PaymentsService paymentsService)
        {
            _payments = paymentsService ?? throw new ArgumentNullException(nameof(paymentsService));
        }

        [HttpPost("livewebhook")]
        public async Task<IActionResult> LiveWebhook()
        {
            return await webhook(true);
        }

        [HttpPost("testwebhook")]
        public async Task<IActionResult> TestWebhook()
        {
            return await webhook(false);

        }

        private async Task<IActionResult> webhook(bool isLive)
        {
            try
            {
                var json = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();

                string signature = null;
#if !DEBUG
                Request.Headers.TryGetValue("Stripe-Signature", out var headerSignature);
                signature = headerSignature.ToString();
#endif

                if (isLive)
                    await _payments.ProcessLiveWebhook(json, signature);
                else
                    await _payments.ProcessLiveWebhook(json, signature);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        
    }
}
