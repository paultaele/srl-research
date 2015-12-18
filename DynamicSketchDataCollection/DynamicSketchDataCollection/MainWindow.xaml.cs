using Ookii.Dialogs.Wpf;
using Srl.Tools;
using Srl.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DynamicSketchDataCollection
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

            // set up the window size
            WindowState = WindowState.Maximized;
            //WindowStyle = WindowStyle.ThreeDBorderWindow;
            //ResizeMode = ResizeMode.NoResize;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // boxify the canvas drawing space
            double length = MyCanvasBorder.ActualHeight > MyCanvasBorder.ActualWidth
                ? MyCanvasBorder.ActualWidth : MyCanvasBorder.ActualHeight;
            MyCanvasBorder.Width = MyCanvasBorder.Height = length;
            MyButtonsBorder.Width = MyLabelImageBorder.Width = length;

            // initialize an empty list of strokes
            myStrokes = new StrokeCollection();

            //
            myMatcher = new GreedyMatcher(RESAMPLE_SIZE, SCALE_BOUNDS, ORIGIN, SCALE_TYPE, TRANSLATE_TYPE);

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
        }

        #endregion

        #region Menu Bar Interactions

        private void MyLoadItem_Click(object sender, RoutedEventArgs e)
        {
            MyLoadPanel.Visibility = Visibility.Visible;
        }

        private void MyExitItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        #endregion

        #region Load Panel Interactions

        private void MyLoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            // display the folder browser dialog
            var dialog = new VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            if (!dialog.ShowDialog().Value)
            {
                return;
            }

            //
            MyLoadDataBox.Text = dialog.SelectedPath;
            myLoadPath = dialog.SelectedPath;
        }

        private void MyLoadImagesButton_Click(object sender, RoutedEventArgs e)
        {
            // display the folder browser dialog
            var dialog = new VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            if (!dialog.ShowDialog().Value)
            {
                return;
            }

            //
            MyLoadImagesBox.Text = dialog.SelectedPath;
        }

        private void MyLoadOkButton_Click(object sender, RoutedEventArgs e)
        {
            // reset
            myIndexer = 0;
            MyBackButton.IsEnabled = false;
            MyNextButton.IsEnabled = false;
            MyCanvas.Children.Clear();

            // check directories
            string dataDirPath = MyLoadDataBox.Text;
            string imagesDirPath = MyLoadImagesBox.Text;

            // check if the text fields contain any text
            if (dataDirPath.Equals(""))
            {
                MessageBox.Show("The load data directory is blank.");
                return;
            }
            if (imagesDirPath.Equals(""))
            {
                MessageBox.Show("The load images directory is blank.");
                return;
            }

            // check if there are data files
            int numDataFiles = 0;
            foreach (var dataFilePath in Directory.GetFiles(dataDirPath))
            {
                if (dataFilePath.EndsWith(".xml"))
                {
                    ++numDataFiles;
                }
            }
            if (numDataFiles == 0)
            {
                MessageBox.Show("The data directory contains no data files.");
                return;
            }

            // check if there are image files
            List<string> labels = new List<string>();
            Dictionary<string, Image> imageDictionary = new Dictionary<string, Image>();
            string label;
            Image image;
            foreach (var imageFilePath in Directory.GetFiles(imagesDirPath))
            {
                if (imageFilePath.EndsWith(".png"))
                {
                    // get the label
                    label = Path.GetFileNameWithoutExtension(imageFilePath);
                    image = CreateImage(imageFilePath);

                    labels.Add(label);
                    imageDictionary.Add(label, image);
                }
            }
            if (labels.Count == 0)
            {
                MessageBox.Show("The images directory contains no image files.");
                return;
            }

            //
            myMatcher.Train(dataDirPath);
            myLabels = labels;
            myImageDictionary = imageDictionary;
            myIndexer = 0;

            //
            MyLabelBlock.Text = myLabels[0];
            if (myLabels.Count > 0)
            {
                MyNextButton.IsEnabled = true;
            }

            //
            MyCanvas.IsEnabled = true;
            MyDisplayImageBox.IsEnabled = true;

            MyLoadPanel.Visibility = Visibility.Collapsed;
        }

        private void MyLoadCancelButton_Click(object sender, RoutedEventArgs e)
        {
            MyLoadDataBox.Text = "";
            MyLoadImagesBox.Text = "";

            MyLoadPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Interface Interactions

        private void MyCheckButton_Click(object sender, RoutedEventArgs e)
        {
            //
            myStrokes.AddPropertyData(SketchTools.LABEL_GUID, "");

            //
            myMatcher.Run(myStrokes);

            //
            DisplayResults(myMatcher.Results(), myMatcher);
        }

        private void DisplayResults(List<StrokeCollection> results, GreedyMatcher matcher)
        {
            int indexLength = 5;
            int labelLength = 5;

            //
            string output = "";
            StrokeCollection topResult;
            StringBuilder topIndex, topLabel, topScore;
            for (int i = 0; i < results.Count / 2; ++i)
            {
                topResult = results[i];

                topIndex = new StringBuilder("" + (i + 1) + ". ");
                topLabel = new StringBuilder((string)topResult.GetPropertyData(SketchTools.LABEL_GUID));
                topScore = new StringBuilder("" + Math.Round(matcher.Score(topResult), 2));

                topIndex.Append(' ', indexLength - topIndex.Length);
                topLabel.Append(' ', labelLength - topLabel.Length);
                output += topIndex.ToString() + topLabel.ToString() + topScore.ToString() + "\n";
            }

            //
            MyOutputBlock.Text = output;
        }

        private void MySaveButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Really save this sketch?", "Save", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            //
            string dirPath = MyLoadDataBox.Text + Path.DirectorySeparatorChar.ToString();
            string label = MyLabelBlock.Text;

            //
            int count = 0;
            string fileName = "";
            string saveName = "";
            foreach (string filePath in Directory.GetFiles(dirPath))
            {
                fileName = Path.GetFileName(filePath);

                if (fileName.StartsWith(label) && fileName.EndsWith(".xml"))
                {
                    ++count;
                    saveName = Path.GetFileNameWithoutExtension(filePath);
                    saveName = dirPath + saveName.Substring(0, saveName.Length - 3);
                }
            }

            //
            string countString = count.ToString();
            string savePath = saveName + countString.PadLeft(3, '0') + ".xml";

            //
            foreach(Stroke stroke in myStrokes)
            {
                StylusPointCollection points = stroke.StylusPoints;
                int[] times = (int[])stroke.GetPropertyData(SketchTools.TIMES_GUID);
            }

            SketchXmlProcessor processor = new SketchXmlProcessor();
            processor.Write(savePath, label, myStrokes);

            //
            myMatcher.Train(myLoadPath);
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

        private void MyBackButton_Click(object sender, RoutedEventArgs e)
        {
            //
            ClearStrokes();
            EnableButtons(false);
            MyOutputBlock.Text = "";

            // get the previous label
            string oldLabel = MyLabelBlock.Text;

            // increment the indexer
            --myIndexer;

            // update label block
            string newLabel = myLabels[myIndexer];
            MyLabelBlock.Text = newLabel;

            // check if current index is within range
            if (myIndexer == 0)
            {
                MyBackButton.IsEnabled = false;
            }

            // update image, if checked
            if (MyDisplayImageBox.IsChecked.Value)
            {
                MyCanvas.Children.Remove(myImageDictionary[oldLabel]);
                MyCanvas.Children.Add(myImageDictionary[newLabel]);
            }

            //
            MyNextButton.IsEnabled = true;
        }

        private void MyNextButton_Click(object sender, RoutedEventArgs e)
        {
            //
            ClearStrokes();
            EnableButtons(false);
            MyOutputBlock.Text = "";

            // get the previous label
            string oldLabel = MyLabelBlock.Text;

            // increment the indexer
            ++myIndexer;

            // update label block
            string newLabel = myLabels[myIndexer];
            MyLabelBlock.Text = newLabel;

            // check if current index is within range
            if (myIndexer == myLabels.Count - 1)
            {
                MyNextButton.IsEnabled = false;
            }

            // update image, if checked
            if (MyDisplayImageBox.IsChecked.Value)
            {
                MyCanvas.Children.Remove(myImageDictionary[oldLabel]);
                MyCanvas.Children.Add(myImageDictionary[newLabel]);
            }

            //
            MyBackButton.IsEnabled = true;
        }

        private void MyDisplayImageBox_Click(object sender, RoutedEventArgs e)
        {
            bool status = MyDisplayImageBox.IsChecked.Value;
            if (status)
            {
                MyCanvas.Children.Add(myImageDictionary[MyLabelBlock.Text]);
            }
            else
            {
                MyCanvas.Children.Remove(myImageDictionary[MyLabelBlock.Text]);
            }
        }

        private void EnableButtons(bool enable)
        {
            MySaveButton.IsEnabled = enable;
            MyCheckButton.IsEnabled = enable;
            MyClearButton.IsEnabled = enable;
            MyUndoButton.IsEnabled = enable;
        }

        #endregion

        #region Mouse and Stylus Events

        private void MyCanvas_StylusButtonDown(object sender, StylusButtonEventArgs e)
        {
            // update and check the stylus interaction flags
            IsStylusMove = true;
            if (PreviousStylusState == StylusState.StylusMove || PreviousStylusState == StylusState.StylusDown)
            {
                return;
            }
            PreviousStylusState = StylusState.StylusDown;

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
            // update and check the stylus interaction flags
            if (PreviousStylusState == StylusState.StylusUp)
            {
                return;
            }
            PreviousStylusState = StylusState.StylusMove;

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
            // update and check the stylus interaction flags
            IsStylusMove = false;
            IsStylusEnd = true;
            if (PreviousStylusState == StylusState.StylusUp)
            {
                return;
            }
            PreviousStylusState = StylusState.StylusUp;

            // update the stroke with the final x, y, and time
            UpdateStroke(
                e.GetPosition(MyCanvas).X,
                e.GetPosition(MyCanvas).Y,
                myPoints,
                myTimes,
                false);

            // set the stroke and times
            Stroke stroke = new Stroke(myPoints);
            int[] times = myTimes.ToArray();

            // add the times and stroke
            stroke.AddPropertyData(SketchTools.TIMES_GUID, times);
            myStrokes.Add(stroke);
        }

        private void MyCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // update and check the preview mouse interaction flags
            IsMouseDown = !IsMouseDown;
            IsMouseMove = false;
            if (!IsMouseDown) { return; }
            if (IsStylusMove) { return; }
            if (PreviousMouseState == MouseState.MouseMove || PreviousMouseState == MouseState.MouseDown)
            {
                return;
            }
            PreviousMouseState = MouseState.MouseDown;

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
            // update and check the preview mouse interaction flags
            if (myPoints == null) { return; }
            if (e.LeftButton != MouseButtonState.Pressed) { return; }
            if (IsMouseDown) { return; }
            if (!IsMouseMove) { IsMouseMove = true; return; }
            if (!IsMouseReady) { IsMouseReady = true; return; }
            if (IsStylusMove) { return; }
            if (PreviousMouseState == MouseState.MouseUp)
            {
                return;
            }
            PreviousMouseState = MouseState.MouseMove;

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
            // update and check the preview mouse interaction flags
            if (myPoints == null) { return; }
            IsMouseUp = !IsMouseUp;
            if (IsMouseUp) { return; }
            if (IsStylusMove) { return; }
            if (IsStylusEnd) { IsStylusEnd = false; return; }
            if (PreviousMouseState == MouseState.MouseUp)
            {
                return;
            }
            PreviousMouseState = MouseState.MouseUp;

            // update the stroke with the final x, y, and time
            UpdateStroke(
                e.GetPosition(MyCanvas).X,
                e.GetPosition(MyCanvas).Y,
                myPoints,
                myTimes,
                false);

            // set the stroke and times
            Stroke stroke = new Stroke(myPoints);
            int[] times = myTimes.ToArray();

            // add the times and stroke
            stroke.AddPropertyData(SketchTools.TIMES_GUID, times);
            myStrokes.Add(stroke);
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
            StylusPoint point = new StylusPoint(x, y);

            // add the point and time to their respective lists
            points.Add(point);
            times.Add(time);
        }

        private void ClearStrokes()
        {
            MyCanvas.Strokes.Clear();
            myStrokes = new StrokeCollection();
        }

        #endregion

        #region Helper Methods

        private double ScaleFactor(double controlWidth, double controlHeight, double imageWidth, double imageHeight)
        {
            double canvasRatio = controlHeight / controlWidth;
            double imageRatio = imageHeight / imageWidth;
            double scaleFactor = canvasRatio <= imageRatio
                ? controlHeight / imageHeight
                : controlWidth / imageWidth;

            return scaleFactor;
        }

        private Image CreateImage(string filePath)
        {
            Image image = new Image();
            image.Source = new BitmapImage(new Uri(filePath));
            double imageWidth = image.Source.Width;
            double imageHeight = image.Source.Height;

            double canvasWidth = MyCanvasBorder.Width;
            double canvasHeight = MyCanvasBorder.Height;
            double scaleFactor = ScaleFactor(canvasWidth, canvasHeight, imageWidth, imageHeight);

            image.Width = imageWidth * scaleFactor;
            image.Height = imageHeight * scaleFactor;
            InkCanvas.SetLeft(image, (canvasWidth / 2.0) - (image.Width / 2.0));
            InkCanvas.SetTop(image, (canvasHeight / 2.0) - (image.Height / 2.0));

            return image;
        }

        private void SetStrokeProperties(Stroke stroke, double length, Color color)
        {
            stroke.DrawingAttributes.Width = MyCanvas.DefaultDrawingAttributes.Width;
            stroke.DrawingAttributes.Height = MyCanvas.DefaultDrawingAttributes.Height;
            stroke.DrawingAttributes.Color = MyCanvas.DefaultDrawingAttributes.Color;
        }

        #endregion

        #region Stylus and Mouse Flags and Enums

        private bool IsStylusMove { get; set; }
        private bool IsStylusEnd { get; set; }
        private bool IsMouseDown { get; set; }
        private bool IsMouseMove { get; set; }
        private bool IsMouseUp { get; set; }
        private bool IsMouseReady { get; set; }
        private StylusState PreviousStylusState { get; set; }
        private MouseState PreviousMouseState { get; set; }
        private enum StylusState { StylusDown, StylusMove, StylusUp }
        private enum MouseState { MouseDown, MouseMove, MouseUp }

        #endregion

        #region Fields

        private StylusPointCollection myPoints;
        private List<int> myTimes;
        private StrokeCollection myStrokes;
        private long myTimeOffset;

        private List<string> myLabels;
        private Dictionary<string, Image> myImageDictionary;
        private int myIndexer;

        private GreedyMatcher myMatcher;

        private string myLoadPath;

        public static readonly int RESAMPLE_SIZE = 128;
        public static readonly double SCALE_BOUNDS = 500;
        public static readonly StylusPoint ORIGIN = new StylusPoint(0.0, 0.0);
        public static readonly SketchTools.ScaleType SCALE_TYPE = SketchTools.ScaleType.Hybrid;
        public static readonly SketchTools.TranslateType TRANSLATE_TYPE = SketchTools.TranslateType.Centroid;
        public static readonly bool WEIGHTED = true;

        public static readonly double BRUSH_LENGTH = 10.0;
        public static readonly Color BRUSH_IMAGE_OFF_COLOR = Colors.Black;
        public static readonly Color BRUSH_IMAGE_ON_COLOR = Colors.Red;

        #endregion
    }
}
