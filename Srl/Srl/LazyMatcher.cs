using System;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Input;

namespace Srl.Tools
{
    public class LazyMatcher : BaseMatcher
    {
        #region Constructor

        public LazyMatcher() : base(RESAMPLE_SIZE, SCALE_BOUNDS, ORIGIN, SCALE_TYPE, TRANSLATE_TYPE) { }

        public LazyMatcher(int resampleSize, double scaleBounds, StylusPoint origin, SketchTools.ScaleType scaleType, SketchTools.TranslateType translateType)
            : base(resampleSize, scaleBounds, origin, scaleType, translateType) { }

        #endregion

        #region Match

        public override void Run(StrokeCollection test)
        {
            //
            StrokeCollection input = SketchTools.Normalize(SketchTools.Clone(test), myResampleSize, myScaleBounds, myOrigin, myScaleType, myTranslateType);

            //
            myLabels = new List<string>();
            myScoreEntries = new Dictionary<StrokeCollection, double>();
            myResults = new List<StrokeCollection>();

            // TODO
            double distance, distance1, distance2, score;
            foreach (StrokeCollection template in myTemplates)
            {
                // calculate the template's score
                distance1 = Distance(input, template);
                distance2 = Distance(template, input);
                // TODO
            }
        }

        #endregion

        #region Accessors

        public override string Label()
        {
            throw new NotImplementedException();
        }

        public override List<string> Labels()
        {
            throw new NotImplementedException();
        }

        public override StrokeCollection Result()
        {
            throw new NotImplementedException();
        }

        public override List<StrokeCollection> Results()
        {
            throw new NotImplementedException();
        }

        public override double Score(StrokeCollection template)
        {
            throw new NotImplementedException();
        }

        public override StrokeCollection TrainingData(StrokeCollection template)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Helper Methods

        private double Distance(StrokeCollection alphaStrokes, StrokeCollection betaStrokes)
        {
            //
            double distances = 0.0;

            // get the alpha and beta points from their respective strokes
            var alphaPoints = new StylusPointCollection();
            var betaPoints = new StylusPointCollection();
            foreach (var stroke in alphaStrokes)
            {
                alphaPoints.Add(stroke.StylusPoints);
            }
            foreach (var stroke in betaStrokes)
            {
                betaPoints.Add(stroke.StylusPoints);
            }

            // iterate through each alpha point
            var pairs = new List<Tuple<StylusPoint, StylusPoint>>();
            double minDistance, /*weight,*/ distance;
            //int index;
            StylusPoint minPoint = betaPoints[0];
            foreach (var alphaPoint in alphaPoints)
            {
                minDistance = Double.MaxValue;

                // iterate through each beta point to find the min beta point to the alpha point
                //index = 1;
                foreach (var betaPoint in betaPoints)
                {
                    distance = SketchTools.Distance(alphaPoint, betaPoint);

                    // update the min distance and min point
                    if (minDistance > distance)
                    {
                        minDistance = distance;
                        minPoint = betaPoint;
                    }
                }

                // update distance between alpha and beta point lists
                //weight = 1 - ((index - 1) / alphaPoints.Count);
                //distances += minDistance * weight;
                distances += minDistance;

                // pair the alpha point to the min beta point and remove min beta point from list of beta points
                pairs.Add(new Tuple<StylusPoint, StylusPoint>(alphaPoint, minPoint));
            }

            //
            return distances;
        }

        #endregion
    }
}
