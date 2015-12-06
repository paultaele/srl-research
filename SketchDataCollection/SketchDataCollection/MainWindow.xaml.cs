using System;
using Ookii.Dialogs.Wpf;
using System.Windows;

namespace SketchDataCollection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constructor and Loader

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

        #endregion

        #region Menu Interactions

        private void MyLoadMenu_Click(object sender, RoutedEventArgs e)
        {
            // display the folder browser dialog
            var dialog = new VistaFolderBrowserDialog();
            if (!dialog.ShowDialog().Value)
            {
                return;
            }

            // get the images directory path
            string dirPath = dialog.SelectedPath;

            //
            MySettingsPanel.Visibility = Visibility.Visible;
        }

        #endregion

        private void MySettingsOkButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MySettingsCancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MySaveDirectoryButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyRectangularAreaButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MySquareAreaButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
