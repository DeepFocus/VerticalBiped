﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpFocus.Models
{
    class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long TwitterId { get; set; }
        public string TwitterHandle { get; set; }
        public string TwitterPhoto { get; set; }
        public int Dogecoins { get; set; }
        public string Mugshot { get; set; }
        public DateTime Created { get; set; }

        //Bonus
        public int Ped { get; set; }
        public int JetPack { get; set; }
        public int Helmet { get; set; }
    }
}
