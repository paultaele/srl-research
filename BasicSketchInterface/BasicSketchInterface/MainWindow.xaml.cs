using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace BasicSketchInterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Window Behaviors

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ResizeControls();

            #region Boilerplate Code

            // initialize an empty list of strokes
            myStrokes = new StrokeCollection();

            // set the stylus and mouse state flags and states
            IsStylusMove = false;
            IsStylusEnd = false;
            IsMouseDown = false;
            IsMouseMove = false;
            IsMouseUp = false;
            IsMouseReady = false;
            PreviousStylusState = StylusState.StylusUp;
            PreviousMouseState = MouseState.MouseUp;

            // add mouse button event handlers to the canvas for mouse up, mouse down, and mouse move
            MyCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(MyCanvas_PreviewMouseDown), true);
            MyCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(MyCanvas_PreviewMouseMove), true);
            MyCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(MyCanvas_PreviewMouseUp), true);

            #endregion
        }

        private void MyWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeControls();
        }

        private void ResizeControls()
        {
            // resize canvas
            MyCanvasBorder.Width = MyCanvasBorder.ActualHeight;

            // resize instructions
            MyInstructionsBorder.Width = MyCanvasBorder.Width;

            // resize buttons
            MyButtonsBorder.Width = MyCanvasBorder.Width;
            MyBackButton.Width = MyBackButton.Height = MyButtonsBorder.ActualHeight;
            MyClearButton.Width = MyClearButton.Height = MyButtonsBorder.ActualHeight;
            MyUndoButton.Width = MyUndoButton.Height = MyButtonsBorder.ActualHeight;
            MySaveButton.Width = MySaveButton.Height = MyButtonsBorder.ActualHeight;
            MyCheckButton.Width = MyCheckButton.Height = MyButtonsBorder.ActualHeight;
            MyNextButton.Width = MyNextButton.Height = MyButtonsBorder.ActualHeight;
        }

        #endregion

        #region Menu Bar Interactions

        private void MyExitItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Button Interactions

        private void MySaveButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyCheckButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyBackButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyNextButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EnableButtons(bool enable)
        {
            MySaveButton.IsEnabled = enable;
            MyCheckButton.IsEnabled = enable;
            MyClearButton.IsEnabled = enable;
            MyUndoButton.IsEnabled = enable;
        }

        #endregion

        #region Fields

        #endregion
    }
}
