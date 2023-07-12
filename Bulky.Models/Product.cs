using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        [Required]
        public string ISBN { get; set; }
        [Required]
        public string Author { get; set; }
        [Required]
        [DisplayName("List Price")]
        [Range(1.00, 1000.00, ErrorMessage = "List Price must be between 1 and 1000")]
        //[DisplayFormat(DataFormatString = "{0:C2}")]
        public double ListPrice { get; set; }

        [Required]
        [DisplayName("Price for 1-50")]
        [Range(1.00, 1000.00, ErrorMessage = "Price for 1-50 must be between 1 and 1000")]
        //[DisplayFormat(DataFormatString = "{0:C2}")]
        public double Price { get; set; }

        [Required]
        [DisplayName("Price for 50+")]
        [Range(1.00, 1000.00, ErrorMessage = "Price for 50+ must be between 1 and 1000")]
        //[DisplayFormat(DataFormatString = "{0:C2}")]
        public double Price50 { get; set; }

        [Required]
        [DisplayName("Price for 100+")]
        [Range(1.00, 1000.00, ErrorMessage = "Price for 100+ must be between 1 and 1000")]
        //[DisplayFormat(DataFormatString = "{0:C2}")]
        public double Price100 { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category Category { get; set; }
        [ValidateNever]
        public string ImageUrl { get; set; }
    }
}
