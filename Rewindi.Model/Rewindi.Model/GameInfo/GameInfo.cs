using System;
using System.Collections.Generic;
using System.Drawing;

namespace Rewindi.Model.GameInfo
{
    public class GameInfo
    {
        public MapInfo Map { get; set; }

        public int Players { get; set; }

        public int BombPower { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public Dictionary<string, TeamInfo> LineUp { get; set; }

        public Dictionary<string, Brush> TeamColors { get; set; }
    }
}
