using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WBCWebDemo.DataAccess.Repository.IRepository;
using WBCWebDemo.Models;

namespace WBCWebDemo.Areas.User.Controllers
{
    [Area("User")]
    public class GameController : Controller
    {
        private readonly ILogger<GameController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public GameController(ILogger<GameController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Games> gamesList = _unitOfWork.Games.GetAll();
            return View(gamesList);
        }
        public IActionResult Details(int gameId)
        {
            ShoppingCart cart = new ()
            {
                Games = _unitOfWork.Games.Get(g => g.GameId == gameId),
                Count = 1,
                GameId = gameId
            };
           

            return View(cart);
        }

        [HttpPost]
        [Authorize]

        public IActionResult Details (ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId = userId;
            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u=>u.ApplicationUser.Id == userId && u.GameId == shoppingCart.GameId);
            if(cartFromDb != null)
            {
                cartFromDb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            else
            {
                _unitOfWork.ShoppingCart.Add(shoppingCart);
            }
            TempData["success"] = "加入購物車成功";
            _unitOfWork.Save();
            return RedirectToAction (nameof(Index));
        }
    }
}
