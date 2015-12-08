using Ookii.Dialogs.Wpf;
using System.Windows;

namespace SketchDataViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            ResizeMode = ResizeMode.NoResize;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MyLoadMenu_Click(object sender, RoutedEventArgs e)
        {
            // display the folder browser dialog
            var dialog = new VistaFolderBrowserDialog();
            if (!dialog.ShowDialog().Value)
            {
                return;
            }

            // get the images directory path
            //myLoadDirectory = dialog.SelectedPath;
        }

        private void MyResampleButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyScaleButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyTranslateButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyDisplayPointsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyResampleSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void MyScaleSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }
    }
}
