using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Security.Claims;
using WBCWebDemo.DataAccess.Repository.IRepository;
using WBCWebDemo.Models;
using WBCWebDemo.Models.ViewModels;
using WBCWebDemo.Utility;

namespace WBCWebDemo.Areas.Admin.Controllers
{
    //管理訂單
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty] //自動綁定資料
        public OrderVM OrderVM { get; set; } //訂單viewmodel全域變數
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        //管理訂單細節
        public IActionResult Details(int orderId)
        {
            OrderVM  = new OrderVM
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Games")
            };

            return View(OrderVM);
        }

        [HttpPost]
        [Authorize(Roles =SD.Role_Admin+","+SD.Role_Employee+","+SD.Role_Manager)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);//抓資料
            //把使用者寫入的資料，覆蓋掉原本的
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.Address = OrderVM.OrderHeader.Address;
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "訂購人資訊更新成功!";
            return RedirectToAction(nameof(Details), new {orderId = orderHeaderFromDb.Id});

        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee + "," + SD.Role_Manager)]
        //更改訂單狀態
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);//狀態改為以收到訂單，訂單準備中
            _unitOfWork.Save();
            TempData["success"] = "訂單狀態更新成功";

            return RedirectToAction(nameof(Details), new {orderId = OrderVM.OrderHeader.Id});
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee + "," + SD.Role_Manager)]
        //更改訂單狀態
        public IActionResult OrderReady()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusReady);//狀態改為訂單準備完成
            _unitOfWork.Save();
            TempData["success"] = "訂單狀態更新成功";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee + "," + SD.Role_Manager)]
        //更改訂單狀態
        public IActionResult OrderCompleted()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusCompleted);//已付款完成，結束訂單
            _unitOfWork.OrderHeader.DecreaseStock(OrderVM.OrderHeader.Id); // 呼叫 庫存SP

            _unitOfWork.Save();
            TempData["success"] = "訂單狀態更新成功";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee + "," + SD.Role_Manager)]
        //更改訂單狀態為取消訂單
        public IActionResult CancelOrder()
        {
             _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusCancelled);//取消訂單
            _unitOfWork.Save();
            TempData["success"] = "訂單狀態更新成功";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }
        #region API CALLS
        [HttpGet]
        //將資料打包為json
        public IActionResult GetAll(string status) //傳入asp-route-status
        {
            IEnumerable<OrderHeader> objOrderHeaders;//宣告物件變數
            //區分各腳色訂單管理權限，使用者只能看到自己的訂單，而其他腳色能看到所有訂單
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee) || User.IsInRole(SD.Role_Manager))
            {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId ,includeProperties: "ApplicationUser");
            }

            //訂單狀態塞選
            switch (status)
            {
                case "Pending": //等待確認訂單
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusPending);
                    break;
                case "Processing": // 訂單準備中
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "Ready":  //可領票
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusReady);
                    break;
                case "Completed"://已完成
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusCompleted);
                    break;
                default:
                    break;
            }
            return Json(new { data = objOrderHeaders });
        }

        #endregion
    }
}
