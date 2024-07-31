using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostCodeController : Controller
    {
        private readonly IPostCodeServie _postCodeServie;
        private readonly AppDbContext _appDbContext;
        public PostCodeController(IPostCodeServie postCodeServie, AppDbContext appDbContext)
        {
            _postCodeServie = postCodeServie;
            _appDbContext = appDbContext;
        }

        [HttpGet("PostCodeInfo")]
        public async Task<ActionResult<IEnumerable<PostCode>>> GetPostCodeInfo()
        {
            return await _postCodeServie.GetPostCodeList();
        }

        [HttpGet("PostCodeByPost")]
        public async Task<ActionResult<IEnumerable<PostCodeDto>>> GetPostCodeByPost()
        {
            return await _postCodeServie.GetPostCodeByPost();
        }

    }
}
