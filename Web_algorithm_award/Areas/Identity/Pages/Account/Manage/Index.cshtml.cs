// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Web_algorithm_award_Model;

namespace Web_algorithm_award.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        [BindProperty]
        public string HomeAddress { get; set; }

        [BindProperty]
        public string PostalCode { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {

            [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Địa chỉ")]
            public string HomeAddress { get; set; }

            [Display(Name = "Mã bưu điện")]
            public string PostalCode { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            HomeAddress = user.HomeAddress;
            PostalCode = user.PostalCode;
            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);

                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin nhập vào.";

                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật số điện thoại.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";

            return RedirectToPage();
        }
    }
}