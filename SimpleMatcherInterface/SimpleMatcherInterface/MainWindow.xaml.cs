using Ookii.Dialogs.Wpf;
using Srl.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace SimpleMatcherInterface
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
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            ResizeMode = ResizeMode.NoResize;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // boxify the canvas drawing space
            double length = MyCanvasBorder.ActualHeight > MyCanvasBorder.ActualWidth
                ? MyCanvasBorder.ActualWidth : MyCanvasBorder.ActualHeight;
            MyCanvasBorder.Width = length;
            MyCanvasBorder.Height = length;

            // set up the classifier
            myResampleSize = 128;
            myScaleBounds = 500.0;
            myOrigin = new StylusPoint(length / 2.0, length / 2.0);
            myScaleType = SketchTools.ScaleType.Hybrid;
            myTranslateType = SketchTools.TranslateType.Centroid;
            myMatcher = new GreedyMatcher(myResampleSize, myScaleBounds, myOrigin, myScaleType, myTranslateType);

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

        #region Menu Interactions

        private void MyLoadMenu_Click(object sender, RoutedEventArgs e)
        {
            // display the folder browser dialog
            var dialog = new VistaFolderBrowserDialog();
            if (!dialog.ShowDialog().Value)
            {
                return;
            }

            //
            bool hasData = false;
            foreach (var file in Directory.GetFiles(dialog.SelectedPath))
            {
                if (file.EndsWith(".xml"))
                {
                    hasData = true;
                }
            }
            if (!hasData)
            {
                MessageBox.Show("You must select a directory that contains data files.");

                return;
            }

            // train classifier
            myMatcher.Train(dialog.SelectedPath);

            // enable canvas
            MyCanvas.IsEnabled = true;
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

        #region Interface Interactions

        private void MySubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // apply label to input strokes
            myStrokes.AddPropertyData(SketchTools.LABEL_GUID, "");

            // run the classifier on the input strokes
            myMatcher.Run(myStrokes);

            // retrieve the list of results and labels
            List<StrokeCollection> results = myMatcher.Results();
            List<string> labels = myMatcher.Labels();

            //
            DisplayResults(results, myMatcher);


            // clear the strokes
            ClearStrokes();
        }

        private void DisplayResults(List<StrokeCollection> results, GreedyMatcher matcher)
        {
            int indexLength = 5;
            int labelLength = 5;

            //
            string output = "";
            StrokeCollection topResult;
            StringBuilder topIndex, topLabel, topScore;
            for (int i = 0; i < 20; ++i)
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

        #endregion

        #region Helper Methods

        #endregion

        #region Stylus and Mouse Flags

        private bool IsStylusMove { get; set; }
        private bool IsStylusEnd { get; set; }
        private bool IsMouseDown { get; set; }
        private bool IsMouseMove { get; set; }
        private bool IsMouseUp { get; set; }
        private bool IsMouseReady { get; set; }

        #endregion

        #region Fields

        private StylusPointCollection myPoints;
        private List<int> myTimes;
        private StrokeCollection myStrokes;
        private long myTimeOffset;

        private int myResampleSize;
        private double myScaleBounds;
        private StylusPoint myOrigin;
        private SketchTools.ScaleType myScaleType;
        private SketchTools.TranslateType myTranslateType;
        
        private GreedyMatcher myMatcher;

        #endregion
    }
}
