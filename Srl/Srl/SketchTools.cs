using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;

namespace Srl.Tools
{
    public static class SketchTools
    {
        #region Normalize

        public static StrokeCollection Normalize(StrokeCollection strokes, int resampleSize, double scaleBounds, StylusPoint origin)
        {
            return Normalize(strokes, resampleSize, scaleBounds, origin, ScaleType.Hybrid, TranslateType.Centroid);
        }

        public static StrokeCollection Normalize(StrokeCollection strokes, int resampleSize, double scaleBounds, StylusPoint origin, ScaleType scaleType, TranslateType translateType)
        {
            // resample
            strokes = Resample(strokes, resampleSize);

            // scale
            if (scaleType.Equals(ScaleType.Proportional))
            {
                strokes = ScaleProportional(strokes, scaleBounds);
            }
            else if (scaleType.Equals(ScaleType.Square))
            {
                strokes = ScaleSquare(strokes, scaleBounds);
            }
            else
            {
                strokes = Scale(strokes, scaleBounds);
            }

            // translate
            if (translateType.Equals(TranslateType.Centroid))
            {
                strokes = TranslateCentroid(strokes, origin);
            }
            else
            {
                strokes = TranslateMedian(strokes, origin);
            }

            return strokes;
        }

        #endregion

        #region Resample

        public static StrokeCollection Resample(StrokeCollection strokes, int n)
        {
            // set the variable for point spacing
            // initialize the variable for total distance
            // initialize list for new strokes
            double I = PathLength(strokes) / (n - 1);
            double D = 0.0;
            StrokeCollection newStrokes = new StrokeCollection();

            // iterate through each stroke points in a list of strokes
            int pointCount = 0;
            foreach (Stroke stroke in strokes)
            {
                // initialize list of resampled stroke points
                // add the first stroke point
                StylusPointCollection points = stroke.StylusPoints;
                StylusPointCollection newPoints = new StylusPointCollection();
                newPoints.Add(points[0]);
                ++pointCount;
                //List<int> newTimes = new List<int>();
                //newTimes.Add(0);

                //
                bool isDone = false;
                for (int i = 1; i < points.Count(); ++i)
                {
                    double d = Distance(points[i - 1], points[i]);
                    if (D + d >= I)
                    {
                        double qx = points[i - 1].X + ((I - D) / d) * (points[i].X - points[i - 1].X);
                        double qy = points[i - 1].Y + ((I - D) / d) * (points[i].Y - points[i - 1].Y);
                        StylusPoint q = new StylusPoint(qx, qy);

                        if (pointCount < n - 1)
                        {
                            newPoints.Add(q);
                            ++pointCount;

                            //newTimes.Add(i);

                            points.Insert(i, q);
                            D = 0.0;
                        }
                        else
                        {
                            isDone = true;
                        }
                    }
                    else
                    {
                        D += d;
                    }

                    if (isDone)
                    {
                        break;
                    }
                }
                D = 0.0;

                //
                Stroke newStroke = new Stroke(newPoints);
                //newStroke.AddPropertyData(TIMES_GUID, newTimes.ToArray());
                newStrokes.Add(newStroke);
            }

            // add the label
            string label = (string)strokes.GetPropertyData(LABEL_GUID);
            newStrokes.AddPropertyData(LABEL_GUID, label);

            // add the times
            int[] oldTimes, newTimes;
            double ratio;
            int index;
            for (int i = 0; i < strokes.Count; ++i)
            {
                // get the old times and initialize the new times
                oldTimes = (int[])strokes[i].GetPropertyData(SketchTools.TIMES_GUID);
                newTimes = new int[newStrokes[i].StylusPoints.Count];

                ratio = (double)oldTimes.Length / (double)newTimes.Length;
                newTimes[0] = oldTimes[0];

                for (int j = 1; j < newTimes.Length - 1; ++j)
                {
                    index = (int)(j * ratio);
                    newTimes[j] = oldTimes[index];
                }
                newTimes[newTimes.Length - 1] = oldTimes[oldTimes.Length - 1];

                newStrokes[i].AddPropertyData(SketchTools.TIMES_GUID, newTimes);
            }

            return newStrokes;
        }

        #endregion

        #region Scale

        public static StrokeCollection Scale(StrokeCollection strokes, double size)
        {
            if (strokes.Count == 1 && !IsDiagonal(strokes[0]))
            {
                return ScaleProportional(strokes, size);
            }

            return ScaleSquare(strokes, size);
        }

        public static StrokeCollection ScaleProportional(StrokeCollection strokes, double size)
        {
            // get the scaling factor
            Rect B = strokes.GetBounds();
            double scale = B.Height > B.Width ? size / B.Height : size / B.Width;

            // get the offset
            double xoffset = Double.MaxValue;
            double yoffset = Double.MaxValue;
            foreach (Stroke stroke in strokes)
            {
                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    if (point.X < xoffset) xoffset = point.X;
                    if (point.Y < yoffset) yoffset = point.Y;
                }
            }

            // 
            StrokeCollection newStrokes = new StrokeCollection();
            foreach (Stroke stroke in strokes)
            {
                var newPoints = new StylusPointCollection();
                double x, y;
                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    x = ((point.X - xoffset) * scale) + xoffset;
                    y = ((point.Y - yoffset) * scale) + yoffset;
                    newPoints.Add(new StylusPoint(x, y));
                }

                Stroke newStroke = new Stroke(newPoints);
                newStroke.AddPropertyData(TIMES_GUID, stroke.GetPropertyData(TIMES_GUID));
                newStrokes.Add(newStroke);
            }

            //
            newStrokes.AddPropertyData(LABEL_GUID, strokes.GetPropertyData(LABEL_GUID));
            return newStrokes;
        }

        public static StrokeCollection ScaleSquare(StrokeCollection strokes, double size)
        {
            Rect B = strokes.GetBounds();

            // 
            StrokeCollection newStrokes = new StrokeCollection();
            foreach (Stroke stroke in strokes)
            {

                StylusPointCollection newPoints = new StylusPointCollection();

                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    double qx = point.X * size / B.Width;
                    double qy = point.Y * size / B.Height;
                    StylusPoint q = new StylusPoint(qx, qy);
                    newPoints.Add(q);
                }

                //
                Stroke newStroke = new Stroke(newPoints);
                newStroke.AddPropertyData(TIMES_GUID, stroke.GetPropertyData(TIMES_GUID));
                newStrokes.Add(newStroke);
            }

            //
            newStrokes.AddPropertyData(LABEL_GUID, strokes.GetPropertyData(LABEL_GUID));
            return newStrokes;
        }

        public static StrokeCollection Scale(StrokeCollection strokes, double size, ScaleType type)
        {
            if (type.Equals(ScaleType.Proportional))
            {
                return ScaleProportional(strokes, size);
            }
            else if (type.Equals(ScaleType.Square))
            {
                return ScaleSquare(strokes, size);
            }

            return Scale(strokes, size);
        }

        #endregion

        #region Translate

        public static StrokeCollection Translate(StrokeCollection strokes, StylusPoint k)
        {
            return Translate(strokes, k, TranslateType.Centroid);
        }

        public static StrokeCollection TranslateCentroid(StrokeCollection strokes, StylusPoint k)
        {
            return Translate(strokes, k, TranslateType.Centroid);
        }

        public static StrokeCollection TranslateMedian(StrokeCollection strokes, StylusPoint k)
        {
            return Translate(strokes, k, TranslateType.Median);
        }

        public static StrokeCollection Translate(StrokeCollection strokes, StylusPoint k, TranslateType type)
        {
            StrokeCollection newStrokes = new StrokeCollection();

            //
            List<StylusPoint> allPoints = new List<StylusPoint>();
            foreach (Stroke stroke in strokes)
            {

                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    allPoints.Add(point);
                }
            }

            //
            StylusPoint c = new StylusPoint();
            if (type.Equals(TranslateType.Centroid))
            {
                c = Centroid(allPoints);
            }
            else
            {
                c = Median(allPoints);
            }

            //
            foreach (Stroke stroke in strokes)
            {

                StylusPointCollection newPoints = new StylusPointCollection();

                foreach (StylusPoint point in stroke.StylusPoints)
                {

                    double qx = point.X + k.X - c.X;
                    double qy = point.Y + k.Y - c.Y;
                    StylusPoint q = new StylusPoint(qx, qy);
                    newPoints.Add(q);
                }

                //
                Stroke newStroke = new Stroke(newPoints);
                newStroke.AddPropertyData(TIMES_GUID, stroke.GetPropertyData(TIMES_GUID));
                newStrokes.Add(newStroke);
            }

            //
            newStrokes.AddPropertyData(LABEL_GUID, strokes.GetPropertyData(LABEL_GUID));
            return newStrokes;
        }

        #endregion

        #region Line Geometries

        public static bool IsLine(Stroke stroke)
        {
            StylusPointCollection points = stroke.StylusPoints;
            StylusPoint p0 = points[0];
            StylusPoint pn = points[points.Count - 1];

            double pathLength = PathLength(stroke);
            double length = Distance(p0, pn);
            double ratio = pathLength < length ? pathLength / length : length / pathLength;

            return ratio > MIN_LINE_RATIO;
        }

        public static bool IsHorizontal(Stroke stroke)
        {
            // case: line test
            if (!IsLine(stroke))
            {
                return false;
            }

            //
            double angle = Angle(stroke);
            if (Math.Abs(angle - 0.0) > ANGLE_DEVIATION && Math.Abs(angle - 180.0) > ANGLE_DEVIATION)
            {
                return false;
            }

            return true;
        }

        public static bool IsVertical(Stroke stroke)
        {
            // case: line test
            if (!IsLine(stroke))
            {
                return false;
            }

            //
            double angle = Angle(stroke);
            if (Math.Abs(angle - 90.0) > ANGLE_DEVIATION && Math.Abs(angle - 270.0) > ANGLE_DEVIATION)
            {
                return false;
            }

            return true;
        }

        public static bool IsDiagonal(Stroke stroke)
        {
            return !IsHorizontal(stroke) && !IsVertical(stroke);
        }

        #endregion

        #region Distance Measurements

        public static double PathLength(StrokeCollection strokes)
        {

            double d = 0.0;
            foreach (Stroke stroke in strokes)
            {

                d += PathLength(stroke);
            }

            return d;
        }

        public static double PathLength(Stroke stroke)
        {
            double d = 0.0;
            for (int i = 1; i < stroke.StylusPoints.Count(); ++i)
            {

                d += Distance(stroke.StylusPoints[i - 1], stroke.StylusPoints[i]);
            }

            return d;
        }

        public static double Distance(StylusPoint a, StylusPoint b)
        {
            double distance = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

            return distance;
        }

        public static double Distance(Stroke a, Stroke b)
        {
            StylusPointCollection apoints = a.StylusPoints;
            StylusPointCollection bpoints = b.StylusPoints;

            if (apoints.Count != bpoints.Count)
            {
                return 0.0;
            }

            double distance = 0.0;
            for (int i = 0; i < apoints.Count; ++i)
            {
                distance += Distance(apoints[i], bpoints[i]);
            }

            return distance;
        }

        public static double Angle(Stroke stroke)
        {
            StylusPointCollection points = stroke.StylusPoints;
            StylusPoint p1 = points[0];
            StylusPoint p2 = points[points.Count - 1];

            double deltaX = p2.X - p1.X;
            double deltaY = p2.Y - p1.Y;

            double angle = Math.Atan2(deltaY, deltaX) * 180.0 / Math.PI;
            if (angle < 0.0)
            {
                angle += 180.0;
            }

            return angle;
        }

        private static StylusPoint Centroid(List<StylusPoint> points)
        {

            double meanX = 0.0;
            double meanY = 0.0;
            foreach (StylusPoint point in points)
            {
                meanX += point.X;
                meanY += point.Y;
            }
            meanX /= points.Count;
            meanY /= points.Count;

            return new StylusPoint(meanX, meanY);
        }

        private static StylusPoint Median(List<StylusPoint> points)
        {
            double minX = Double.MaxValue;
            double minY = Double.MaxValue;
            double maxX = Double.MinValue;
            double maxY = Double.MinValue;
            foreach (StylusPoint point in points)
            {
                if (point.X < minX) minX = point.X;
                if (point.Y < minY) minY = point.Y;

                if (point.X > maxX) maxX = point.X;
                if (point.Y > maxY) maxY = point.Y;
            }

            return new StylusPoint((minX + maxX) / 2.0, (minY + maxY) / 2.0);
        }

        #endregion

        #region Helper Methods

        public static StrokeCollection Clone(StrokeCollection others)
        {
            // get GUID
            Guid labelGuid = SketchTools.LABEL_GUID;

            // initialize clone list
            StrokeCollection strokes = new StrokeCollection();
            strokes.AddPropertyData(labelGuid, others.GetPropertyData(labelGuid));

            // iterate through each original strokes
            foreach (Stroke other in others)
            {
                strokes.Add(Clone(other));
            }

            return strokes;
        }

        public static Stroke Clone(Stroke other)
        {
            // get GUID
            Guid timesGuid = SketchTools.TIMES_GUID;

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

            return stroke;
        }

        public static Stroke Reverse(Stroke other)
        {
            //
            int count = other.StylusPoints.Count;
            StylusPointCollection points = new StylusPointCollection();
            List<int> times = new List<int>();

            //
            int[] otherTimes = (int[])other.GetPropertyData(SketchTools.TIMES_GUID);
            for (int i = 0; i < count; ++i)
            {
                points.Add(other.StylusPoints[(count - 1) - i]);
                times.Add(otherTimes[(count - 1) - i]);
            }

            //
            Stroke stroke = new Stroke(points);
            stroke.AddPropertyData(SketchTools.TIMES_GUID, times.ToArray());

            return stroke;
        }

        #endregion

        #region Fields and Enums

        private static readonly double MIN_LINE_RATIO = 0.95;
        private static readonly double ANGLE_DEVIATION = 5.0;

        public static readonly Guid TIMES_GUID = new Guid("21EC2020-3AEA-4069-A2DD-08002B30309D");
        public static readonly Guid LABEL_GUID = new Guid("21EC2020-3AEA-4069-A2DD-08002B30309E");

        public enum ScaleType { Hybrid, Proportional, Square }
        public enum TranslateType { Centroid, Median }

        #endregion
    }
}
