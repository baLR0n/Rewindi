using System.Windows;
using PropertyChanged;

namespace Rewindi.WpfApp.Views
{
    /// <summary>
    /// Interaction logic for BoardView.xaml
    /// </summary>
    [ImplementPropertyChanged]
    public partial class BoardView : Window
    {
        public BoardView()
        {
            this.InitializeComponent();
        }
    }
}
