using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyWeb.ViewComponents
{
	public class ShoppingCartViewComponent : ViewComponent
	{
		private readonly IUnitOfWork _unitOfWork;
		public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<IViewComponentResult> InvokeAsync()
		{
			var claimsIdentity = User.Identity as ClaimsIdentity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			if (claim != null)
			{
				var cartCount = HttpContext.Session.GetInt32(SD.SessionCart);
				if(cartCount == null)
				{
					HttpContext.Session.SetInt32(SD.SessionCart,
						_unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).Count());
					cartCount = HttpContext.Session.GetInt32(SD.SessionCart);
				}

				return View(cartCount);
			}

			HttpContext.Session.Clear();
			return View(0);
		}
	}
}
