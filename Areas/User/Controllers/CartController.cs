using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WBCWebDemo.DataAccess.Repository.IRepository;
using WBCWebDemo.Models;
using WBCWebDemo.Models.ViewModels;
using WBCWebDemo.Utility;

namespace WBCWebDemo.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

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
            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Games"),

                OrderHeader = new()//初始化物件
            };
            //用foreach逐一走訪cart，用於計算購物車價格的加總
            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Games.Price * cart.Count);
            }

            return View(ShoppingCartVM);
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


       
        //總結，將使用者資料跟購物車資料帶入
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            //帶入購物車內的資料
            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Games")
                ,
                OrderHeader = new()
            };
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;//購物人姓名帶入
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.Address = ShoppingCartVM.OrderHeader.ApplicationUser.Address;

            foreach(var cart in ShoppingCartVM.ShoppingCartList)//找到每一筆購物資訊，然後做加總
            {
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Games.Price * cart.Count); //把加總完後的質帶入ordertotal
            }

            return View(ShoppingCartVM);
        }

        [HttpPost] //只處理表單送出資料
        [ActionName("Summary")] //對外路由宣稱
        public IActionResult SummaryPOST(ShoppingCartVM shoppingCartVM)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Games");
            ShoppingCartVM.OrderHeader.orderDate = DateTime.Now;//訂單設定為今日
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;//訂單綁定該使用者
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u=>u.Id == userId);

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                //計算訂單總金額
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Games.Price * cart.Count);
            }

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);//加入新的訂單資料
            _unitOfWork.Save();
            
            foreach(var cart in ShoppingCartVM.ShoppingCartList) //創建每筆訂單的訂單細節資訊
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    GameId = cart.GameId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Games.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);//新增詳細資料
                _unitOfWork.Save();
            }

            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });//重新導向到訂單確認，並把訂單編號傳送下去
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id,includeProperties:"ApplicationUser");
            _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusPending);//更新訂單狀態為等待訂單確認
            //找到該使用者購物車內容，並用list存起來
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(u=> u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);//訂單送出後，刪除購物車內容
            _unitOfWork.Save();
            return View(id);
        }
    }
}
