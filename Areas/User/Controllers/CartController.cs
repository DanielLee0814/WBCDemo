using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WBCWebDemo.DataAccess.Repository.IRepository;
using WBCWebDemo.Models;
using WBCWebDemo.Models.ViewModels;

namespace WBCWebDemo.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCartVM shoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {       
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            //驗證是否登入
           var claimsIdentity = (ClaimsIdentity)User.Identity;
           var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            //建立shoppingCartVM物件，並抓資料
            shoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Games"),

                OrderHeader = new()//初始化物件
            };
            //用foreach逐一走訪cart，用於計算購物車價格的加總
            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Games.Price * cart.Count);
            }

            return View(shoppingCartVM);
        }

        //增加購物車內產品的數量
        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId); //找到該項購物車內產品的資料，利用view傳了cartId來找
            cartFromDb.Count += 1;// 把該產品的數量加一
            _unitOfWork.ShoppingCart.Update(cartFromDb); //更新資料
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        //減少購物車產品的數量
        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u =>u.Id == cartId);
            if(cartFromDb.Count <= 1) //如果數量小於等於一，就移除，因為本來最少數量就是一，所以按了減就代表要在購物車移除這項產品
            {
                _unitOfWork.ShoppingCart.Remove(cartFromDb); 
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        //直接刪除購物車內的產品

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u =>u.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        //總結

        public IActionResult Summary()
        {
            return View();
        }
    }
}
