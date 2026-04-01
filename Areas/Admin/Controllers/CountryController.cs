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
    public class CountryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;   //資料庫(資料表)變數
        //建構子

        public CountryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            //用泛型集合物件List將資料存入物件中
            List<Country> objCountryList = _unitOfWork.Country.GetAll().ToList();
            return View(objCountryList);
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Country obj)
        {
            // 檢查 Name 是否重複
            bool nameExists = _unitOfWork.Country.NameExists(obj.Name);

            // 檢查 Rank 是否重複
            bool rankExists = _unitOfWork.Country.RankExists(obj.Rank);

            if (nameExists)
            {
                ModelState.AddModelError("Name", "國家名稱已存在");
            }

            if (rankExists)
            {
                ModelState.AddModelError("Rank", "排名已被使用");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.Country.Add(obj);
                _unitOfWork.Save();//將所有的資料進行保存並寫重新設定物件內容
                //新增TempData:用來顯示新增成功的通知
                TempData["success"] = "國家新增成功";
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
            Country? countryFromDb = _unitOfWork.Country.Get(u => u.CountryID == id);
            if(countryFromDb == null)
            {
                return NotFound();
            }

            return View(countryFromDb);
        }
        [HttpPost]
        //由表單post的物件，然後修改
        public IActionResult Edit(Country obj)
        {
             var country = _unitOfWork.Country.Get(
            c => (c.Name == obj.Name || c.Rank == obj.Rank)
             && c.CountryID != obj.CountryID
                );

            if (country != null)
            {
                if (country.Name == obj.Name)
                    ModelState.AddModelError("Name", "國家名稱已存在");

                if (country.Rank == obj.Rank)
                    ModelState.AddModelError("Rank", "排名已被使用");
            }

            if (ModelState.IsValid)
            {                                                   
                _unitOfWork.Country.Update(obj);
                _unitOfWork.Save();//將所有的資料進行保存並寫重新設定物件內容
                //新增TempData:用來顯示編輯成功的通知
                TempData["success"] = "國家編輯成功";
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
            Country? countryFromDb = _unitOfWork.Country.Get(u => u.CountryID == id);
            if (countryFromDb == null)
            {
                return NotFound();
            }
            return View(countryFromDb);
        }
        //對外的路由 / Action 名稱還是叫 Delete
        [HttpPost,ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {
            Country? obj = _unitOfWork.Country.Get(u => u.CountryID == id);
            if(obj == null)
            {
                return NotFound();
            }

            _unitOfWork.Country.Remove(obj);
            _unitOfWork.Save();
            //新增TempData:用來顯示刪除成功的通知
            TempData["success"] = "國家刪除成功";
            return RedirectToAction("Index");
        }
    }
}
