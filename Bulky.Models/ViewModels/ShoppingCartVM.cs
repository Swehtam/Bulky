using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bulky.Models.ViewModels
{
    public class ShoppingCartVM
    {
        [ValidateNever]
        public IEnumerable<ShoppingCart> ShoppingCartList { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}
