﻿using Srl.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Ink;
using System.Windows.Input;

namespace FollowDemo
{
    public class Assessor
    {
        public Assessor(double size)
        {
            CanvasSize = size;
        }

        // debug
        public Assessor(MainWindow window)
        {
            CanvasSize = window.MyCanvas.ActualWidth;
            myWindow = window;
        }

        public void Run(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // clone the original user and model strokes
            userStrokes = SketchTools.Clone(userStrokes);
            modelStrokes = SketchTools.Clone(modelStrokes);

            // sync the model storkes
            // means resampling the model strokes to the user strokes
            // AND matching the best pairwise direction between the user and model strokes
            SyncStroke(userStrokes, modelStrokes);

            // length test?
            LengthTest(userStrokes, modelStrokes);
        }

        #region Tests

        private void LengthTest(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // initialize the variables
            Stroke userStroke, modelStroke;
            double userStrokeLength, modelStrokeLength, userModelRatio, userCanvasRatio, score;
            int numLow = 0;
            int numMed = 0;
            int numHigh = 0;
            ResultType result;
            List<ResultType> results = new List<ResultType>();
            List<double> debug = new List<double>();

            // iterate through each user and modelstroke
            //double high = 0.0; // debug
            //int highIndex = -1; // debug
            for (int i = 0; i < userStrokes.Count; ++i)
            {
                // compute the metrics
                userStroke = userStrokes[i];
                modelStroke = modelStrokes[i];
                userStrokeLength = SketchTools.PathLength(userStroke);
                modelStrokeLength = SketchTools.PathLength(modelStroke);
                userModelRatio = Math.Abs(1.0 - userStrokeLength / modelStrokeLength);
                if (userModelRatio > 1.0) { userModelRatio = 1.0; }
                userCanvasRatio = userStrokeLength / CanvasSize;

                // calculate the score
                score = userModelRatio * userCanvasRatio;
                debug.Add(score);
                if (score > THRESHOLD_LOW)
                {
                    result = ResultType.Low;
                    ++numLow;
                }
                else if (THRESHOLD_LOW >= score && score > THRESHOLD_MED)
                {
                    result = ResultType.Med;
                    ++numMed;
                }
                else
                {
                    result = ResultType.High;
                    ++numHigh;
                }
                results.Add(result);

                //// debug
                //Console.WriteLine(score);

                //// debugging
                //if (score > high)
                //{
                //    high = score;
                //    highIndex = i;
                //}
            }

            // get the list of results from all the symbol's strokes
            LengthResults = results.ToArray();
            if (numLow > 0)
            {
                LengthResult = ResultType.Low;
            }
            else if (numMed > 0)
            {
                LengthResult = ResultType.Med;
            }
            else
            {
                LengthResult = ResultType.High;
            }
            LengthDebug = debug.ToArray();

            //// debug
            //Console.WriteLine();

            // NOTES:
            // 3 STARS = 5% or below?
            // 2 STARS = 10% or below?
            // 1 STAR  = above 10%?

            // debugging
            //string result = "";
            //if (numLow > 0) { result = "LOW"; }
            //else if (numMed > 0) { result = "MED"; }
            //else { result = "HIGH"; }
            //result += "\n [" + highIndex + "] " + Math.Round(high, 2);
            //myWindow.MyOutputBlock.Text = result;
        }

        #endregion

        #region Helper Methods

        private StrokeCollection SyncStroke(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // initialize the list of synched strokes
            StrokeCollection syncedStrokes = new StrokeCollection();

            // iterate through each user and model strokes
            int numPoints;
            Stroke userStroke, modelStroke, reverseStroke;
            StylusPoint lastPoint;
            int lastTime;
            double directDistance, reverseDistance;
            Stroke synchedStroke;
            for (int i = 0; i < userStrokes.Count; ++i)
            {
                // retrieve the current user and model strokes
                // also retrieve the last point of the pre-resampled last model point
                // due to resampling algorithm throwing away the last point
                // (without this, the resampled model stroke will have one less point than the user stroke)
                userStroke = userStrokes[i];
                modelStroke = SketchTools.Clone(modelStrokes[i]);

                // get the number of user stroke points
                numPoints = userStroke.StylusPoints.Count;

                // resample the model stroke to match the number of user stroke points
                // this code fragment also adds the last point and time to the stroke
                lastPoint = modelStroke.StylusPoints[modelStroke.StylusPoints.Count - 1];
                lastTime = ((int[])modelStroke.GetPropertyData(SketchTools.TIMES_GUID))[modelStroke.StylusPoints.Count - 1];
                StrokeCollection tempStrokes = new StrokeCollection() { modelStroke };
                tempStrokes.AddPropertyData(SketchTools.LABEL_GUID, "");
                modelStroke = SketchTools.Resample(tempStrokes, numPoints)[0];
                modelStroke.StylusPoints.Add(lastPoint);
                List<int> tempTimes = new List<int>() { lastTime };
                tempTimes.AddRange((int[])modelStroke.GetPropertyData(SketchTools.TIMES_GUID));
                modelStroke.AddPropertyData(SketchTools.TIMES_GUID, tempTimes.ToArray());

                // create the reverse stroke and calculate the direct and reverse pairwise stroke distances
                reverseStroke = SketchTools.Reverse(modelStroke);
                directDistance = SketchTools.Distance(userStroke, modelStroke);
                reverseDistance = SketchTools.Distance(userStroke, reverseStroke);

                // reserve the stroke if the reverse pairwise distance is shorter
                synchedStroke = reverseDistance < directDistance ? reverseStroke : modelStroke;

                // add the synced stroke
                syncedStrokes.Add(synchedStroke);
            }

            return syncedStrokes;
        }

        #endregion

        #region Properties and Enums

        public double CanvasSize { get; set; } // debug

        public ResultType LengthResult { get; private set; }
        public ResultType ClosenessResult { get; private set; }
        public ResultType DirectionResult { get; private set; }

        public ResultType[] LengthResults { get; private set; }
        public ResultType[] ClosenessResults { get; private set; }
        public ResultType[] DirectionResults { get; private set; }
        public double[] LengthDebug { get; private set; }

        public enum ResultType { Low, Med, High }

        #endregion

        #region Fields

        private MainWindow myWindow; // debug

        public static readonly double THRESHOLD_LOW = 0.10;
        public static readonly double THRESHOLD_MED = 0.05;
        public static readonly string DEBUG_FILE = @"C:\Users\paultaele\Desktop\debug.txt";

        #endregion
    }
}
