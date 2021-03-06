﻿using Srl.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace FollowDemo
{
    public class FollowAssessor
    {
        #region Constructors

        public FollowAssessor(double size)
        {
            CanvasSize = size;
        }

        // debug
        public FollowAssessor(MainWindow window)
        {
            CanvasSize = window.MyCanvas.ActualWidth;
            myWindow = window;
        }

        #endregion

        public void Run(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // clone the original user and model strokes
            userStrokes = SketchTools.Clone(userStrokes);
            modelStrokes = SketchTools.Clone(modelStrokes);

            // length test
            LengthTest(userStrokes, modelStrokes);

            // precision (formerly, closeness) test
            PrecisionTest(userStrokes, modelStrokes);

            // accuracy (formerly, smoothness) test
            AccuracyTest(userStrokes, modelStrokes);

            // direction test
            DirectionTest(userStrokes, modelStrokes);

            // visual test
            VisualTest();

            // technique test
            TechniqueTest();
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

            // iterate through each user and model stroke
            for (int i = 0; i < userStrokes.Count; ++i)
            {
                // compute the metrics
                userStroke = userStrokes[i];
                modelStroke = modelStrokes[i];
                userStrokeLength = SketchTools.PathLength(userStroke);
                modelStrokeLength = SketchTools.PathLength(modelStroke);
                userModelRatio = Math.Abs(1.0 - userStrokeLength / modelStrokeLength);
                userCanvasRatio = userStrokeLength / CanvasSize;
                if (userCanvasRatio > 1.0) { userCanvasRatio = 1.0; }

                // calculate the score
                score = userModelRatio * userCanvasRatio;
                if (score > LENGTH_THRESHOLD_LOW)
                {
                    result = ResultType.Low;
                    ++numLow;
                }
                else if (LENGTH_THRESHOLD_LOW >= score && score > LENGTH_THRESHOLD_MED)
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
            }

            // get the list of results from all the symbol's strokes
            LengthResults = results.ToArray();
            if (numLow > 0)         { LengthResult = ResultType.Low; }
            else if (numMed > 0)    { LengthResult = ResultType.Med; }
            else                    { LengthResult = ResultType.High; }
        }

        private void PrecisionTest(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // initialize the variables
            Stroke userStroke, modelStroke;
            StylusPoint userPoint, modelPoint;
            List<double> distances = new List<double>();
            double distance;

            // iterate through each user and model stroke
            int count = 0;
            int numLow = 0;
            int numMed = 0;
            int numHigh = 0;
            for (int i = 0; i < userStrokes.Count; ++i)
            {
                // get the current user and model stroke
                userStroke = userStrokes[i];
                modelStroke = modelStrokes[i];

                // iterate through each point of the current user and model stroke
                for (int j = 0; j < userStroke.StylusPoints.Count; ++j)
                {
                    // get the current user and model point
                    userPoint = userStroke.StylusPoints[j];
                    modelPoint = modelStroke.StylusPoints[j];

                    // calculate the distance between the current user and model point
                    distance = SketchTools.Distance(userPoint, modelPoint) / CanvasSize;
                    distances.Add(distance);

                    // case: current distance exceeds closeness threshold
                    // record the current distance as a closeness error
                    if (distance > PRECISION_THRESHOLD_LOW)
                    {
                        ++numLow;
                    }
                    else if (PRECISION_THRESHOLD_LOW >= distance && distance >= PRECISION_THRESHOLD_MED)
                    {
                        ++numMed;
                    }
                    else
                    {
                        ++numHigh;
                    }
                    ++count;
                }
            }

            //
            if (numLow > 0) { PrecisionResult = ResultType.Low; }
            else if (numMed > 0) { PrecisionResult = ResultType.Med; }
            else { PrecisionResult = ResultType.High; }
        }

        private void AccuracyTest(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // create the variables
            StylusPointCollection userPoints, modelPoints;
            StylusPoint userPoint;
            List<double> distances;

            // iterate through the user and model strokes
            distances = new List<double>();
            for (int i = 0; i < userStrokes.Count; ++i)
            {
                // get the points from the current user and model stroke
                userPoints = userStrokes[i].StylusPoints;
                modelPoints = modelStrokes[i].StylusPoints;

                // iterate through each user points
                for (int j = 0; j < userPoints.Count; ++j)
                {
                    userPoint = userPoints[j];

                    // iterate through each model point
                    double min = Double.MaxValue;
                    foreach (StylusPoint modelPoint in modelPoints)
                    {
                        double distance = SketchTools.Distance(userPoint, modelPoint);
                        if (distance < min) { min = distance; }
                    }

                    distances.Add(min);
                }
            }

            //
            int count = 0;
            foreach (double distance in distances)
            {
                if (distance > ACCURACY_DISTANCE_MIN)
                {
                    ++count;
                }
            }

            //
            double ratio = (double)count / (double)distances.Count;

            //
            if (ratio > ACCURACY_THRESHOLD_LOW) { AccuracyResult = ResultType.Low; }
            else if (ratio > ACCURACY_THRESHOLD_MED) { AccuracyResult = ResultType.Med; }
            else { AccuracyResult = ResultType.High; }
        }

        private void DirectionTest(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // note: this method works as is due to how the strokes are synced
            // without this syncing, the code for the direction test would be much less trivial

            // initialize the variables
            Stroke userStroke;
            int[] userTimes;
            bool isCorrect;
            int numIncorrect = 0;
            int numStrokes = userStrokes.Count;

            //
            for (int i = 0; i < userStrokes.Count; ++i)
            {
                // get the user stroke and its times
                userStroke = userStrokes[i];
                userTimes = (int[])userStroke.GetPropertyData(SketchTools.TIMES_GUID);

                //
                isCorrect = userTimes[0] == 0;

                //
                if (!isCorrect) { ++numIncorrect; }
            }

            //
            if (numIncorrect == numStrokes) { DirectionResult = ResultType.Low; }
            else if (numIncorrect > 0) { DirectionResult = ResultType.Med; }
            else { DirectionResult = ResultType.High; }
        }

        private void VisualTest()
        {
            int length = (int)LengthResult;
            int precision = (int)PrecisionResult;
            int accuracy = (int)AccuracyResult;
            double visual = (double)(length + precision + accuracy) / 3.0;

            //
            if (visual < 0.5)       { VisualResult = ResultType.Low; }
            else if (visual < 1.5)  { VisualResult = ResultType.Med; }
            else                    { VisualResult = ResultType.High; }
        }

        private void TechniqueTest()
        {
            int direction = (int)DirectionResult;
            double technique = (double)(direction) / 1.0;

            //
            if (technique < 0.5)        { TechniqueResult = ResultType.Low; }
            else if (technique < 1.5)   { TechniqueResult = ResultType.Med; }
            else                        { TechniqueResult = ResultType.High; }
        }

        #endregion

        #region Properties and Enums

        public double CanvasSize { get; set; } // debug

        public ResultType LengthResult { get; private set; }
        public ResultType PrecisionResult { get; private set; }
        public ResultType AccuracyResult { get; private set; }
        public ResultType DirectionResult { get; private set; }
        public ResultType VisualResult { get; private set; }
        public ResultType TechniqueResult { get; private set; }

        public ResultType[] LengthResults { get; private set; }
        public ResultType[] PrecisionResults { get; private set; }
        public ResultType[] DirectionResults { get; private set; }
        public double[] ProportionDebug { get; private set; }

        public enum ResultType:int { Low = 0, Med = 1, High = 2 }

        #endregion

        #region Fields

        private MainWindow myWindow; // debug

        public static readonly double LENGTH_THRESHOLD_LOW = 0.10; // 10%
        public static readonly double LENGTH_THRESHOLD_MED = 0.05; //  5%

        public static readonly double PRECISION_THRESHOLD_LOW = 0.07; // 6.0% of canvas width
        public static readonly double PRECISION_THRESHOLD_MED = 0.05; // 3.0% of canvas width
        //public static readonly double PRECISION_THRESHOLD_LOW = 40.0; // 6.0% of canvas width
        //public static readonly double PRECISION_THRESHOLD_MED = 25.0; // 3.0% of canvas width
        //public static readonly double PRECISION_THRESHOLD_LOW = 30.0; // 6.0% of canvas width
        //public static readonly double PRECISION_THRESHOLD_MED = 20.0; // 3.0% of canvas width

        public static readonly double ACCURACY_THRESHOLD_LOW = 0.10; // 10%
        public static readonly double ACCURACY_THRESHOLD_MED = 0.05; //  4%

        private static readonly double ACCURACY_DISTANCE_MIN = 15.0;

        #endregion
    }
}
