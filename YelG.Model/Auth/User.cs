
 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YelG.Model.Basic; 
using Microsoft.AspNetCore.Identity;

namespace YelG.Model.Auth
{
    public class RefreshToken : BaseEntity<long>
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public long UserId { get; set; }
        public bool Active => DateTime.UtcNow <= Expires;
        public string RemoteIpAddress { get; set; } = string.Empty;
    }

    public enum Gender
    {
        NotSet = 0,
        Male,
        Female
    }

    public class User : IdentityUser<long>, IBaseEntity
    {
        public User()
        {
            AddedDate = LastModifiedDate = DateTime.Now;
        }

        private readonly List<RefreshToken> _refreshTokens = new List<RefreshToken>();
        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

        [DefaultValue(false)]
        public bool IsBuiltIn { get; set; }

        [DefaultValue(true)]
        public bool IsActive { get; set; } = true;

        [DefaultValue(false)]
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Set when an admin creates the account with a temporary password: the
        /// holder is held at the change-password page on first sign-in until
        /// they choose their own. Cleared once they do.
        /// </summary>
        [DefaultValue(false)]
        public bool MustChangePassword { get; set; }

        public string VerificationCode { get; set; } = "0";
        public Gender? Gender { get; set; }

        /// <summary>Name shown across the app ("مقيم أول", "A. Abdalla").</summary>
        [MaxLength(150)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? RepresentativeRoleTitle { get; set; }

        [MaxLength(10)]
        public string? PreferredLocale { get; set; }

        public DateTime? BirthDate { get; set; }

        public long? DepartmentId { get; set; }
        public virtual Department? Department { get; set; }
        public virtual List<UserRole> UserRole { get; set; } = new();

        public DateTime AddedDate { get; set; }
        public DateTime LastModifiedDate { get; set; } 
    }
}
