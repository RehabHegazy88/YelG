using Microsoft.AspNetCore.Identity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YelG.Model.Auth
{
    public class UserClaim :  IdentityUserClaim<long>
    {
    }
    public class RoleClaim : IdentityRoleClaim<long>
    {
    }
    public class userToken : IdentityUserToken<long>
{
}
}
