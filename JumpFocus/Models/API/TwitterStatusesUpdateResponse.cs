using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpFocus.Models.API
{
    class TwitterStatusesUpdateResponse
    {
        public DateTime created_at { get; set; }
        public long id { get; set; }
        public string id_str { get; set; }
        public string text { get; set; }
        public bool truncated { get; set; }
        public string lang { get; set; }
    }
}
