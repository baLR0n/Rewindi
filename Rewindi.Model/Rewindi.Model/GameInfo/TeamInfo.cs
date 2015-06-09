using System;

namespace Rewindi.Model.GameInfo
{
    public class TeamInfo
    {
        public string Name { get; set; }

        public TimeSpan TimeLeft { get; set; }

        public int Stones { get; set; }

        public int Rank { get; set; }
    }
}
