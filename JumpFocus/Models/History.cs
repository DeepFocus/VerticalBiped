using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpFocus.Models
{
    class History
    {
        public int Id { get; set; }
        public Player Player { get; set; }
        public DateTime Played { get; set; }
        public int Altitude { get; set; }
        public int Dogecoins { get; set; }
        public string Picture { get; set; }

        //Bonus
        public int Ped { get; set; }
        public int JetPack { get; set; }
        public int Helmet { get; set; }
    }
}
