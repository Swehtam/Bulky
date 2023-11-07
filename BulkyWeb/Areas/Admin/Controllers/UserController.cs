using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string id)
        {
            UserVM userVM = new()
            {
                ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == id),
                CompaniesList = _unitOfWork.Company.GetAll(),
                RolesList = _roleManager.Roles.ToList()
            };

            userVM.ApplicationUser.Role = _userManager.GetRolesAsync(userVM.ApplicationUser)
                .GetAwaiter().GetResult().FirstOrDefault();

            return View(userVM);
        }

        [HttpPost]
        public IActionResult RoleManagement(UserVM userVM)
        {
            var userObj = _unitOfWork.ApplicationUser.Get(u => u.Id == userVM.ApplicationUser.Id, tracked: true);

            string oldRoleName = _userManager.GetRolesAsync(userObj).GetAwaiter().GetResult().FirstOrDefault();

            if (oldRoleName == userVM.ApplicationUser.Role && userObj.CompanyId == userVM.ApplicationUser.CompanyId)
            {
                userVM.CompaniesList = _unitOfWork.Company.GetAll();
                userVM.RolesList = _roleManager.Roles.ToList();
                TempData["error"] = "Role did not change";
                return View(userVM);
            }
            
            if (oldRoleName != userVM.ApplicationUser.Role)
            {
                _userManager.RemoveFromRoleAsync(userObj, oldRoleName).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(userObj, userVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }

            if (oldRoleName == SD.Role_Company)
            {
                _unitOfWork.ApplicationUser.UpdateCompany(userObj.Id, null);
            }

            if (userVM.ApplicationUser.Role == SD.Role_Company)
            {
                _unitOfWork.ApplicationUser.UpdateCompany(userObj.Id, userVM.ApplicationUser.CompanyId);
            }
            
            _unitOfWork.Save();

            TempData["success"] = "User Role changed successfully";
            return RedirectToAction("Index");
        }

        #region API CLASS

        [HttpGet]
        public IActionResult GetAll()
        {
            IEnumerable<ApplicationUser> objUserList = _unitOfWork.ApplicationUser.GetAll(includedProperties: "Company");

            foreach (var objUser in objUserList)
            {
                objUser.Role = _userManager.GetRolesAsync(objUser).GetAwaiter().GetResult().FirstOrDefault();

                if (objUser.Company == null)
                {
                    objUser.Company = new() { Name = "" };
                }
            }

            return Json(new { data = objUserList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var objFromdb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);
            if (objFromdb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if (objFromdb.LockoutEnd != null && objFromdb.LockoutEnd > DateTime.Now)
            {
                //user is currently locked and we need to unlock
                _unitOfWork.ApplicationUser.UnlockUser(objFromdb.Id);
            }
            else
            {
                _unitOfWork.ApplicationUser.LockUser(objFromdb.Id);
            }

            _unitOfWork.Save();
            return Json(new { success = true, message = "Locking/Unlocking Successfull" });
        }

        #endregion
    }
}