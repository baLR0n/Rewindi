using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rewindi.Model.GameInfo
{
    public class MapInfo
    {
        public string MapName { get; set; }

        public int Players { get; set; }

        public int Overrides { get; set; }

        public int Bombs { get; set; }

        public int BombPower { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Transitions { get; set; }
    }
}
