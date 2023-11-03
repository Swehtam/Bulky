using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;

namespace BulkyWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = SD.Role_Admin)]
	public class UserController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly UserManager<IdentityUser> _userManager;
		public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
		{
			_db = db;
			_userManager = userManager;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult RoleManagement(string id)
		{
			string RoleID = _db.UserRoles.FirstOrDefault(u => u.UserId == id).RoleId;
			UserVM userVM = new()
			{
				ApplicationUser = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == id),
				CompaniesList = _db.Companies.ToList(),
				RolesList = _db.Roles.ToList()
			};

			userVM.ApplicationUser.Role = _db.Roles.FirstOrDefault(u => u.Id == RoleID).Name;

			return View(userVM);
		}

		[HttpPost]
		public IActionResult RoleManagement(UserVM userVM)
		{
			var userRoleObj = _db.UserRoles.FirstOrDefault(u => u.UserId == userVM.ApplicationUser.Id);
			string oldRoleName = _db.Roles.FirstOrDefault(u => u.Id == userRoleObj.RoleId).Name;

			ApplicationUser userObj = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == userVM.ApplicationUser.Id);

			if (oldRoleName == userVM.ApplicationUser.Role && userObj.CompanyId == userVM.ApplicationUser.CompanyId)
			{
				userVM.CompaniesList = _db.Companies.ToList();
				userVM.RolesList = _db.Roles.ToList();
				TempData["error"] = "Role did not change";
				return View(userVM);
			}

			if (oldRoleName == SD.Role_Company)
			{
				userObj.CompanyId = null;
			}

			if (userVM.ApplicationUser.Role == SD.Role_Company)
			{
				userObj.CompanyId = userVM.ApplicationUser.CompanyId;
			}

			if(oldRoleName != userVM.ApplicationUser.Role)
			{
				_userManager.RemoveFromRoleAsync(userObj, oldRoleName).GetAwaiter().GetResult();
				_userManager.AddToRoleAsync(userObj, userVM.ApplicationUser.Role).GetAwaiter().GetResult();
			}

			_db.SaveChangesAsync().GetAwaiter().GetResult();

			TempData["success"] = "User Role changed successfully";
			return RedirectToAction("Index");
		}

		#region API CLASS

		[HttpGet]
		public IActionResult GetAll()
		{
			List<ApplicationUser> objUserList = _db.ApplicationUsers.Include(u => u.Company).ToList();

			var userRoles = _db.UserRoles.ToList();
			var roles = _db.Roles.ToList();

			foreach (var objUser in objUserList)
			{
				var roleId = userRoles.FirstOrDefault(u => u.UserId == objUser.Id).RoleId;
				objUser.Role = roles.FirstOrDefault(u => u.Id == roleId).Name;

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
			var objFromdb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
			if (objFromdb == null)
			{
				return Json(new { success = false, message = "Error while Locking/Unlocking" });
			}

			if (objFromdb.LockoutEnd != null && objFromdb.LockoutEnd > DateTime.Now)
			{
				//user is currently locked and we need to unlock
				objFromdb.LockoutEnd = DateTime.Now;
			}
			else
			{
				objFromdb.LockoutEnd = DateTime.Now.AddYears(1000);
			}

			_db.SaveChanges();
			return Json(new { success = true, message = "Locking/Unlocking Successfull" });
		}

		#endregion
	}
}