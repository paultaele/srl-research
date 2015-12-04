using Srl.Tools;
using Srl.Xml;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace NormalizeInterface
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
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

            // enable interaction buttons
            EnableDrawButtons(true);
            EnableModeButtons(true);
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

            // enable interaction buttons
            EnableDrawButtons(true);
            EnableModeButtons(true);
        }

        #endregion

        #region Mode Switching Interactions

        private void MyDrawButton_Click(object sender, RoutedEventArgs e)
        {
            // disable editing buttons and enable normalization buttons
            EnableDrawButtons(true);
            EnableNormalizeButtons(false);
            MyCanvas.IsEnabled = true;
            
            // restore the original strokes
            ClearStrokes();
            myStrokes = myOriginalStrokes;
            SetStrokeCollectionProperties(myStrokes);
            MyCanvas.Strokes.Add(myStrokes);
        }

        private void MyTransformButton_Click(object sender, RoutedEventArgs e)
        {
            // enable editing buttons and disable normalization buttons
            EnableDrawButtons(false);
            EnableNormalizeButtons(true);
            MyCanvas.IsEnabled = false;

            // clone the strokes
            myStrokes.AddPropertyData(SketchTools.LABEL_GUID, "None");
            myOriginalStrokes = Clone(myStrokes);
            myDisplayStrokes = Clone(myOriginalStrokes);

            // reset the strokes
            ClearStrokes();
            SetStrokeCollectionProperties(myDisplayStrokes);
            MyCanvas.Strokes.Add(myDisplayStrokes);
        }

        #endregion

        #region Normalization Button Interactions

        private void MyResampleButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyTranslateButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyScaleButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyDisplayPointsButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void Transform()
        {
            // clear and reset the strokes
            MyCanvas.Strokes.Clear();
            myDisplayStrokes = Clone(myOriginalStrokes);

            // case: resample points
            if (MyResampleButton.IsChecked.Value)
            {
                int n = 32;

                myDisplayStrokes = SketchTools.Resample(myDisplayStrokes, n);
            }

            // case: scale points
            if (MyScaleButton.IsChecked.Value)
            {
                double size = 300.0;

                myDisplayStrokes = SketchTools.Scale(myDisplayStrokes, size);
            }

            // case: translate points
            if (MyTranslateButton.IsChecked.Value)
            {
                StylusPoint k = new StylusPoint(MyCanvas.ActualWidth / 2.0, MyCanvas.ActualHeight / 2.0);

                myDisplayStrokes = SketchTools.Translate(myDisplayStrokes, k);
            }

            // case: display points
            if (MyDisplayPointsButton.IsChecked.Value)
            {
                DisplayPoints(myDisplayStrokes);
            }

            // display the strokes on canvas
            SetStrokeCollectionProperties(myDisplayStrokes);
            MyCanvas.Strokes.Add(myDisplayStrokes);
        }

        private void DisplayPoints(StrokeCollection strokes)
        {
            var displayPoints = new List<Stroke>();

            foreach (Stroke stroke in strokes)
            {
                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    Stroke displayPoint
                        = new Stroke(new StylusPointCollection() { new StylusPoint(point.X, point.Y) });
                    displayPoint.DrawingAttributes.Color = Colors.Red;
                    displayPoint.DrawingAttributes.Width = 15;
                    displayPoint.DrawingAttributes.Height = 15;
                    displayPoints.Add(displayPoint);
                }
            }

            foreach (Stroke displayPoint in displayPoints)
            {
                MyCanvas.Strokes.Add(displayPoint);
            }
        }

        #endregion

        #region Drawing Button Interactions

        private void MyClearButton_Click(object sender, RoutedEventArgs e)
        {
            // clear the strokes from the canvas
            ClearStrokes();

            // disable interaction buttons
            EnableDrawButtons(false);
            EnableModeButtons(false);
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
                EnableDrawButtons(false);
                EnableModeButtons(false);
            }
        }

        #endregion

        #region Enable Button Interactions

        private void EnableDrawButtons(bool enable)
        {
            MyClearButton.IsEnabled = enable;
            MyUndoButton.IsEnabled = enable;
        }

        private void EnableNormalizeButtons(bool enable)
        {
            MyResampleButton.IsEnabled = enable;
            MyScaleButton.IsEnabled = enable;
            MyTranslateButton.IsEnabled = enable;
            MyDisplayPointsButton.IsEnabled = enable;

            if (!enable)
            {
                MyResampleButton.IsChecked = false;
                MyScaleButton.IsChecked = false;
                MyTranslateButton.IsChecked = false;
                MyDisplayPointsButton.IsChecked = false;
            }
        }

        private void EnableModeButtons(bool enable)
        {
            MyDrawButton.IsEnabled = enable;
            MyTransformButton.IsEnabled = enable;
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

        private void SetStrokeCollectionProperties(StrokeCollection strokes)
        {
            foreach (Stroke stroke in strokes)
            {
                SetStrokeProperties(stroke);
            }
        }

        private void SetStrokeProperties(Stroke stroke)
        {
            stroke.DrawingAttributes.Width = MyCanvas.DefaultDrawingAttributes.Width;
            stroke.DrawingAttributes.Height = MyCanvas.DefaultDrawingAttributes.Height;
            stroke.DrawingAttributes.Color = MyCanvas.DefaultDrawingAttributes.Color;
        }

        private StrokeCollection Clone(StrokeCollection others)
        {
            // get GUIDs
            Guid labelGuid = SketchTools.LABEL_GUID;
            Guid timesGuid = SketchTools.TIMES_GUID;

            // initialize clone list
            StrokeCollection strokes = new StrokeCollection();
            strokes.AddPropertyData(labelGuid, others.GetPropertyData(labelGuid));

            // iterate through each original strokes
            foreach (Stroke other in others)
            {
                // clone the points
                var points = new StylusPointCollection();
                foreach (StylusPoint point in other.StylusPoints)
                {
                    points.Add(new StylusPoint(point.X, point.Y));
                }

                // clone the times
                var times = new List<int>();
                int[] otherTimes = (int[])other.GetPropertyData(timesGuid);
                foreach (int time in otherTimes)
                {
                    times.Add(time);
                }

                // clone the stroke
                Stroke stroke = new Stroke(points);
                stroke.AddPropertyData(timesGuid, times.ToArray());
                strokes.Add(stroke);
            }

            return strokes;
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

        #region Fields

        private StylusPointCollection myPoints;
        private List<int> myTimes;
        private StrokeCollection myStrokes;
        private long myTimeOffset;

        private StrokeCollection myOriginalStrokes;
        private StrokeCollection myDisplayStrokes;


        #endregion
    }
}
