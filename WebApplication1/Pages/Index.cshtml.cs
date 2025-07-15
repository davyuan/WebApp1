using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly AppDbContext _db;

        public IndexModel(ILogger<IndexModel> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        [BindProperty]
        [Required(ErrorMessage = "Zip code is required.")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Zip code must be a 5 digit number.")]
        public string ZipCode { get; set; }

        [TempData]
        public string Message { get; set; }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // For demo, use CityId = 1. In real app, get CityId from user or context.
            int cityId = 1;
            bool exists = await _db.ZipCodes.AnyAsync(z => z.CityId == cityId && z.Zip == ZipCode);
            if (exists)
            {
                Message = $"Zip code {ZipCode} already exists.";
                return Page();
            }

            var zip = new ZipCode { CityId = cityId, Zip = ZipCode };
            _db.ZipCodes.Add(zip);
            await _db.SaveChangesAsync();
            Message = $"Zip code {ZipCode} saved successfully.";
            return Page();
        }
    }
}
