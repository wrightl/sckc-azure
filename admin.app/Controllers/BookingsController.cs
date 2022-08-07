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
            var result = await _bookingsService.LogBooking(info);

            if (result)
                return Ok();
            return BadRequest();
        }

        [HttpPost("enquiry")]
        public async Task<IActionResult> Enquiry(BookingDto info)
        {
            var result = await _bookingsService.LogEnquiry(info);

            if (result)
                return Ok();
            return BadRequest();
        }
    }
}
