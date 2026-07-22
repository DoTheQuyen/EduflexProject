using Eduflex.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Services.Interface.Integration;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "app")]
    public class FilesController : ControllerBase
    {
        private readonly IAzureBlobDocStorageService _blobStorageService;

        public FilesController(IAzureBlobDocStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(10_000_000)] // 10MB
        public async Task<ActionResult<FileUploadResultDto>> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided.");
            }

            using var stream = file.OpenReadStream();
            var url = await _blobStorageService.UploadAsync(stream, file.FileName, file.ContentType);

            return Ok(new FileUploadResultDto
            {
                Url = url,
                FileName = file.FileName
            });
        }
    }
}