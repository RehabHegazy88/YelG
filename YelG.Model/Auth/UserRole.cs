
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YelG.Model.Auth
{
public class UserRole : IdentityUserRole<long>
{

        public virtual Role Role { get; set; } = null!;

        public virtual User User { get; set; } = null!;

    }
}
