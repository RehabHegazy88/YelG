using Microsoft.AspNetCore.Identity;

namespace YelG.WebSite.Infrastructure.Identity
{
    /// <summary>An admin panel account (Admin or Editor). Not related to the YELG Lens users.</summary>
    public class AdminUser : IdentityUser<long>
    {
        public string DisplayName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>Set for accounts created with a temporary password; held at the change-password page until replaced.</summary>
        public bool MustChangePassword { get; set; }

        /// <summary>UTC.</summary>
        public DateTime AddedDate { get; set; }

        /// <summary>UTC.</summary>
        public DateTime LastModifiedDate { get; set; }
    }

    public class AdminRole : IdentityRole<long>
    {
        public AdminRole()
        {
        }

        public AdminRole(string name) : base(name)
        {
        }
    }
}
