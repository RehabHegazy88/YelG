using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using YelG.Model.Basic;

namespace YelG.Model.Auth;

[Table("Departments")]
public class Department : LookupEntity
{
    public virtual ICollection<User> Users { get; set; } = new HashSet<User>();
}
