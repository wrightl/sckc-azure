using admin.app.Services;
using admin.core.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace admin.app.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly BookingsService _bookingsService;

        public BookingsController(BookingsService service)
        {
            _bookingsService = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpPost("book")]
        public async Task<IActionResult> Book(BookingDto info)
        {
            try
            {
                var result = await _bookingsService.LogBooking(info);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();

        }

        [HttpPost("enquiry")]
        public async Task<IActionResult> Enquiry(BookingDto info)
        {
            try
            {
                var result = await _bookingsService.LogEnquiry(info);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();
        }
    }
}
