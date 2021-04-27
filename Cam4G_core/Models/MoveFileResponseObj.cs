using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cam4G_core.Models
{
    public class MoveFileResponseObj
    {
        public bool IsSuccess { get; set; }
        public int MovedFileCount { get; set; }
        public double MovedFileTotalSize { get; set; }
        public int DeletedNum { get; set; }
        public double DeletedSize { get; set; }
        public string Message { get; set; }
    }
}
