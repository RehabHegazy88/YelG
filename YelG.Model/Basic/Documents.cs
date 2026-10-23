using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YelG.Model.Basic
{
   public class Documents:MainLookupEntity 
    {

        public string DOC_Path { get; set; } = string.Empty;

        public string DOC_Notes { get; set; } = string.Empty;
        public string DOC_Types { get; set; } = string.Empty;
    }
}
