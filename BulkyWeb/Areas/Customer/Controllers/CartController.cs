using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Bulky.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Bulky.Utility;
using System.Reflection;
using Stripe.Checkout;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Text.Encodings.Web;

namespace BulkyWeb.Areas.Customer.Controllers
{
	[Area("Customer")]
	[Authorize]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailSender _emailSender;
		[BindProperty]
		public ShoppingCartVM ShoppingCartVM { get; set; }

		public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
		{
			_unitOfWork = unitOfWork;
			_emailSender = emailSender;
		}

		public IActionResult Index()
		{
			var claimsIdentity = User.Identity as ClaimsIdentity;
			string userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => (u.ApplicationUserId == userId), includedProperties: "Product"),
				OrderHeader = new()
			};

			foreach (ShoppingCart cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}


			return View(ShoppingCartVM);
		}

		public IActionResult Plus(int cartId)
		{
			ShoppingCart cartObj = _unitOfWork.ShoppingCart.Get(u => (u.Id == cartId));

			cartObj.Count += 1;
			_unitOfWork.ShoppingCart.Update(cartObj);
			_unitOfWork.Save();

			return RedirectToAction(nameof(Index));
		}

		public IActionResult Minus(int cartId)
		{
			ShoppingCart cartObj = _unitOfWork.ShoppingCart.Get(u => (u.Id == cartId));

			if (cartObj.Count > 1)
			{
				cartObj.Count -= 1;
				_unitOfWork.ShoppingCart.Update(cartObj);
				_unitOfWork.Save();
			}

			return RedirectToAction(nameof(Index));
		}

		public IActionResult Remove(int cartId)
		{
			ShoppingCart cartObj = _unitOfWork.ShoppingCart.Get(u => (u.Id == cartId), tracked: true);

			HttpContext.Session.SetInt32(SD.SessionCart,
					_unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartObj.ApplicationUserId).Count() - 1);

			_unitOfWork.ShoppingCart.Remove(cartObj);
			_unitOfWork.Save();

			return RedirectToAction(nameof(Index));
		}

		public IActionResult Summary()
		{
			var claimsIdentity = User.Identity as ClaimsIdentity;
			string userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => (u.ApplicationUserId == userId),
					includedProperties: "Product"),
				OrderHeader = new()
			};

			ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

			ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
			ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
			ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

			foreach (ShoppingCart cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

			return View(ShoppingCartVM);
		}

		[HttpPost]
		[ActionName("Summary")]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = User.Identity as ClaimsIdentity;
			string userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => (u.ApplicationUserId == userId),
				includedProperties: "Product");

			ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
			ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

			foreach (ShoppingCart cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				//its a costumer
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatus_Pending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.Status_Pending;
			}
			else
			{
				//its a company
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatus_DelayedPayment;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.Status_Approved;
			}

			_unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
			_unitOfWork.Save();

			foreach (ShoppingCart cart in ShoppingCartVM.ShoppingCartList)
			{
				OrderDetail orderDetail = new()
				{
					ProductId = cart.ProductId,
					OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
					Price = cart.Price,
					Count = cart.Count
				};
				_unitOfWork.OrderDetail.Add(orderDetail);
				_unitOfWork.Save();
			}

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				//its a regular customer account and we need to capture payment
				//stripe logic
				var domain = "https://localhost:7005/";
				var options = new SessionCreateOptions
				{
					SuccessUrl = domain + $"Customer/Cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + "customer/cart/index",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};

				foreach (var item in ShoppingCartVM.ShoppingCartList)
				{
					var sessionItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions()
						{
							UnitAmount = (long)(item.Price * 100),
							Currency = "brl",
							ProductData = new SessionLineItemPriceDataProductDataOptions()
							{
								Name = item.Product.Title,
							}
						},
						Quantity = item.Count
					};
					options.LineItems.Add(sessionItem);
				}

				var service = new SessionService();
				Session session = service.Create(options);
				_unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();

				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
			}

			return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
		}


		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includedProperties: "ApplicationUser");
			if (orderHeader.PaymentStatus != SD.PaymentStatus_DelayedPayment)
			{
				//this is an order by customer
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(id, SD.Status_Approved, SD.PaymentStatus_Approved);
					_unitOfWork.Save();
				}
			}

			List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
				.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

			_unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
            HttpContext.Session.Clear();

            _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Bulky Web",
                        $"<p>New Order created  - {orderHeader.Id} </p>");

            return View(id);
		}

		private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
		{
			if (shoppingCart.Count <= 50)
			{
				return shoppingCart.Product.Price;
			}
			else if (shoppingCart.Count <= 100)
			{
				return shoppingCart.Product.Price50;
			}
			else
			{
				return shoppingCart.Product.Price100;
			}
		}
	}
}