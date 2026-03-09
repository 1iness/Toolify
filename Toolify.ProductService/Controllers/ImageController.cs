using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Data;

namespace Toolify.ProductService.Controllers
{
    public class ImageController : Controller
    {
        private readonly ProductRepository _repository;

        public ImageController(ProductRepository repository)
        {
            _repository = repository;
        }
        [HttpGet("image/{productId}")]
        public async Task<IActionResult> GetImage(int productId)
        {
            var image = await _repository.GetMainImageAsync(productId);

            if (image == null)
            {
                return File("~/images/no-image.png", "image/png");
            }

            return File(image.Value.Data, image.Value.ContentType);
        }
    }
}
