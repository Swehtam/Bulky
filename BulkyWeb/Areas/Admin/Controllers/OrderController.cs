using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{
		public readonly IUnitOfWork _unitOfWork;
		[BindProperty]
		public OrderVM OrderVM { get; set; }

		public OrderController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Details(int orderId)
		{
			OrderVM = new()
			{
				OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includedProperties: "ApplicationUser"),
				OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includedProperties: "Product")
			};
			return View(OrderVM);
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult UpdateOrderDetail()
		{
			var orderHeaderFromDB = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
			orderHeaderFromDB.Name = OrderVM.OrderHeader.Name;
			orderHeaderFromDB.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
			orderHeaderFromDB.StreetAddress = OrderVM.OrderHeader.StreetAddress;
			orderHeaderFromDB.City = OrderVM.OrderHeader.City;
			orderHeaderFromDB.State = OrderVM.OrderHeader.State;
			orderHeaderFromDB.PostalCode = OrderVM.OrderHeader.PostalCode;
			if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
			{
				orderHeaderFromDB.PostalCode = OrderVM.OrderHeader.PostalCode;
			}
			if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
			{
				orderHeaderFromDB.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			}

			_unitOfWork.OrderHeader.Update(orderHeaderFromDB);
			_unitOfWork.Save();

			TempData["Success"] = "Order Details Updated Successfully.";

			return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDB.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult StartProcessing()
		{
			_unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.Status_InProcess);
			_unitOfWork.Save();

			TempData["Success"] = "Order Started Processing Successfully.";

			return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult ShipOrder()
		{
			_unitOfWork.OrderHeader.UpdateShippingStatus(OrderVM.OrderHeader.Id, OrderVM.OrderHeader.Carrier, OrderVM.OrderHeader.TrackingNumber);
			_unitOfWork.Save();

			TempData["Success"] = "Order Shipped Successfully.";

			return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult CancelOrder()
		{
			var orderHeaderFromDB = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

			if(orderHeaderFromDB.PaymentStatus == SD.PaymentStatus_Approved)
			{
				var options = new RefundCreateOptions()
				{
					Reason = RefundReasons.RequestedByCustomer,
					PaymentIntent = orderHeaderFromDB.PaymentIntentId
				};

				var service = new RefundService();
				Refund refund = service.Create(options);

				_unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDB.Id, SD.Status_Cancelled, SD.Status_Refunded);
			}
			else
			{
				_unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDB.Id, SD.Status_Cancelled, SD.Status_Cancelled);
			}
			_unitOfWork.Save();

			TempData["Success"] = "Order Cancelled Successfully.";

			return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
		}

		[ActionName("Details")]
		[HttpPost]
		public IActionResult Details_PAY_NOW()
		{
			OrderVM.OrderHeader = _unitOfWork.OrderHeader.Get(u=> u.Id == OrderVM.OrderHeader.Id, includedProperties:"ApplicationUser");
			OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includedProperties: "Product");

			//its a company account and who needs to pay
			//stripe logic
			var domain = "https://localhost:7005/";
			var options = new SessionCreateOptions
			{
				SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
				CancelUrl = domain + $"admin/order/details?orderHeaderId={OrderVM.OrderHeader.Id}",
				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
			};

			foreach (var item in OrderVM.OrderDetail)
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
			_unitOfWork.OrderHeader.UpdateStripePaymentId(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
			_unitOfWork.Save();

			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
		}

		public IActionResult PaymentConfirmation(int orderHeaderId)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
			if (orderHeader.PaymentStatus == SD.PaymentStatus_DelayedPayment)
			{
				//this is an order by company
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatus_Approved);
					_unitOfWork.Save();
				}
			}

			return View(orderHeaderId);
		}

		#region APICALLS

		[HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> objOrderHeaders;

			if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
			{
				objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includedProperties: "ApplicationUser").ToList();
			}
			else
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

				objOrderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includedProperties: "ApplicationUser").ToList();
			}

			switch (status)
			{
				case "inprocess":
					objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.Status_InProcess);
					break;
				case "pending":
					objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatus_Pending);
					break;
				case "completed":
					objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.Status_Shipped);
					break;
				case "approved":
					objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.Status_Approved);
					break;
				default:
					break;
			}

			return Json(new { data = objOrderHeaders });
		}

		#endregion
	}
}
