using CompressVariations.Service.Absolute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OE.SixLabors.ImageSharp.Jpeg.CompressVariations.Controllers
{
    /// <summary>
    /// Jpeg compress variations controller.
    /// </summary>
    [ApiController]
    [Route("jpeg-compress-variations")]
    public class JpegCompressVariationsController : ControllerBase
    {
        private readonly IImageService _imageService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="imageService">Handle image tasks</param>
        public JpegCompressVariationsController(IImageService imageService)
        {
            _imageService = imageService;
        }

        /// <summary>
        /// Will produce images with quality, sampler, size(width,height) and mode variations for specified file.
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns></returns>
        [HttpPost("produce-variations-for-single-image")]
        [Produces("application/json")]
        [AllowAnonymous]
        public IActionResult ProduceVariationsForSingleImage([FromBody] string imageFilePath)
        {
            var (result, message) = _imageService.ProduceVariationsForSingleImage(imageFilePath);
            if (result)
                return Ok(message);
            else return BadRequest(message);
        }

        /// <summary>
        /// Will produce images with quality, sampler, size(width,height) and mode variations in specified folder and its subfolders.
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns></returns>
        [HttpPost("produce-variations-for-multiple-directory")]
        [Produces("application/json")]
        [AllowAnonymous]
        public IActionResult ProduceVariationsForMultipleImageImage([FromBody] string imagesDirectoryPath)
        {
            var (result, message) = _imageService.ProduceVariationsForMultipleImageImage(imagesDirectoryPath);
            if (result)
                return Ok(message);
            else return BadRequest(message);
        }
    }
}