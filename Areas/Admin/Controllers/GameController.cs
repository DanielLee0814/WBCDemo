using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using WBCWebDemo.DataAccess.Data;
using WBCWebDemo.DataAccess.Repository.IRepository;
using WBCWebDemo.Models;
using WBCWebDemo.Utility;



namespace WBCWebDemo.Areas.Admin.Controllers
{
    [Area ("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class GameController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;   //資料庫(資料表)變數
        //建構子

        public GameController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            //用泛型集合物件List將資料存入物件中
            List<Games> objGameList = _unitOfWork.Games.GetAll().ToList();
            return View(objGameList);
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Games obj)
        {
           
            if (ModelState.IsValid)
            {
                _unitOfWork.Games.Add(obj);
                _unitOfWork.Save();//將所有的資料進行保存並寫重新設定物件內容
                //新增TempData:用來顯示新增成功的通知
                TempData["success"] = "場次資料新增成功";
                return RedirectToAction("Index");
            }
            return View();
        }

        //找到對應的id，並回傳對應的資料給view
        public IActionResult Edit(int? id)
        {
            if(id == null || id == 0)
            {
                return NotFound();
            }
            Games? GameFromDb = _unitOfWork.Games.Get(u => u.GameId == id);
            if(GameFromDb == null)
            {
                return NotFound();
            }

            return View(GameFromDb);
        }
        [HttpPost]
        //由表單post的物件，然後修改
        public IActionResult Edit(Games obj)
        {
            

            if (ModelState.IsValid)
            {                                                   
                _unitOfWork.Games.Update(obj);
                _unitOfWork.Save();//將所有的資料進行保存並寫重新設定物件內容
                //新增TempData:用來顯示編輯成功的通知
                TempData["success"] = "場次編輯成功";
                return RedirectToAction("Index");
            }
          
            return View();
        }

        public IActionResult Delete(int? id)
        { 
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Games? GameFromDb = _unitOfWork.Games.Get(u => u.GameId == id);
            if (GameFromDb == null)
            {
                return NotFound();
            }
            return View(GameFromDb);
        }
        //對外的路由 / Action 名稱還是叫 Delete
        [HttpPost,ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {
            Games? obj = _unitOfWork.Games.Get(u => u.GameId == id);
            if(obj == null)
            {
                return NotFound();
            }

            _unitOfWork.Games.Remove(obj);
            _unitOfWork.Save();
            //新增TempData:用來顯示刪除成功的通知
            TempData["success"] = "場次刪除成功";
            return RedirectToAction("Index");
        }
    }
}
