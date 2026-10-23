using YelG.DAL.Abstractions;

namespace YelG.BLL.Common
{
    public static class CurrentUserExtensions
    {
        public static long RequireUserId(this ICurrentUser user) =>
            user.UserId ?? throw new ForbiddenException("انتهت الجلسة. سجّلي الدخول مرة أخرى.");

        public static long RequireAdmin(this ICurrentUser user)
        {
            var id = user.RequireUserId();
            return user.IsAdmin ? id : throw new ForbiddenException("هذه العملية متاحة للـAdmin فقط.");
        }
    }
}
