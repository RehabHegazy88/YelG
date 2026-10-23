using Microsoft.AspNetCore.Identity;

namespace YelG.BLL.Common
{
    public static class IdentityErrors
    {
        /// <summary>Arabic messages for the Identity errors users can actually hit.</summary>
        public static string ToArabic(IdentityError error) => error.Code switch
        {
            "DuplicateUserName" => "اسم المستخدم مستخدم بالفعل.",
            "InvalidUserName" => "اسم المستخدم يقبل الحروف الإنجليزية والأرقام والرموز . _ - @ فقط.",
            "PasswordTooShort" => "كلمة المرور يجب ألا تقل عن 8 خانات.",
            "PasswordRequiresDigit" => "كلمة المرور يجب أن تحتوي على رقم.",
            "PasswordRequiresLower" => "كلمة المرور يجب أن تحتوي على حرف إنجليزي صغير.",
            "PasswordRequiresUpper" => "كلمة المرور يجب أن تحتوي على حرف إنجليزي كبير.",
            "PasswordRequiresNonAlphanumeric" => "كلمة المرور يجب أن تحتوي على رمز.",
            "PasswordMismatch" => "كلمة المرور الحالية غير صحيحة.",
            _ => error.Description
        };

        public static void ThrowIfFailed(IdentityResult result)
        {
            if (!result.Succeeded)
                throw new BusinessRuleException(string.Join(" ", result.Errors.Select(ToArabic)));
        }
    }
}
