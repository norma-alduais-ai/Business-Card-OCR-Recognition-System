using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace Web.Controllers
{
    public class CardController : Controller
    {
        private readonly ICardService _cardService;

        public CardController(ICardService cardService)
        {
            _cardService = cardService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                ViewBag.Message = "Please select a file.";
                return View();
            }

            // File validation for Tesseract
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                ViewBag.Message = "Please upload a valid image file (PNG, JPG, JPEG, BMP, TIFF) for best OCR results.";
                return View();
            }

            // File size check
            if (imageFile.Length > 5 * 1024 * 1024) // 5MB max
            {
                ViewBag.Message = "File size too large. Maximum size is 5MB.";
                return View();
            }

            try
            {
                var result = await _cardService.ProcessCardAsync(imageFile);

                if (result.Success)
                {
                    ViewBag.Success = "Business card processed successfully!";
                    return View(result.Data);
                }
                else
                {
                    ViewBag.Message = result.Error ?? "Failed to process business card.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Error: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> AllCards()
        {
            var cards = await _cardService.GetAllAsync();
            return View(cards);
        }
    }
}