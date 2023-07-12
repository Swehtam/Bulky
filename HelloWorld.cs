// C# program to print Hello World!
using System;

// namespace declaration
namespace HelloWorldApp
{

    // Class declaration
    class Geeks
    {

        // Main Method
        static void Main(string[] args)
        {
            Console.WriteLine(int.TryParse("5555555555555566666666666777777777777778888888888889999999999999900000000000000000000", out _));

            List<ProductRecord> pRecords = _warehouseRepository.GetProductRecords().ToList();
            List<WarehouseEntry> warehouse = new List<WarehouseEntry>();
            foreach (ProductRecord product in pRecords)
            {
                WarehouseEntry entry = new WarehouseEntry();
                entry.ProductId = product.ProductId;
                entry.Quantity = product.Quantity;
                warehouse.Add(entry);
            }
            return new OkObjectResult(warehouse.AsEnumerable());

            // To prevents the screen from 
            // running and closing quickly
            Console.ReadKey();
        }
    }
}


using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Codility.WarehouseApi
{
    public class WarehouseController : Controller
    {
        private readonly IWarehouseRepository _warehouseRepository;

        public WarehouseController(IWarehouseRepository warehouseRepository)
        {
            _warehouseRepository = warehouseRepository;
        }

        // Return OkObjectResult(IEnumerable<WarehouseEntry>)
        public IActionResult GetProducts()
        {
            throw new NotImplementedException();
        }

        // Return OkResult, BadRequestObjectResult(NotPositiveQuantityMessage), or BadRequestObjectResult(QuantityTooLowMessage)
        public IActionResult SetProductCapacity(int productId, int capacity)
        {
            if (capacity <= 0) return new BadRequestObjectResult(new NotPositiveQuantityMessage());

            ProductRecord pRecord = _warehouseRepository.GetProductRecords(c => c.ProductId == productId).FirstOrDefault();

            if (pRecord != null && capacity < pRecord.Quantity) return new BadRequestObjectResult(new QuantityTooLowMessage());

            _warehouseRepository.SetCapacityRecord(productId, capacity);

            return new OkResult();
        }

        // Return OkResult, BadRequestObjectResult(NotPositiveQuantityMessage), or BadRequestObjectResult(QuantityTooHighMessage)
        public IActionResult ReceiveProduct(int productId, int qty)
        {
            if (qty <= 0) return new BadRequestObjectResult(new NotPositiveQuantityMessage());

            ProductRecord pRecord = _warehouseRepository.GetProductRecords(c => c.ProductId == productId).FirstOrDefault();
            CapacityRecord cRecord = _warehouseRepository.GetCapacityRecords(c => c.ProductId == productId).FirstOrDefault();

            int productQuantity = 0;
            if (pRecord != null) productQuantity = pRecord.Quantity;

            if (cRecord == null) return new BadRequestObjectResult(new QuantityTooHighMessage());

            if ((cRecord == null) || ((productQuantity + qty) > cRecord.Capacity)) return new BadRequestObjectResult(new QuantityTooHighMessage());

            _warehouseRepository.SetProductRecord(productId, productQuantity + qty);

            return new OkResult();
        }

        // Return OkResult, BadRequestObjectResult(NotPositiveQuantityMessage), or BadRequestObjectResult(QuantityTooHighMessage)
        public IActionResult DispatchProduct(int productId, int qty)
        {
            if (qty <= 0) return new BadRequestObjectResult(new NotPositiveQuantityMessage());

            ProductRecord pRecord = _warehouseRepository.GetProductRecords(c => c.ProductId == productId).FirstOrDefault();

            int productQuantity = 0;
            if (pRecord != null) productQuantity = pRecord.Quantity;

            if (qty > productQuantity) return new BadRequestObjectResult(new QuantityTooHighMessage());

            _warehouseRepository.SetProductRecord(productId, productQuantity - qty);

            return new OkResult();
        }
    }
}



