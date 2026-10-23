using YelG.Model.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YelG.Model.Basic
{
    public class Error : BaseEntity<long>
    {

        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(10000)]
        public string StackTrace { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; }

        public bool? IsSolved { get; set; }

        public long? SolvedBy { get; set; }

        [MaxLength(10000)]
        public string Comment { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string MethodName { get; set; } = string.Empty;

        public string LineNumber { get; set; } = string.Empty;

        //public virtual SystemAdmin SystemAdmin { get; set; }
    }
    public class ActivityLog : BaseEntity<long>
    {


        public int StatusCode { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string MethodName { get; set; } = string.Empty;

        public string RequestHeader { get; set; } = string.Empty;
        public string MethodType { get; set; } = string.Empty;

        public RoleType Role { get; set; }
        public long? UserID { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;

        public string RequestUri { get; set; } = string.Empty;
        public string AbsoluteUri { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string RequestPostContent { get; set; } = string.Empty;
        //public virtual SystemAdmin SystemAdmin { get; set; }
    }
    public class LogMetadata
    {
        public string RequestContentType { get; set; } = string.Empty;
        public string RequestUri { get; set; } = string.Empty;
        public string RequestMethod { get; set; } = string.Empty;
        public DateTime? RequestTimestamp { get; set; }
        public string ResponseContentType { get; set; } = string.Empty;
        public HttpStatusCode ResponseStatusCode { get; set; }

        public DateTime? ResponseTimestamp { get; set; }


    }
}
