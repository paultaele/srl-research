﻿//using System;
//using System.Linq;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Collections.Generic;
//using System.Windows;

//namespace NormalizeInterface
//{
//    public static class SketchTools
//    {
//        public static StrokeCollection Normalize(StrokeCollection strokes, int resampleSize, double scaleBounds, StylusPoint origin)
//        {
//            strokes = Resample(strokes, resampleSize);
//            strokes = Scale(strokes, scaleBounds);
//            strokes = Translate(strokes, origin);

//            return strokes;
//        }

//        public static StrokeCollection Resample(StrokeCollection strokes, int n)
//        {
//            // set the variable for point spacing
//            // initialize the variable for total distance
//            // initialize list for new strokes
//            double I = PathLength(strokes) / (n - 1);
//            double D = 0.0;
//            StrokeCollection newStrokes = new StrokeCollection();

//            // iterate through each stroke points in a list of strokes
//            foreach (Stroke stroke in strokes)
//            {

//                // initialize list of resampled stroke points
//                // add the first stroke point
//                StylusPointCollection points = stroke.StylusPoints;
//                StylusPointCollection newPoints = new StylusPointCollection();
//                newPoints.Add(points[0]);

//                //
//                for (int i = 1; i < points.Count(); ++i)
//                {

//                    double d = Distance(points[i - 1], points[i]);
//                    if (D + d >= I)
//                    {

//                        double qx = points[i - 1].X + ((I - D) / d) * (points[i].X - points[i - 1].X);
//                        double qy = points[i - 1].Y + ((I - D) / d) * (points[i].Y - points[i - 1].Y);
//                        StylusPoint q = new StylusPoint(qx, qy);
//                        newPoints.Add(q);
//                        points.Insert(i, q);
//                        D = 0.0;
//                    }
//                    else
//                    {
//                        D += d;
//                    }
//                }
//                D = 0.0;

//                //
//                Stroke newStroke = new Stroke(newPoints);
//                newStrokes.Add(newStroke);
//            }

//            //
//            string label = (string)strokes.GetPropertyData(LABEL_GUID);
//            newStrokes.AddPropertyData(LABEL_GUID, label);
//            return newStrokes;
//        }

//        public static StrokeCollection Scale(StrokeCollection strokes, double size)
//        {
//            if (strokes.Count == 1 && !IsDiagonal(strokes[0]))
//            {
//                return ScaleProportional(strokes, size);
//            }

//            return ScaleBox(strokes, size);
//        }

//        public static StrokeCollection Translate(StrokeCollection strokes, StylusPoint k)
//        {
//            StrokeCollection newStrokes = new StrokeCollection();

//            //
//            List<StylusPoint> allPoints = new List<StylusPoint>();
//            foreach (Stroke stroke in strokes)
//            {

//                foreach (StylusPoint point in stroke.StylusPoints)
//                {
//                    allPoints.Add(point);
//                }
//            }
//            StylusPoint c = Centroid(allPoints);

//            foreach (Stroke stroke in strokes)
//            {

//                StylusPointCollection newPoints = new StylusPointCollection();

//                foreach (StylusPoint point in stroke.StylusPoints)
//                {

//                    double qx = point.X + k.X - c.X;
//                    double qy = point.Y + k.Y - c.Y;
//                    StylusPoint q = new StylusPoint(qx, qy);
//                    newPoints.Add(q);
//                }

//                //
//                Stroke newStroke = new Stroke(newPoints);
//                newStrokes.Add(newStroke);
//            }

//            //
//            newStrokes.AddPropertyData(LABEL_GUID, strokes.GetPropertyData(LABEL_GUID));
//            return newStrokes;
//        }

//        public static bool IsLine(Stroke stroke)
//        {
//            StylusPointCollection points = stroke.StylusPoints;
//            StylusPoint p0 = points[0];
//            StylusPoint pn = points[points.Count - 1];

//            double pathLength = PathLength(stroke);
//            double length = Distance(p0, pn);
//            double ratio = pathLength < length ? pathLength / length : length / pathLength;

//            return ratio > MIN_LINE_RATIO;
//        }

//        public static bool IsHorizontal(Stroke stroke)
//        {
//            // case: line test
//            if (!IsLine(stroke))
//            {
//                return false;
//            }

//            //
//            double angle = Angle(stroke);
//            if (Math.Abs(angle - 0.0) > ANGLE_DEVIATION && Math.Abs(angle - 180.0) > ANGLE_DEVIATION)
//            {
//                return false;
//            }

//            return true;
//        }

//        public static bool IsVertical(Stroke stroke)
//        {
//            // case: line test
//            if (!IsLine(stroke))
//            {
//                return false;
//            }

//            //
//            double angle = Angle(stroke);
//            if (Math.Abs(angle - 90.0) > ANGLE_DEVIATION && Math.Abs(angle - 270.0) > ANGLE_DEVIATION)
//            {
//                return false;
//            }

//            return true;
//        }

//        public static bool IsDiagonal(Stroke stroke)
//        {
//            return !IsHorizontal(stroke) && !IsVertical(stroke);
//        }

//        public static StrokeCollection ScaleProportional(StrokeCollection strokes, double size)
//        {
//            // get the scaling factor
//            Rect B = strokes.GetBounds();
//            double scale = B.Height > B.Width ? size / B.Height : size / B.Width;

//            // get the offset
//            double xoffset = Double.MaxValue;
//            double yoffset = Double.MaxValue;
//            foreach (Stroke stroke in strokes)
//            {
//                foreach (StylusPoint point in stroke.StylusPoints)
//                {
//                    if (point.X < xoffset) xoffset = point.X;
//                    if (point.Y < yoffset) yoffset = point.Y;
//                }
//            }

//            // 
//            StrokeCollection newStrokes = new StrokeCollection();
//            foreach (Stroke stroke in strokes)
//            {
//                var newPoints = new StylusPointCollection();
//                double x, y;
//                foreach (StylusPoint point in stroke.StylusPoints)
//                {
//                    x = ((point.X - xoffset) * scale) + xoffset;
//                    y = ((point.Y - yoffset) * scale) + yoffset;
//                    newPoints.Add(new StylusPoint(x, y));
//                }

//                newStrokes.Add(new Stroke(newPoints));
//            }

//            //
//            newStrokes.AddPropertyData(LABEL_GUID, strokes.GetPropertyData(LABEL_GUID));
//            return newStrokes;
//        }

//        public static StrokeCollection ScaleBox(StrokeCollection strokes, double size)
//        {
//            Rect B = strokes.GetBounds();

//            // 
//            StrokeCollection newStrokes = new StrokeCollection();
//            foreach (Stroke stroke in strokes)
//            {

//                StylusPointCollection newPoints = new StylusPointCollection();

//                foreach (StylusPoint point in stroke.StylusPoints)
//                {
//                    double qx = point.X * size / B.Width;
//                    double qy = point.Y * size / B.Height;
//                    StylusPoint q = new StylusPoint(qx, qy);
//                    newPoints.Add(q);
//                }

//                //
//                Stroke newStroke = new Stroke(newPoints);
//                newStrokes.Add(newStroke);
//            }

//            //
//            newStrokes.AddPropertyData(LABEL_GUID, strokes.GetPropertyData(LABEL_GUID));
//            return newStrokes;
//        }

//        public static double PathLength(StrokeCollection strokes)
//        {

//            double d = 0.0;
//            foreach (Stroke stroke in strokes)
//            {

//                d += PathLength(stroke);
//            }

//            return d;
//        }

//        public static double PathLength(Stroke stroke)
//        {
//            double d = 0.0;
//            for (int i = 1; i < stroke.StylusPoints.Count(); ++i)
//            {

//                d += Distance(stroke.StylusPoints[i - 1], stroke.StylusPoints[i]);
//            }

//            return d;
//        }

//        public static double Distance(StylusPoint a, StylusPoint b)
//        {
//            double distance = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

//            return distance;
//        }

//        public static double Angle(Stroke stroke)
//        {
//            StylusPointCollection points = stroke.StylusPoints;
//            StylusPoint p1 = points[0];
//            StylusPoint p2 = points[points.Count - 1];

//            double deltaX = p2.X - p1.X;
//            double deltaY = p2.Y - p1.Y;

//            double angle = Math.Atan2(deltaY, deltaX) * 180.0 / Math.PI;
//            if (angle < 0.0)
//            {
//                angle += 180.0;
//            }

//            return angle;
//        }

//        private static StylusPoint Centroid(List<StylusPoint> points)
//        {

//            double meanX = 0.0;
//            double meanY = 0.0;
//            foreach (StylusPoint point in points)
//            {
//                meanX += point.X;
//                meanY += point.Y;
//            }
//            meanX /= points.Count;
//            meanY /= points.Count;

//            return new StylusPoint(meanX, meanY);
//        }



//        private static readonly double MIN_LINE_RATIO = 0.95;
//        private static readonly double ANGLE_DEVIATION = 5.0;

//        public static readonly Guid TIMES_GUID = new Guid("21EC2020-3AEA-4069-A2DD-08002B30309D");
//        public static readonly Guid LABEL_GUID = new Guid("21EC2020-3AEA-4069-A2DD-08002B30309E");
//    }
//}
