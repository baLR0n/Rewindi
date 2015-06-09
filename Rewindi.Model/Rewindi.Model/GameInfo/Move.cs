
using System;

namespace Rewindi.Model.GameInfo
{
    public class Move
    {
        public int PosX { get; set; }

        public int PosY { get; set; }

        public StoneType StoneType{ get; set; }

        public TimeSpan CalculationTime { get; set; }

        public TimeSpan TimeLeft { get; set; }

        public string TeamName { get; set; }
    }
}
