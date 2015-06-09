using System.ComponentModel.Composition;
using Caliburn.Micro;
using PropertyChanged;

namespace Rewindi.ViewModel.ViewModels
{
    [ImplementPropertyChanged]
    public class BoardViewModel
    {
        private readonly IEventAggregator events;
        private readonly IWindowManager windowManager;

        public BoardViewModel()
        {}

        [ImportingConstructor]
        public BoardViewModel(IEventAggregator events, IWindowManager windowManager)
        {
            this.events = events;
            this.windowManager = windowManager;

            this.events.Subscribe(this);
        }
    }
}
