using System.ComponentModel.DataAnnotations;

namespace YelG.AdminPortal.Models
{
    public sealed class LoginViewModel
    {
        [Required(ErrorMessage = "اكتبي اسم المستخدم.")]
        [StringLength(80)]
        [Display(Name = "اسم المستخدم")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتبي كلمة المرور.")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public sealed class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "اكتبي كلمة المرور الحالية.")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الحالية")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتبي كلمة المرور الجديدة.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "كلمة المرور يجب ألا تقل عن 8 خانات.")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الجديدة")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتبي كلمة المرور الجديدة مرة أخرى.")]
        [Compare(nameof(NewPassword), ErrorMessage = "كلمتا المرور غير متطابقتين.")]
        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
