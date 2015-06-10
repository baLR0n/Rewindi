using Rewindi.Model.GameLogic.Map;

namespace Rewindi.Model.GameInfo
{
    public class LogEntry
    {
        public Move Move { get; private set; }

        public FieldState[] MapState { get; private set; }

        public int Index { get; set; }

        public LogEntry(Move move, FieldState[] state, int index)
        {
            this.Move = move;
            this.MapState = state;
            this.Index = index;
        }
    }
}
