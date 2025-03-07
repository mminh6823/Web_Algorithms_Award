using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web_algorithm_award_Model
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string? Name { get; set; }
        [Display(Name = "Địa chỉ nhà")]
        public string? HomeAddress { get; set; }
        public string? PostalCode { get; set; }
        [Display(Name = "Số điện thoại")]
        override public string? PhoneNumber { get; set; }
    }
}
