using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YelG.Model.Basic
{
    public class SystemSetting:BaseEntity<long>
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

    }
}
