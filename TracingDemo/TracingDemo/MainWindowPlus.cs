using Srl.Tools;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;

namespace TracingDemo
{
    public partial class MainWindow : Window
    {
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

        #region Button Interactions

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

        #endregion

        #region Stroke and Button Modifiers

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

        #endregion
    }
}
