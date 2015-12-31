using Srl.Tools;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Shapes;

namespace FollowDemo
{
    public partial class MainWindow : Window
    {
        #region Mouse and Stylus Events

        private void MyCanvas_StylusButtonDown(object sender, StylusButtonEventArgs e)
        {
            #region Boilerplate Code

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

            #endregion

            // remove the animated stroke from the canvas if the last stroke was not a garbage stroke (e.g., accidental mini-stroke)
            if (!IgnoreRecentStroke)
            {
                foreach (Line animatedPoint in myAnimatedStroke)
                {
                    MyCanvas.Children.Remove(animatedPoint);
                }
            }
            else
            {
                IgnoreRecentStroke = false;
            }

            // remove the animated stroke from the canvas
            foreach (Line animatedPoint in myAnimatedStroke)
            {
                MyCanvas.Children.Remove(animatedPoint);
            }

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
            #region Boilerplate Code

            // update and check the stylus interaction flags
            if (PreviousStylusState == StylusState.StylusUp)
            {
                return;
            }
            PreviousStylusState = StylusState.StylusMove;

            #endregion

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
            #region Boilerplate Code

            // update and check the stylus interaction flags
            IsStylusMove = false;
            IsStylusEnd = true;
            if (PreviousStylusState == StylusState.StylusUp)
            {
                return;
            }
            PreviousStylusState = StylusState.StylusUp;

            #endregion

            //
            if (!CAN_INTERRUPT) { MyCanvas.IsEnabled = false; }

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

            // throw away garbage strokes (i.e., any unintentional mini-stroke with few points)
            if (stroke.StylusPoints.Count < MIN_STROKE_POINTS)
            {
                IgnoreRecentStroke = true;
                return;
            }

            // case #1: there are still strokes left to animate
            // update to the next animated stroke
            // case #2: there are no more strokes to animate
            // enable the animation complete flag
            ++myAnimationIndexer;
            if (myAnimationIndexer < myModelsDictionary[myLabels[myIndexer]].Count)
            {
                myAnimatedStroke = AnimateStroke(myModelsDictionary[myLabels[myIndexer]][myAnimationIndexer]);
            }
            else
            {
                IsAnimationDone = true;
            }
        }

        private void MyCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            #region Boilerplate Code

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

            #endregion

            // remove the animated stroke from the canvas if the last stroke was not a garbage stroke (e.g., accidental mini-stroke)
            if (!IgnoreRecentStroke)
            {
                foreach (Line animatedPoint in myAnimatedStroke)
                {
                    MyCanvas.Children.Remove(animatedPoint);
                }
            }
            else
            {
                IgnoreRecentStroke = false;
            }

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
            #region Boilerplate Code

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

            #endregion

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
            #region Boilerplate Code

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

            #endregion

            //
            if (!CAN_INTERRUPT) { MyCanvas.IsEnabled = false; }

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

            // throw away garbage strokes (i.e., any unintentional mini-stroke with few points)
            if (stroke.StylusPoints.Count < MIN_STROKE_POINTS)
            {
                IgnoreRecentStroke = true;
                return;
            }

            // case #1: there are still strokes left to animate
            // update to the next animated stroke
            // case #2: there are no more strokes to animate
            // enable the animation complete flag
            ++myAnimationIndexer;
            if (myAnimationIndexer < myModelsDictionary[myLabels[myIndexer]].Count)
            {
                myAnimatedStroke = AnimateStroke(myModelsDictionary[myLabels[myIndexer]][myAnimationIndexer]);
            }
            else
            {
                IsAnimationDone = true;
            }
        }

        private void MyCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // case: the user drew an unintentional garbage stroke (e.g., mini-stroke with few points)
            // ignore the last drawn stroke by removing it from the canvas and stroke list
            // and re-enabling the canvas for the user to try again
            if (IgnoreRecentStroke)
            {
                // re-enable the canvas
                if (!CAN_INTERRUPT) { MyCanvas.IsEnabled = true; }

                // manually remove the very last stroke added as soon as the mouse is up
                // note: UndoStrokes() not used here since MyCanvas does not detect most recent stroke added to myStrokes
                // therefore, the very last stroke in MyCanvas.Strokes instead of myStrokes is removed instead
                // for this scenario where mouse is up, this should be fine in theory
                MyCanvas.Strokes.RemoveAt(MyCanvas.Strokes.Count - 1);
                myStrokes.RemoveAt(myStrokes.Count - 1);

                return;
            }

            #region Boilerplate Code

            // remove the latest automatic stroke added to the canvas
            MyCanvas.Strokes.RemoveAt(MyCanvas.Strokes.Count - 1);

            // set the visual settings of the replacement manual stroke
            Stroke stroke = myStrokes[myStrokes.Count - 1];
            stroke.DrawingAttributes.Width = MyCanvas.DefaultDrawingAttributes.Width;
            stroke.DrawingAttributes.Height = MyCanvas.DefaultDrawingAttributes.Height;
            stroke.DrawingAttributes.Color = MyCanvas.DefaultDrawingAttributes.Color;

            // add the latest manual stroke to the canvas
            MyCanvas.Strokes.Add(stroke);

            #endregion

            // case: there are no more animated stroke to draw
            // once animations are done, then it is time for assessment
            if (IsAnimationDone)
            {
                // IMPORTANT! first assign a dummy label to mystrokes before proceeding
                // needed to do later processing
                myStrokes.AddPropertyData(SketchTools.LABEL_GUID, "");

                // reset the animation state flag
                IsAnimationDone = false;
                IsModelDisplayed = true;
                IsMappingDisplayed = true;

                // display the mapping strokes between the user and model strokes
                // also move the user strokes to the foreground by removing then re-adding them
                StrokeCollection userStrokes = myStrokes;
                StrokeCollection modelStrokes = myModelsDictionary[myLabels[myIndexer]];
                myMappingStrokes = CreateMapping(userStrokes, modelStrokes);
                MyCanvas.Strokes.Add(myMappingStrokes);
                MyCanvas.Strokes.Add(myModelsDictionary[myLabels[myIndexer]]);
                MyCanvas.Strokes.Remove(myStrokes);
                MyCanvas.Strokes.Add(myStrokes);

                // TODO: assess the user's strokes
                FollowAssessor assessor = new FollowAssessor(this); // debug version
                //FollowAssessor assessor = new FollowAssessor(MyCanvas.ActualWidth);
                assessor.Run(userStrokes, modelStrokes);

                FollowAssessor.ResultType lengthResult = assessor.LengthResult;
                string lengthResultOutput = "";
                switch (lengthResult)
                {
                    case FollowAssessor.ResultType.Low:
                        lengthResultOutput = "★☆☆";
                        break;
                    case FollowAssessor.ResultType.Med:
                        lengthResultOutput = "★★☆";
                        break;
                    default:
                        lengthResultOutput = "★★★";
                        break;
                }

                // TODO: RESTORE LATER
                lengthResultOutput = "Length Result:\n" + lengthResultOutput;
                //???
                MyOutputBlock.Text = lengthResultOutput;
            }
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
            if (MyCanvas.Strokes.Count > 0)
            {
                UndoStroke();
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
            foreach (Stroke stroke in myStrokes)
            {
                MyCanvas.Strokes.Remove(stroke);
            }
            myStrokes = new StrokeCollection();
        }

        private void UndoStroke()
        {
            // retrieve the last stroke added
            Stroke undoStroke = myStrokes[myStrokes.Count - 1];

            // remove the last stroke added to the canvas
            MyCanvas.Strokes.Remove(undoStroke);

            // remove the last stroke added from the list of strokes
            myStrokes.RemoveAt(myStrokes.Count - 1);
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

        private bool IsAnimationDone { get; set; }
        private bool IsModelDisplayed { get; set; }
        private bool IsMappingDisplayed { get; set; }
        private bool IgnoreRecentStroke { get; set; }

        #endregion

        #region Fields

        private StylusPointCollection myPoints;
        private List<int> myTimes;
        private StrokeCollection myStrokes;
        private long myTimeOffset;

        public static readonly int MIN_STROKE_POINTS = 10;
        public static readonly bool CAN_INTERRUPT = false;

        #endregion
    }
}
