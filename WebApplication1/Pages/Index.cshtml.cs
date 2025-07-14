using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "Zip code is required.")] // RequiredAttribute is defined in System.ComponentModel.DataAnnotations
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Zip code must be a 5 digit number.")]
        public string ZipCode { get; set; }

        public void OnGet()
        {

        }

        public void OnPost()
        {
            if (!ModelState.IsValid)
            {
                // Handle validation errors (optional)
            }
        }
    }
}
