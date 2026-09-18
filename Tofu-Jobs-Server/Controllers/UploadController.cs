using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tofu_Jobs_Server.Controllers;

[Authorize, ApiController, Route("[controller]")]
public class UploadController : ControllerBase
{
    private readonly Cloudinary cloudinary;
    private readonly IConfiguration configuration;
    private readonly ILogger<UploadController> logger;
    public UploadController(Cloudinary provider, IConfiguration settings, ILogger<UploadController> log)
    { cloudinary = provider; configuration = settings; logger = log; }

    [HttpPost, Consumes("multipart/form-data"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] IFormFile file)
    {
        if ((configuration.GetValue<bool>("LocalDemo:Enabled") || configuration.GetValue<bool>("VisitorDemo:Enabled")))
            return StatusCode(503, "External uploads are disabled in the disposable local test stack.");
        if (file == null || file.Length <= 0 || file.Length > 5 * 1024 * 1024)
            return BadRequest("Choose an image smaller than 5 MiB.");
        if (file.ContentType is not ("image/png" or "image/jpeg" or "image/webp"))
            return BadRequest("Only PNG, JPEG and WebP images are supported.");
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription("image", stream), Format = "webp"
            });
            if (result.StatusCode != HttpStatusCode.OK || result.SecureUrl == null)
                return StatusCode(502, "Image provider could not complete the upload.");
            return Ok(result.SecureUrl);
        }
        catch (Exception)
        {
            // Provider exceptions can contain credentials or upstream response bodies.
            logger.LogWarning("Image upload provider request failed");
            return StatusCode(502, "Image provider is unavailable.");
        }
    }
}
