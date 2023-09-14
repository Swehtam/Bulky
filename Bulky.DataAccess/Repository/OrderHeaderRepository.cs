using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;

namespace Bulky.DataAccess.Repository
{
	public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
	{
		private ApplicationDbContext _db;

		public OrderHeaderRepository(ApplicationDbContext db) : base(db)
		{
			_db = db;
		}

		public void Update(OrderHeader obj)
		{
			_db.OrderHeaders.Update(obj);
		}

		public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
			var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
			if (orderFromDb != null)
			{
				orderFromDb.OrderStatus = orderStatus;
				if (!string.IsNullOrEmpty(paymentStatus))
				{
					orderFromDb.PaymentStatus = paymentStatus;
				}
			}
		}

		public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
		{
			var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
			if (!string.IsNullOrEmpty(sessionId))
			{
				orderFromDb.SessionId = sessionId;
			}
			if (!string.IsNullOrEmpty(paymentIntentId))
			{
				orderFromDb.PaymentIntentId = paymentIntentId;
				orderFromDb.PaymentDate = DateTime.Now;
			}
		}

		public void UpdateShippingStatus(int id, string carrier, string trackingNumber)
		{
			var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
			orderFromDb.Carrier = carrier;
			orderFromDb.TrackingNumber = trackingNumber;
			orderFromDb.OrderStatus = SD.Status_Shipped;
			orderFromDb.ShippingDate = DateTime.Now;
			if (orderFromDb.PaymentStatus == SD.PaymentStatus_DelayedPayment)
			{
				orderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
			}
		}
	}
}
