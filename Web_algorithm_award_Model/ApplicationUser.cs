using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

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
        public override string? PhoneNumber { get; set; }
    }
}