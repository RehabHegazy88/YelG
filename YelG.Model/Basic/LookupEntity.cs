using YelG.Model.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YelG.Model.Basic
{
    
    public class LookupEntity : BaseEntity<long>
    {
        [MaxLength(100)]
        public virtual string EnglishName { get; set; } = string.Empty;

        [MaxLength(100)]
        public virtual string ArabicName { get; set; } = string.Empty;

        public string? EnglishDescription { get; set; }

        public string? ArabicDescription { get; set; }

        public Guid UniqueId { get; set; } = Guid.NewGuid();
    }

    public class MainLookupEntity : MainBaseEntity<User>
    {
        [MaxLength(100)]
        public virtual string EnglishName { get; set; } = string.Empty;

        [MaxLength(100)]
        public virtual string ArabicName { get; set; } = string.Empty;

        public string? EnglishDescription { get; set; }

        public string? ArabicDescription { get; set; }

        public Guid UniqueId { get; set; } = Guid.NewGuid();


        //public bool Deleted { get; set; }
        //public DateTime? DeleteDate { get; set; }
    }

    public class MiniLookupEntity : LookupEntity
    {
    }
}
