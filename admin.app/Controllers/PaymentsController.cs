using admin.app.Services;
using Microsoft.AspNetCore.Mvc;
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
            try
            {
                await _payments.ProcessLiveWebhook(HttpContext.Request.Body);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("testwebhook")]
        public async Task<IActionResult> TestWebhook()
        {
            try
            {
                await _payments.ProcessTestWebhook(HttpContext.Request.Body);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
