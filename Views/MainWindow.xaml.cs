using System.Windows;

namespace Beb64.GUI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var about = new Views.AboutWindow { Owner = this };
            about.ShowDialog();
        }
    }
}
