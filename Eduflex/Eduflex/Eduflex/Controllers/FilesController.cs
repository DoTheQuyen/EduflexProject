using Eduflex.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Services.Interface.Integration;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "app")]
    public class FilesController : BaseApiController
    {
        private readonly IAzureBlobDocStorageService _blobStorageService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IAzureBlobDocStorageService blobStorageService, ILogger<FilesController> logger)
        {
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(10_000_000)] // 10MB
        public Task<ActionResult<FileUploadResultDto>> Upload(IFormFile file)
        {
            return HandleRequestAsync(_logger, "Error in Upload endpoint", async () =>
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("No file provided.");

                using var stream = file.OpenReadStream();
                var url = await _blobStorageService.UploadAsync(stream, file.FileName, file.ContentType);

                return new FileUploadResultDto
                {
                    Url = url,
                    FileName = file.FileName
                };
            });
        }
    }
}
