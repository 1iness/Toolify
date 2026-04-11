using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Data;
using Toolify.ProductService.Models;

namespace Toolify.ProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : Controller
    {
        private readonly ProductRepository _repository;

        public ReviewsController(ProductRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{productId}")]
        public async Task<IActionResult> GetReviews(int productId)
        {
            var reviews = await _repository.GetReviewsByProductIdAsync(productId);
            return Ok(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] Review review)
        {
            if (review.Rating < 0.5 || review.Rating > 5)
                return BadRequest("Рейтинг должен быть от 0.5 до 5");

            var doubled = review.Rating * 2;
            if (Math.Abs(doubled - Math.Round(doubled)) > 0.001)
                return BadRequest("Рейтинг должен быть с шагом 0.5");

            await _repository.AddReviewAsync(review);
            return Ok();
        }
    }
}
