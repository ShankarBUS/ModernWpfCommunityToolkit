using ModernWpf.Toolkit.Controls;
using System.Windows;

namespace ModernWpf.Toolkit.SampleApp
{
    public partial class MainWindow : Window
    {
        Eyedropper globalEyedropper;

        public MainWindow()
        {
            InitializeComponent();
            globalEyedropper = new Eyedropper();
            globalEyedropper.ColorChanged += (s, e) => { TargetSolidColorBrush.Color = e.NewColor; };
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var color = await globalEyedropper.Open();
            TargetSolidColorBrush.Color = color;
        }
    }
}
