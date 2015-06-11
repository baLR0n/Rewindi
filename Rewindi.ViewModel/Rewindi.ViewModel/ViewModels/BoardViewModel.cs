using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using Caliburn.Micro;
using PropertyChanged;
using Rewindi.Model.GameInfo;
using Rewindi.Model.GameLogic;
using Rewindi.Model.GameLogic.Map;
using Rewindi.Model.Parsing;

namespace Rewindi.ViewModel.ViewModels
{
    [ImplementPropertyChanged]
    public class BoardViewModel
    {
        private readonly IEventAggregator events;
        private readonly IWindowManager windowManager;

        private Timer playtimer;

        /// <summary>
        /// Gets or sets the game logic.
        /// </summary>
        /// <value>
        /// The game logic.
        /// </value>
        public GameLogic GameLogic { get; set; }

        /// <summary>
        /// A dictionary which contains the player names and their number.
        /// </summary>
        private readonly Dictionary<string, int> playerNumbers = new Dictionary<string, int>();

        /// <summary>
        /// Gets the list of logged moves.
        /// </summary>
        /// <value>
        /// The logged moves.
        /// </value>
        public List<Move> LoggedMoves { get; private set; }

        /// <summary>
        /// Gets or sets the log entries.
        /// </summary>
        /// <value>
        /// The log entries.
        /// </value>
        public ObservableCollection<LogEntry> LogEntries { get; set; }

        /// <summary>
        /// Gets or sets the index of the selected log entry.
        /// </summary>
        /// <value>
        /// The index of the selected log.
        /// </value>
        public int SelectedLogIndex { get; set; }

        /// <summary>
        /// Gets the current map to display.
        /// </summary>
        /// <value>
        /// The current map to display.
        /// </value>
        public ObservableCollection<FieldState> CurrentMapToDisplay { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoardViewModel"/> class.
        /// </summary>
        public BoardViewModel()
        {
            playtimer = new Timer(1000.0);
            playtimer.Elapsed += this.PlaytimerOnElapsed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoardViewModel"/> class.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="windowManager">The window manager.</param>
        [ImportingConstructor]
        public BoardViewModel(IEventAggregator events, IWindowManager windowManager)
        {
            this.events = events;
            this.windowManager = windowManager;
            playtimer = new Timer(1000.0);
            playtimer.Elapsed += this.PlaytimerOnElapsed;

            this.events.Subscribe(this);
        }

        /// <summary>
        /// Occurs when a map file is dropped.
        /// </summary>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        public void DropMap(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (File.Exists(files[0]))
                {
                    this.MapDropLabelText = files[0];

                    string[] mapdata = File.ReadAllLines(files[0]);

                    this.GameLogic = new GameLogic();
                    this.GameLogic.ReadMapData(mapdata);

                    if (this.GameLogic != null)
                    {
                        this.GameWidth = (this.GameLogic.MapWidth * 20) + 19;
                        this.GameHeight = (this.GameLogic.MapHeight * 20) + 19;

                        if (this.GameLogic.GameStates.Count > this.SelectedLogIndex)
                        {
                            this.InitializeBoardValues();

                            //ToDo: GameInfo-Objekt füllen. StartDate, enddate, etc.
                        }

                        // Start the game simulation if the log has alreay been dropped.
                        if (this.LoggedMoves != null && this.GameLogic.GameStates != null)
                        {
                            this.PlayTheGame();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the board values.
        /// </summary>
        private void InitializeBoardValues()
        {
            this.CurrentMapToDisplay = new ObservableCollection<FieldState>(this.GameLogic.GameStates[this.SelectedLogIndex].ToList());

            this.LogEntries = new ObservableCollection<LogEntry>();
            this.LogEntries.Add(new LogEntry(new Move("Initial setup"), this.CurrentMapToDisplay.ToArray(), -1));

            this.Players = this.GameLogic.PlayerCount;
            this.BombPower = this.GameLogic.BombStrength;
        }

        /// <summary>
        /// Occurs when a log file is dropped.
        /// </summary>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        public void DropLog(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (File.Exists(files[0]))
                {
                    this.LogDropLabelText = files[0];

                    // Parse log, get moves and play the game from start to the end.
                    GameInfoParser parser = new GameInfoParser();
                    this.LoggedMoves = parser.ParseLog(files[0]);

                    if (this.LoggedMoves != null && this.GameLogic != null && this.GameLogic.GameStates != null)
                    {
                        this.PlayTheGame();
                    }
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:SelectionChanged" /> event. Changes the state of the map to display.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void OnSelectionChanged(EventArgs args)
        {
            SelectionChangedEventArgs changedArgs = (SelectionChangedEventArgs) args;
            
            // If you are processing forward, show changed fields.
            if (changedArgs.RemovedItems.Count > 0 && changedArgs.AddedItems.Count > 0 &&
                this.LogEntries.IndexOf((LogEntry) changedArgs.AddedItems[0]) < this.LogEntries.IndexOf((LogEntry) changedArgs.RemovedItems[0]))
            {
                for (int i = 0; i < this.CurrentMapToDisplay.Count; i++)
                {
                    this.CurrentMapToDisplay[i].Affected = false;
                    if (this.CurrentMapToDisplay[i].Type != this.LogEntries[this.SelectedLogIndex].MapState[i].Type)
                    {
                        this.CurrentMapToDisplay[i].Type = this.LogEntries[this.SelectedLogIndex].MapState[i].Type;
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.CurrentMapToDisplay.Count; i++)
                {
                    this.CurrentMapToDisplay[i].Affected = false;
                    if (this.CurrentMapToDisplay[i].Type != this.LogEntries[this.SelectedLogIndex].MapState[i].Type)
                    {
                        this.CurrentMapToDisplay[i].Type = this.LogEntries[this.SelectedLogIndex].MapState[i].Type;
                        this.CurrentMapToDisplay[i].Affected = true;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the drag enter.
        /// </summary>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        public void PreviewDragEnter(DragEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Display the next move.
        /// </summary>
        public void Next()
        {
            this.SelectedLogIndex++;    
        }

        /// <summary>
        /// Gets a value indicating whether this instance can next.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can next; otherwise, <c>false</c>.
        /// </value>
        public bool CanNext
        {
            get
            {
                return this.LogEntries != null && this.SelectedLogIndex < this.LogEntries.Count - 1;
            }
        }

        /// <summary>
        /// Display the previous move.
        /// </summary>
        public void Previous()
        {
            this.SelectedLogIndex--;
        }

        /// <summary>
        /// Gets a value indicating whether this instance can previous.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can previous; otherwise, <c>false</c>.
        /// </value>
        public bool CanPrevious
        {
            get
            {
                return this.LogEntries != null && this.SelectedLogIndex > 0;
            }
        }

        /// <summary>
        /// Starts or stops the autoplay.
        /// </summary>
        public void PlayStop()
        {
            this.IsPlaying = !this.IsPlaying;

            if (this.IsPlaying)
            {
                this.playtimer.Start();
            }
            else
            {
                this.playtimer.Stop();
            }
        }

        /// <summary>
        /// Replays the game from the log entries.
        /// </summary>
        private void PlayTheGame()
        {
            for (int i = 1; i <= this.GameLogic.PlayerCount; i++)
            {
                if (!this.playerNumbers.ContainsKey(this.LoggedMoves[this.LoggedMoves.Count - i].TeamName))
                {
                    this.playerNumbers.Add(this.LoggedMoves[this.LoggedMoves.Count - i].TeamName, i);
                }
            }

            int gameStateIndex = 1;
            for (int i = this.LoggedMoves.Count - 1; i >= 0; i--)
            {
                // Set stone for this move and add log entry.
                this.GameLogic.SetStone(this.LoggedMoves[i].PosX, this.LoggedMoves[i].PosY, new byte(),
                    this.playerNumbers[this.LoggedMoves[i].TeamName]);

                this.LogEntries.Add(new LogEntry(this.LoggedMoves[i],
                    this.GameLogic.GameStates[gameStateIndex],
                    playerNumbers[this.LoggedMoves[i].TeamName]));

                gameStateIndex++;
            }
        }

        /// <summary>
        /// Occurs when the play timer´s timespan ellapsed. Displays the next move.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="elapsedEventArgs">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void PlaytimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (this.IsPlaying)
            {
                this.Next();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is playing.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is playing; otherwise, <c>false</c>.
        /// </value>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Gets or sets the map drop label text.
        /// </summary>
        /// <value>
        /// The map drop label text.
        /// </value>
        public string MapDropLabelText { get; set; }

        /// <summary>
        /// Gets or sets the log drop label text.
        /// </summary>
        /// <value>
        /// The log drop label text.
        /// </value>
        public string LogDropLabelText { get; set; }

        /// <summary>
        /// Gets or sets the play or stop button text.
        /// </summary>
        /// <value>
        /// The play or stop button text.
        /// </value>
        public string PlayOrStop 
        {
            get { return this.IsPlaying ? "Stop" : "Play"; }
        }

        /// <summary>
        /// Gets the width of the game.
        /// </summary>
        /// <value>
        /// The width of the game.
        /// </value>
        public int GameWidth { get; set; }

        /// <summary>
        /// Gets the height of the game.
        /// </summary>
        /// <value>
        /// The height of the game.
        /// </value>
        public int GameHeight { get; set; }

        /// <summary>
        /// Gets the amount of players.
        /// </summary>
        /// <value>
        /// The players.
        /// </value>
        public int Players { get; private set; }

        /// <summary>
        /// Gets or sets the scores. For later versions.
        /// </summary>
        /// <value>
        /// The scores.
        /// </value>
        public ObservableCollection<int> Scores { get; set; }

        /// <summary>
        /// Gets the bomb power.
        /// </summary>
        /// <value>
        /// The bomb power.
        /// </value>
        public int BombPower { get; private set; }

        /// <summary>
        /// Gets the start date.
        /// </summary>
        /// <value>
        /// The start date.
        /// </value>
        public DateTime StartDate{ get; private set; }
        
        /// <summary>
        /// Gets the end date.
        /// </summary>
        /// <value>
        /// The end date.
        /// </value>
        public DateTime EndDate { get; private set; }
    }
}
