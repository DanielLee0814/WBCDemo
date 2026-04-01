using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using WBCWebDemo.DataAccess.Data;
using WBCWebDemo.DataAccess.Repository.IRepository;
using WBCWebDemo.Models;
using WBCWebDemo.Models.ViewModels;
using WBCWebDemo.Utility;



namespace WBCWebDemo.Areas.Admin.Controllers
{
    [Area ("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class PlayerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;   //資料庫(資料表)變數
        private readonly IWebHostEnvironment _webHostEnvironment; // 儲存圖片位置的變數
        //建構子

        public PlayerController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            //用泛型集合物件List將資料存入物件中
            List<Players> objPlayersList = _unitOfWork.Players.GetAll(includeProperties: "Country,PlayerStats").ToList();
            return View(objPlayersList);
        }


        #region 舊寫法create 跟 edit 分開寫
        //public IActionResult Create()
        //{
        //使用viewbag
        //IEnumerable<SelectListItem> CountryList = _unitOfWork.Country.GetAll().Select(u => new SelectListItem
        //{
        //    Text = u.Name,
        //    Value = u.CountryID.ToString()
        //});
        //使用viewbag
        //ViewBag.CountryList = CountryList;  
        //使用viewdata
        //ViewData["CountryList"] = CountryList;

        //使用viewmodel，裡面包括player的所有欄位
        //    PlayerVM playerVM = new()
        //    {
        //        CountryList = _unitOfWork.Country.GetAll().Select(u => new SelectListItem { Text = u.Name, Value = u.CountryID.ToString() }),
        //        Players = new Players()
        //    };
        //    return View(playerVM);
        //}
        //[HttpPost]
        //public IActionResult Create(PlayerVM playerVM)
        //{
        //    if (ModelState.IsValid)
        //    {

        //_unitOfWork.Players.Add(playerVM.Players);
        /* _unitOfWork.Save();*///將所有的資料進行保存並寫重新設定物件內容
                                //新增TempData:用來顯示新增成功的通知
                                //TempData["success"] = "球員新增成功";
                                //return RedirectToAction("Index");
                                //}
                                //確保驗證為false時，不會出現錯誤畫面
                                //    else
                                //    {
                                //        playerVM.CountryList = _unitOfWork.Country.GetAll().Select(u => new SelectListItem { Text = u.Name,Value = u.CountryID.ToString()});
                                //    }
                                //    return View(playerVM);
                                //}

        //找到對應的id，並回傳對應的資料給view
        //public IActionResult Edit(int? id)
        //{
        //    if(id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Players? PlayersFromDb = _unitOfWork.Players.Get(u => u.PlayerID == id);
        //    if(PlayersFromDb == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(PlayersFromDb);
        //}
        //[HttpPost]
        ////由表單post的物件，然後修改
        //public IActionResult Edit(Players obj)
        //{

        //    if (ModelState.IsValid)
        //    {                                                   
        //        _unitOfWork.Players.Update(obj);
        //        _unitOfWork.Save();//將所有的資料進行保存並寫重新設定物件內容
        //        //新增TempData:用來顯示編輯成功的通知
        //        TempData["success"] = "球員編輯成功";
        //        return RedirectToAction("Index");
        //    }

        //    return View();
        //}

        #endregion


        #region 新寫法，整合create 跟 edit

        public IActionResult Upsert(int? id)
        {
            //使用viewmodel，裡面包括player的所有欄位
            PlayerVM playerVM = new()
            {
                CountryList = _unitOfWork.Country.GetAll().Select(u => new SelectListItem { Text = u.Name, Value = u.CountryID.ToString() }), // 國家下拉選單
                Players = new Players(), //球員基本資料
                PlayerStats = new PlayerStats() // 數據資料
            
        };

            //編輯與新增功能畫面
            if(id == null || id == 0)
            {
                //執行新增畫面
                return View(playerVM);
            }
            else
            {
                //執行編輯畫面
                playerVM.Players = _unitOfWork.Players.Get(u => u.PlayerID == id); 
                playerVM.PlayerStats = _unitOfWork.PlayerStats.Get(u => u.PlayerID == id);
                playerVM.PlayerStats ??= new PlayerStats();
                return View(playerVM);
            }          
        }
        [HttpPost]
        public IActionResult Upsert(PlayerVM playerVM,IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string playerPath = Path.Combine(wwwRootPath, @"images\player");
                    //檢查字串是不空的，檢查有沒有圖片
                    if (!string.IsNullOrEmpty(playerVM.Players.ImageURL))
                    {
                        //有新圖，要刪掉舊的
                        var oldImagePath = Path.Combine(wwwRootPath,playerVM.Players.ImageURL.TrimStart('\\'));
                        if(System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }


                    using (var fileStream = new FileStream(Path.Combine(playerPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    playerVM.Players.ImageURL = @"\images\player\" + fileName;
                }

                //新增時要新增照片，修改時要修改照片
                if (playerVM.Players.PlayerID == 0)  //在id還是0的時候，就代表資料還沒進入database(為新資料)，所以可以拿來判斷為新增的操作
                {
                    // 1️.先存 Player，因為PlayerStats 需要 PlayerID（外鍵）
                    _unitOfWork.Players.Add(playerVM.Players);
                    _unitOfWork.Save(); //PlayerID 是 DB 自動產生（Identity），所以要先存，這樣數據才能找到外鍵
                    // 2.確保 PlayerStats 不為 null
                    if (playerVM.PlayerStats != null)
                    {
                        // 3️ 綁 FK
                        playerVM.PlayerStats.PlayerID = playerVM.Players.PlayerID;

                        // 4️ 存 Stats
                        _unitOfWork.PlayerStats.Add(playerVM.PlayerStats);
                        _unitOfWork.Save();
                    }
                   
                }
                else //編輯
                {
                    var playerFromDb = _unitOfWork.Players.Get(u => u.PlayerID == playerVM.Players.PlayerID, includeProperties: "PlayerStats");

                    if (playerFromDb != null)
                    {
                        // 更新 Player
                        playerFromDb.PlayerName = playerVM.Players.PlayerName;
                        playerFromDb.PlayAgo = playerVM.Players.PlayAgo;
                        playerFromDb.Position = playerVM.Players.Position;
                        playerFromDb.PlayerDescription = playerVM.Players.PlayerDescription;
                        playerFromDb.CountryID = playerVM.Players.CountryID;

                        // 更新圖片
                        if (!string.IsNullOrEmpty(playerVM.Players.ImageURL))
                        {
                            playerFromDb.ImageURL = playerVM.Players.ImageURL;
                        }

                        // ⭐ 更新 Stats
                        if (playerFromDb.PlayerStats == null)
                        {
                            playerFromDb.PlayerStats = new PlayerStats
                            {
                                PlayerID = playerFromDb.PlayerID,
                                HR = playerVM.PlayerStats.HR,
                                AVG = playerVM.PlayerStats.AVG,
                                RBI = playerVM.PlayerStats.RBI,
                                OPS = playerVM.PlayerStats.OPS
                            };
                        }
                        else
                        {
                            playerFromDb.PlayerStats.HR = playerVM.PlayerStats.HR;
                            playerFromDb.PlayerStats.AVG = playerVM.PlayerStats.AVG;
                            playerFromDb.PlayerStats.RBI = playerVM.PlayerStats.RBI;
                            playerFromDb.PlayerStats.OPS = playerVM.PlayerStats.OPS;
                        }
                    }


                }
                
                _unitOfWork.Save();//將所有的資料進行保存並寫重新設定物件內容
                                

                TempData["success"] = "球員新增成功";

                return RedirectToAction("Index");
            }
            //確保驗證為false時，不會出現錯誤畫面
            else
            {
                playerVM.CountryList = _unitOfWork.Country.GetAll().Select(u => new SelectListItem { Text=u.Name, Value = u.CountryID.ToString() });
            }
            return View(playerVM);
        }
        #endregion

        #region 刪除功能的舊寫法

        //public IActionResult Delete(int? id)
        //{ 
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Players? PlayersFromDb = _unitOfWork.Players.Get(u => u.PlayerID == id);
        //    if (PlayersFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(PlayersFromDb);
        //}



        //對外的路由 / Action 名稱還是叫 Delete
        //[HttpPost,ActionName("Delete")]
        //public IActionResult DeletePOST(int? id)
        //{
        //    Players? obj = _unitOfWork.Players.Get(u => u.PlayerID == id);
        //    if(obj == null)
        //    {
        //        return NotFound();
        //    }

        //    _unitOfWork.Players.Remove(obj);
        //    _unitOfWork.Save();
        //    //新增TempData:用來顯示刪除成功的通知
        //    TempData["success"] = "球員刪除成功";
        //    return RedirectToAction("Index");
        //}

        #endregion
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var playerToBeDeleted = _unitOfWork.Players.Get(u => u.PlayerID == id);
            if(playerToBeDeleted == null)
            {
                return Json(new { success = false, message = "刪除失敗" });
            }

            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, playerToBeDeleted.ImageURL.TrimStart('\\'));
            if(System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }
            _unitOfWork.Players.Remove(playerToBeDeleted);
            _unitOfWork.Save();
            return Json(new { success = true, message = "刪除成功" });
        }

        #region API CALLS
        [HttpGet]
        //將資料打包為json
        public IActionResult GetAll()
        {
            List<Players> objPlayerList = _unitOfWork.Players.GetAll(includeProperties: "Country,PlayerStats").ToList();
            return Json(new {data = objPlayerList});
        }

        #endregion
    }
}
