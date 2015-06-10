
using System;

namespace Rewindi.Model.GameInfo
{
    public class Move
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Move"/> class.
        /// </summary>
        public Move()
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Move"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public Move(string message)
        {
            this.SpecialMessage = message;
        }

        public int PosX { get; set; }

        public int PosY { get; set; }

        public string SpecialMessage { get; set; }

        public string CalculationTime { get; set; }

        public TimeSpan TimeLeft { get; set; }

        public string TeamName { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.TeamName))
            {
                return this.SpecialMessage;
            }

            if(string.IsNullOrEmpty(this.SpecialMessage))
            {
                return string.Format("{0} moves to ({1},{2}) [{3}]", this.TeamName, this.PosX, this.PosY,
                    this.CalculationTime);
            }

            return string.Format("{0} moves to ({1},{2}) [{3}] - [{4}]", this.TeamName, this.PosX, this.PosY,
                this.CalculationTime, this.SpecialMessage);
        }
    }
}
