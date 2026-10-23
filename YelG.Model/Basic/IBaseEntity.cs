using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YelG.Model.Basic
{

    [JsonObject(IsReference = false)]
    public abstract class BaseEntity<TKey>
{

        public BaseEntity()
        {
            AddedDate = LastModifiedDate = DateTime.Now;
}

        [Column(Order = 1)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual TKey Id { get; set; } = default!;


        public DateTime AddedDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

    public bool Deleted { get; set; }
        public DateTime? DeleteDate { get; set; }

         
}

    public abstract class MainBaseEntity<T>: BaseEntity<long> where T : class
{
        
        public long AddedBy_PersonID { get; set; }
       
        public long? ModifiedBy_PersonID { get; set; }

        public string Notes { get; set; } = "";

         [ForeignKey("AddedBy_PersonID")]
        public virtual T AddedBy { get; set; } = null!;

        // Optional: ModifiedBy_PersonID is null until the row is first edited.
        [ForeignKey("ModifiedBy_PersonID")]
        public virtual T? ModifiedBy { get; set; }
    }
    [JsonObject(IsReference = false)]
    public abstract class BaseEntity { 

        public BaseEntity()
        {
            AddedDate = LastModifiedDate = DateTime.Now;
        }
     

        public DateTime AddedDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

    }

    public interface IBaseEntity 
    {
    

        DateTime AddedDate { get; set; }

        DateTime LastModifiedDate { get; set; }

    }
}
