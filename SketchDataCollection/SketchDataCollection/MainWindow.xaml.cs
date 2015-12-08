using System;
using Ookii.Dialogs.Wpf;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Input;
using Srl.Xml;
using Srl.Tools;
using System.Windows.Controls;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Linq;

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
            // initialize an empty list of strokes
            myStrokes = new StrokeCollection();

            // set the stylus and mouse state flags
            IsStylusMove = false;
            IsStylusEnd = false;
            IsMouseDown = false;
            IsMouseMove = false;
            IsMouseUp = false;
            IsMouseReady = false;

            // add mouse button event handlers to the canvas for mouse up, mouse down, and mouse move
            MyCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(MyCanvas_PreviewMouseDown), true);
            MyCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(MyCanvas_PreviewMouseMove), true);
            MyCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(MyCanvas_PreviewMouseUp), true);
        }

        #endregion

        #region Mouse and Stylus Events

        private void MyCanvas_StylusButtonDown(object sender, StylusButtonEventArgs e)
        {
            // update the stylus interaction flags
            IsStylusMove = true;

            // initialize the points and times
            myPoints = new StylusPointCollection();
            myTimes = new List<int>();
            myTimeOffset = 0;

            // update the stroke with the initial x, y, and time
            UpdateStroke(
                e.GetPosition(MyCanvas).X,
                e.GetPosition(MyCanvas).Y,
                myPoints,
                myTimes,
                true);

            // enable interaction buttons
            EnableButtons(true);
        }

        private void MyCanvas_StylusMove(object sender, StylusEventArgs e)
        {
            // update the stroke with the current x, y, and time
            UpdateStroke(
                e.GetPosition(MyCanvas).X,
                e.GetPosition(MyCanvas).Y,
                myPoints,
                myTimes,
                false);
        }

        private void MyCanvas_StylusButtonUp(object sender, StylusButtonEventArgs e)
        {
            // update the stylus interaction flags
            IsStylusMove = false;
            IsStylusEnd = true;

            // update the stroke with the final x, y, and time
            UpdateStroke(
                e.GetPosition(MyCanvas).X,
                e.GetPosition(MyCanvas).Y,
                myPoints,
                myTimes,
                false);

            // add stroke to the list
            Stroke stroke = new Stroke(myPoints);
            stroke.AddPropertyData(SketchTools.TIMES_GUID, myTimes.ToArray());
            myStrokes.Add(stroke);
        }

        private void MyCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // update the stylus interaction flags
            IsMouseDown = !IsMouseDown;
            IsMouseMove = false;
            if (!IsMouseDown) { return; }
            if (IsStylusMove) { return; }

            // initialize the points and times
            myPoints = new StylusPointCollection();
            myTimes = new List<int>();
            myTimeOffset = 0;

            // update the stroke with the initial x, y, and time
            UpdateStroke(
                e.GetPosition(MyCanvas).X,
                e.GetPosition(MyCanvas).Y,
                myPoints,
                myTimes,
                true);

            // enable interaction buttons
            EnableButtons(true);
        }

        private void MyCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // update the stylus interaction flags
            if (myPoints == null) { return; }
            if (e.LeftButton != MouseButtonState.Pressed) { return; }
            if (IsMouseDown) { return; }
            if (!IsMouseMove) { IsMouseMove = true; return; }
            if (!IsMouseReady) { IsMouseReady = true; return; }
            if (IsStylusMove) { return; }

            // update the stroke with the current x, y, and time
            UpdateStroke(
                e.GetPosition(MyCanvas).X,
                e.GetPosition(MyCanvas).Y,
                myPoints,
                myTimes,
                false);
        }

        private void MyCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // update the stylus interaction flags
            if (myPoints == null) { return; }
            IsMouseUp = !IsMouseUp;
            if (IsMouseUp) { return; }
            if (IsStylusMove) { return; }
            if (IsStylusEnd) { IsStylusEnd = false; return; }

            // update the stroke with the final x, y, and time
            UpdateStroke(
                e.GetPosition(MyCanvas).X,
                e.GetPosition(MyCanvas).Y,
                myPoints,
                myTimes,
                false);

            // add the stroke to the list
            Stroke stroke = new Stroke(myPoints);
            stroke.AddPropertyData(SketchTools.TIMES_GUID, myTimes.ToArray());
            myStrokes.Add(stroke);
        }

        #endregion

        #region Menu Bar Interaction

        private void MyLoadMenu_Click(object sender, RoutedEventArgs e)
        {
            // display the folder browser dialog
            var dialog = new VistaFolderBrowserDialog();
            if (!dialog.ShowDialog().Value)
            {
                return;
            }

            // get the images directory path
            myLoadDirectory = dialog.SelectedPath;

            //
            MySettingsPanel.Visibility = Visibility.Visible;
        }

        #endregion

        #region Settings Panel Interaction

        private void MySettingsOkButton_Click(object sender, RoutedEventArgs e)
        {
            bool isNumeric;

            // get user number
            string userNumberText = MyUserNumberBox.Text;
            int userNumber;
            isNumeric = int.TryParse(userNumberText, out userNumber);
            if (!isNumeric || userNumber < 0)
            {
                MessageBox.Show("User number is not a positive integer.");
                return;
            }

            // get iteration count
            string iterationCountText = MyIterationCountBox.Text;
            int iterationCount;
            isNumeric = int.TryParse(iterationCountText, out iterationCount);
            if (!isNumeric || iterationCount < 0)
            {
                MessageBox.Show("Iteration count is not a positive integer.");
                return;
            }

            // get checkbox values
            bool showPreviewImage = MyPreviewImageCheckBox.IsChecked.Value;
            bool showTraceImage = MyTraceImageCheckBox.IsChecked.Value;
            bool randomizePrompts = MyRandomizePromptsCheckBox.IsChecked.Value;

            // get radio button values
            bool isRectangularArea = MyRectangularAreaButton.IsChecked.Value;

            // get save directory
            if (MySaveDirectoryBox.Text.Equals(""))
            {
                MessageBox.Show("Save directory is blank.");
                return;
            }
            string saveDirectory = MySaveDirectoryBox.Text;

            // disable menu and close settings panel
            MyMenu.IsEnabled = false;
            MySettingsPanel.Visibility = Visibility.Collapsed;

            // set preview and trace image flags
            myShowPreviewImage = showPreviewImage;
            myShowTraceImage = showTraceImage;

            // set up interface
            SetupInterface(userNumber,
                iterationCount,
                showPreviewImage,
                showTraceImage,
                randomizePrompts,
                isRectangularArea,
                myLoadDirectory,
                saveDirectory);
        }

        private void SetupInterface(int user, int count, bool preview, bool trace, bool random, bool isRect, string loadDir, string saveDir)
        {
            // display interface content
            MyInstructionDisplay.Visibility = Visibility.Visible;
            MyProgressPanel.Visibility = Visibility.Visible;
            MyClearButton.Visibility = Visibility.Visible;
            MyUndoButton.Visibility = Visibility.Visible;
            MySubmitButton.Visibility = Visibility.Visible;

            // explicitly set up canvas size
            // NOTE: needed for future referencing canvas' size when adding image
            MyCanvasBorder.Visibility = Visibility.Visible;
            if (isRect)
            {
                MyCanvasBorder.Width = MyCanvasBorder.ActualWidth;
                MyCanvasBorder.Height = MyCanvasBorder.ActualHeight;
            }
            else
            {
                MyCanvasBorder.Width = MyCanvasBorder.ActualHeight;
                MyCanvasBorder.Height = MyCanvasBorder.ActualHeight;
            }

            // set up contents
            // Tuple< LABEL , IMAGE_PATH , SAVE_PATH >
            string userNumber = Prepend(user.ToString(), 2, "0");
            string userDir = saveDir + @"\" + userNumber;
            var contents = new List<Tuple<string, string, string>>();
            Directory.CreateDirectory(userDir);
            foreach (string imagePath in Directory.GetFiles(myLoadDirectory))
            {
                if (imagePath.EndsWith(".png"))
                {
                    string label = Path.GetFileNameWithoutExtension(imagePath);

                    string savePath, iterationCount;
                    for (int i = 0; i < count; ++i)
                    {
                        iterationCount = Prepend(i.ToString(), 3, "0");
                        savePath = label + "_" + userNumber + "_" + iterationCount;
                        contents.Add(new Tuple<string, string, string>(label, imagePath, userDir + @"\" + savePath + ".xml"));
                    }
                }
            }
            if (random)
            {
                var rnd = new Random();
                contents = contents.OrderBy(x => rnd.Next()).ToList();
            }
            myContents = contents;

            // load initial content
            myIndexer = 0;
            MyTotalCountBlock.Text = contents.Count.ToString();
            LoadContent(contents, myIndexer, preview, trace);
        }

        private void LoadContent(List<Tuple<string,string,string>> contents, int index, bool preview, bool trace)
        {
            // get content
            var content = contents[index];
            string label = content.Item1;
            string imagePath = content.Item2;
            string savePath = content.Item3;
            MyCurrentCountBlock.Text = index.ToString();

            // get image and info
            Image image = new Image();
            image.Source = new BitmapImage(new Uri(imagePath));
            double imageWidth = image.Source.Width;
            double imageHeight = image.Source.Height;

            //
            MyLabelDisplay.Text = label;

            //
            if (preview)
            {
                double controlWidth = MyPreviewImage.ActualHeight;
                double controlHeight = MyPreviewImage.ActualHeight;
                double scaleFactor = ScaleFactor(controlWidth, controlHeight, imageWidth, imageHeight);

                image.Width = imageWidth * scaleFactor;
                image.Height = imageHeight * scaleFactor;
                MyPreviewImage.Source = image.Source;
                MyPreviewImageControl.Background = Brushes.White;
            }

            //
            if (trace)
            {
                //
                MyCanvas.DefaultDrawingAttributes.Color = Colors.Red;
                if (myPreviewImage != null)
                {
                    MyCanvas.Children.Remove(myPreviewImage);
                }

                //
                double canvasWidth = MyCanvasBorder.Width;
                double canvasHeight = MyCanvasBorder.Height;
                double scaleFactor = ScaleFactor(canvasWidth, canvasHeight, imageWidth, imageHeight);

                //
                image.Width = imageWidth * scaleFactor;
                image.Height = imageHeight * scaleFactor;
                InkCanvas.SetLeft(image, (canvasWidth / 2.0) - (imageWidth / 2.0));
                InkCanvas.SetTop(image, (canvasHeight / 2.0) - (imageHeight / 2.0));
                MyCanvas.Children.Add(image);

                myPreviewImage = image;
            }
        }

        private void MySettingsCancelButton_Click(object sender, RoutedEventArgs e)
        {
            myLoadDirectory = "";

            MyUserNumberBox.Text = "";
            MyIterationCountBox.Text = "";

            MyPreviewImageCheckBox.IsChecked = false;
            MyTraceImageCheckBox.IsChecked = false;
            MyRandomizePromptsCheckBox.IsChecked = false;

            MyRectangularAreaButton.IsChecked = false;
            MySquareAreaButton.IsChecked = false;

            MySaveDirectoryBox.Text = "";

            MySettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void MySaveDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            // display the folder browser dialog
            var dialog = new VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            if (!dialog.ShowDialog().Value)
            {
                return;
            }

            // get the images directory path
            string dirPath = dialog.SelectedPath;

            //
            MySaveDirectoryBox.Text = dirPath;
        }

        #endregion

        #region Interface Interactions

        private void MySubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // save strokes
            var content = myContents[myIndexer];
            string label = content.Item1;
            string savePath = content.Item3;

            var processor = new SketchXmlProcessor();
            processor.Write(savePath, label, myStrokes);

            // clear the strokes
            ClearStrokes();

            // load next content
            ++myIndexer;
            if (myIndexer < myContents.Count)
            {
                LoadContent(myContents, myIndexer, myShowPreviewImage, myShowTraceImage);
            }
            else
            {
                if (MessageBox.Show("You have completed this data collection study.", "Notification", MessageBoxButton.OK) == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        private void MyLoadButton_Click(object sender, RoutedEventArgs e)
        {
            // set up the open file dialog
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dialog.DefaultExt = "xml";
            dialog.Filter = "XML files|*.xml|All Files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.Multiselect = false;
            dialog.RestoreDirectory = true;
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;

            // show the open file dialog
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            // clear the strokes from the canvas
            ClearStrokes();

            // get the file path of the XML file
            string filePath = dialog.FileName;
            var processor = new SketchXmlProcessor();

            // load and set the strokes
            StrokeCollection strokes = processor.Read(filePath);
            foreach (var stroke in strokes)
            {
                SetStrokeProperties(stroke);
            }

            // set the recorded and canvas strokes
            myStrokes = strokes;
            MyCanvas.Strokes.Add(myStrokes);

            // enable interaction buttons
            EnableButtons(true);
        }

        private void MyClearButton_Click(object sender, RoutedEventArgs e)
        {
            // clear the strokes from the canvas
            ClearStrokes();

            // disable interaction buttons
            EnableButtons(false);
        }

        private void MyUndoButton_Click(object sender, RoutedEventArgs e)
        {
            // case: there are strokes on the canvas
            if (MyCanvas.Strokes.Count > 0)
            {
                // remove the most recent stroke added from the canvas
                MyCanvas.Strokes.RemoveAt(MyCanvas.Strokes.Count - 1);

                // remove the most recent stroke added from the list of strokes
                myStrokes.RemoveAt(myStrokes.Count - 1);
            }

            // case: there are now no strokes on the canvas
            if (MyCanvas.Strokes.Count == 0)
            {
                // disable interaction buttons
                EnableButtons(false);
            }
        }

        private void EnableButtons(bool enable)
        {
            MySubmitButton.IsEnabled = enable;
            MyClearButton.IsEnabled = enable;
            MyUndoButton.IsEnabled = enable;
        }

        #endregion

        #region Stroke Modifiers

        private void UpdateStroke(double x, double y, StylusPointCollection points, List<int> times, bool isFirstPoint)
        {
            // get the time
            long fullTime = DateTime.Now.Ticks;

            // record the subtracting constant (to account for overflow when later converting to int)
            if (isFirstPoint)
            {
                myTimeOffset = fullTime;
            }

            // subtract out the constant value
            fullTime = fullTime - myTimeOffset;

            // convert time to int
            int time = (int)fullTime;

            // add the point and time to their respective lists
            points.Add(new StylusPoint(x, y));
            times.Add(time);
        }

        private void ClearStrokes()
        {
            MyCanvas.Strokes.Clear();
            myStrokes = new StrokeCollection();
        }

        private void SetStrokeProperties(Stroke stroke)
        {
            stroke.DrawingAttributes.Width = MyCanvas.DefaultDrawingAttributes.Width;
            stroke.DrawingAttributes.Height = MyCanvas.DefaultDrawingAttributes.Height;
            stroke.DrawingAttributes.Color = MyCanvas.DefaultDrawingAttributes.Color;
        }

        #endregion

        #region Stylus and Mouse Flags

        private bool IsStylusMove { get; set; }
        private bool IsStylusEnd { get; set; }
        private bool IsMouseDown { get; set; }
        private bool IsMouseMove { get; set; }
        private bool IsMouseUp { get; set; }
        private bool IsMouseReady { get; set; }

        #endregion

        #region Helper Methods

        private string Prepend(string input, int digits, string character)
        {
            if (input.Length >= digits)
            {
                return input;
            }

            while (input.Length < digits)
            {
                input = character + input;
            }

            return input;
        }

        private double ScaleFactor(double controlWidth, double controlHeight, double imageWidth, double imageHeight)
        {
            double canvasRatio = controlHeight / controlWidth;
            double imageRatio = imageHeight / imageWidth;
            double scaleFactor = canvasRatio <= imageRatio
                ? controlHeight / imageHeight
                : controlWidth / imageWidth;

            return scaleFactor;
        }

        #endregion

        #region Fields

        private string myLoadDirectory;
        private bool myShowPreviewImage;
        private bool myShowTraceImage;

        private StylusPointCollection myPoints;
        private List<int> myTimes;
        private StrokeCollection myStrokes;
        private long myTimeOffset;

        private List<Tuple<string, string, string>> myContents;
        private int myIndexer;

        private Image myPreviewImage;

        #endregion
    }
}
