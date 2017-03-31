using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Web1.Options;

namespace Web1.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyOptions _options;

        public HomeController(IOptionsSnapshot<MyOptions> optionsAccessor)
        {
            _options = optionsAccessor.Value;
        }

        public IActionResult Index()
        {
            ViewBag.Message = _options.MyConfigSection.MyParameter;
            return View();
        }
    }
}
