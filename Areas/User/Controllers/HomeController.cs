using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WBCWebDemo.DataAccess.Repository.IRepository;
using WBCWebDemo.Models;

namespace WBCWebDemo.Areas.User.Controllers
{
    [Area("User")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Players> playersList = _unitOfWork.Players.GetAll(includeProperties: "Country,PlayerStats");
            return View(playersList);
        }
         //球員細節數據
        public IActionResult Details(int playerId)
        {
            Players players = _unitOfWork.Players.Get(u =>u.PlayerID == playerId,includeProperties: "Country,PlayerStats");
            return View(players);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
