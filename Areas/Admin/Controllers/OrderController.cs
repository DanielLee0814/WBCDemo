using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WBCWebDemo.DataAccess.Repository.IRepository;
using WBCWebDemo.Models;

namespace WBCWebDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region API CALLS
        [HttpGet]
        //將資料打包為json
        public IActionResult GetAll()
        {
            List<OrderHeader> objOrderList = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            return Json(new { data = objOrderList });
        }

        #endregion
    }
}
