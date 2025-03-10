using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Services;
using OnePortal_Api.Model;
using OnePortal_Api.Dto;
using OnePortal_Api.Filters;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(CustomAuthorizationFilter))]
    public class PostCodeController(IPostCodeServie postCodeServie) : Controller
    {
        private readonly IPostCodeServie _postCodeServie = postCodeServie;

        [HttpGet("PostCodeInfo")]
        public async Task<ActionResult<IEnumerable<PostCode>>> GetPostCodeInfo()
        {
            try
            {
                var postCodes = await _postCodeServie.GetPostCodeList();
                if (postCodes == null || postCodes.Count == 0)
                {
                    return NotFound("No post code data found.");
                }
                return Ok(postCodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}