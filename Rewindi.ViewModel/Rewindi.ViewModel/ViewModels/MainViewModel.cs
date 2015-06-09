using System.ComponentModel.Composition;
using Caliburn.Micro;
using PropertyChanged;

namespace Rewindi.ViewModel.ViewModels
{
    [Export(typeof(MainViewModel))]
    [ImplementPropertyChanged]
    public class MainViewModel : PropertyChangedBase
    {
        private readonly IEventAggregator events;
        private readonly IWindowManager windowManager;
        private const string WindowTitleDefault = "Default Title";

        private string windowTitle = WindowTitleDefault;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="windowManager">The window manager.</param>
        [ImportingConstructor]
        public MainViewModel(IEventAggregator events, IWindowManager windowManager)
        {
            this.events = events;
            this.windowManager = windowManager;

            this.events.Subscribe(this);
        }

        /// <summary>
        /// Gets or sets the window title.
        /// </summary>
        /// <value>
        /// The window title.
        /// </value>
        public string WindowTitle
        {
            get { return windowTitle; }
            set
            {
                windowTitle = value;
                this.NotifyOfPropertyChange(() => this.WindowTitle);
            }
        }

        public void NewBoard()
        {
            windowManager.ShowWindow(new BoardViewModel());
        }
    }
}