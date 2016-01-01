using Srl.Tools;
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

        public void Run(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // clone the original user and model strokes
            userStrokes = SketchTools.Clone(userStrokes);
            modelStrokes = SketchTools.Clone(modelStrokes);

            //SyncStrokes(...)

            // length test
            LengthTest(userStrokes, modelStrokes);

            // closeness test
            ClosenessTest(userStrokes, modelStrokes);

            // smoothness test
            SmoothnessTest(userStrokes, modelStrokes);

            // direction test
            DirectionTest(userStrokes, modelStrokes);
        }

        #region Tests

        private void SmoothnessTest(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            ;
        }

        private void DirectionTest(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
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
            else if (numIncorrect > 0)      { DirectionResult = ResultType.Med; }
            else                            { DirectionResult = ResultType.High; }
        }

        private void ClosenessTest(StrokeCollection userStrokes, StrokeCollection modelStrokes)
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
                    distance = SketchTools.Distance(userPoint, modelPoint);
                    distances.Add(distance);

                    // case: current distance exceeds closeness threshold
                    // record the current distance as a closeness error
                    if (distance > CLOSENESS_THRESHOLD_LOW)
                    {
                        ++numLow;
                    }
                    else if (CLOSENESS_THRESHOLD_LOW >= distance && distance > CLOSENESS_THRESHOLD_MED)
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
            if (numLow > 0)         { ClosenessResult = ResultType.Low; }
            else if (numMed > 0)    { ClosenessResult = ResultType.Med; }
            else                    { ClosenessResult = ResultType.High; }
        }

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

        #endregion

        #region Helper Methods

        private StrokeCollection SyncStrokes(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // initialize the list of map strokes
            StrokeCollection syncedStrokes = new StrokeCollection();
            string label = (string)modelStrokes.GetPropertyData(SketchTools.LABEL_GUID);

            // iterate through each user and model strokes
            int numPoints;
            Stroke userStroke, modelStroke, reverseStroke;
            StylusPoint lastPoint;
            int lastTime;
            double directDistance, reverseDistance;

            //
            for (int i = 0; i < userStrokes.Count; ++i)
            {
                // retrieve the current user and model strokes
                // also retrieve the last point of the pre-resampled last model point
                // due to resampling algorithm throwing away the last point
                // (without this, the resampled model stroke will have one less point than the user stroke)
                userStroke = userStrokes[i];
                modelStroke = SketchTools.Clone(modelStrokes[i]);  // clone model stroke so not affected

                // get the number of user stroke points
                numPoints = modelStroke.StylusPoints.Count;

                // resample the model stroke to match the number of user stroke points
                // this code fragment also adds the last point and time to the stroke
                lastPoint = userStroke.StylusPoints[userStroke.StylusPoints.Count - 1];
                lastTime = ((int[])userStroke.GetPropertyData(SketchTools.TIMES_GUID))[userStroke.StylusPoints.Count - 1];
                StrokeCollection tempStrokes = new StrokeCollection() { userStroke };
                tempStrokes.AddPropertyData(SketchTools.LABEL_GUID, "");
                userStroke = SketchTools.Resample(tempStrokes, numPoints)[0];
                userStroke.StylusPoints.Add(lastPoint);
                List<int> tempTimes = new List<int>() { lastTime };
                tempTimes.AddRange((int[])userStroke.GetPropertyData(SketchTools.TIMES_GUID));
                userStroke.AddPropertyData(SketchTools.TIMES_GUID, tempTimes.ToArray());

                // create the reverse stroke and calculate the direct and reverse pairwise stroke distances
                reverseStroke = SketchTools.Reverse(userStroke);
                directDistance = SketchTools.Distance(modelStroke, userStroke);
                reverseDistance = SketchTools.Distance(modelStroke, reverseStroke);

                // reserve the stroke if the reverse pairwise distance is shorter
                if (reverseDistance < directDistance)
                {
                    userStroke = reverseStroke;
                }
                syncedStrokes.Add(userStroke);
            }

            //
            syncedStrokes.AddPropertyData(SketchTools.LABEL_GUID, label);
            return syncedStrokes;
        }


        #endregion

        #region Debug

        private void Debug(List<double> distances)
        {
            string dirPath = @"C:\Users\paultaele\Desktop\debug\";
            int count = Directory.GetFiles(dirPath).Length;
            string filePath = dirPath + "debug_" + count + ".txt";
            using (StreamWriter file = new StreamWriter(filePath))
            {
                for (int i = 0; i < distances.Count; ++i)
                {
                    file.WriteLine(distances[i]);
                }
            }
        }

        private void Debug(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            Stroke userStroke, modelStroke;
            StylusPoint userPoint, modelPoint;

            string dirPath = @"C:\Users\paultaele\Desktop\debug\";
            int count = Directory.GetFiles(dirPath).Length;
            string filePath = dirPath + "error_" + count + ".txt";
            double distance;
            using (StreamWriter file = new StreamWriter(filePath))
            {
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

                        distance = SketchTools.Distance(userPoint, modelPoint);

                        file.Write(Math.Round(userPoint.X, 2) + "\t" + Math.Round(userPoint.Y, 2) + "\t");
                        file.Write(Math.Round(modelPoint.X, 2) + "\t" + Math.Round(modelPoint.Y, 2) + "\t");
                        file.WriteLine(distance);
                    }
                }
            }
        }

        #endregion

        #region Properties and Enums

        public double CanvasSize { get; set; } // debug

        public ResultType LengthResult { get; private set; }
        public ResultType ClosenessResult { get; private set; }
        public ResultType SmoothnessResult { get; private set; }
        public ResultType DirectionResult { get; private set; }

        public ResultType[] LengthResults { get; private set; }
        public ResultType[] ClosenessResults { get; private set; }
        public ResultType[] DirectionResults { get; private set; }
        public double[] LengthDebug { get; private set; }

        public enum ResultType { Low, Med, High }

        #endregion

        #region Fields

        private MainWindow myWindow; // debug

        public static readonly double LENGTH_THRESHOLD_LOW = 0.10;
        public static readonly double LENGTH_THRESHOLD_MED = 0.05;

        public static readonly double CLOSENESS_THRESHOLD_LOW = 40.0; // 12.5% of canvas width
        public static readonly double CLOSENESS_THRESHOLD_MED = 25.0; //  5.0% of canvas width

        #endregion
    }
}
