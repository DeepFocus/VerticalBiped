using JumpFocus.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpFocus.DAL
{
    class JumpFocusContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<History> Histories { get; set; }
    }
}
