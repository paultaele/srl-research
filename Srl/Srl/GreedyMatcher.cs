using Srl.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Ink;
using System.Windows.Input;

namespace Srl.Tools
{
    public class GreedyMatcher
    {
        #region Constructor

        public GreedyMatcher(int resampleSize, double scaleBounds, StylusPoint origin, SketchTools.ScaleType scaleType, SketchTools.TranslateType translateType, bool isWeighted)
        {
            myResampleSize = resampleSize;
            myScaleBounds = scaleBounds;
            myOrigin = origin;
            myScaleType = scaleType;
            myTranslateType = translateType;
            myIsWeighted = isWeighted;
        }

        #endregion

        #region Match

        public void Train(string dirPath)
        {
            //
            myTrainingData = new List<StrokeCollection>();
            myTemplates = new List<StrokeCollection>();
            myTemplateToDatum = new Dictionary<StrokeCollection, StrokeCollection>();

            //
            SketchXmlProcessor processor = new SketchXmlProcessor();
            StrokeCollection trainingDatum, template;
            List<StrokeCollection> sketches = new List<StrokeCollection>();
            foreach (string filePath in Directory.GetFiles(dirPath))
            {
                //  handle each file that is an XML file
                if (filePath.EndsWith(".xml"))
                {
                    // convert the file to a sketch
                    trainingDatum = processor.Read(filePath);
                    myTrainingData.Add(trainingDatum);

                    // normalize the sketch
                    template = SketchTools.Normalize(SketchTools.Clone(trainingDatum), myResampleSize, myScaleBounds, myOrigin, myScaleType, myTranslateType);
                    myTemplates.Add(template);

                    // add new dictionary entry
                    myTemplateToDatum.Add(template, trainingDatum);
                }
            }
        }

        public void Run(StrokeCollection test)
        {
            //
            StrokeCollection input = SketchTools.Normalize(SketchTools.Clone(test), myResampleSize, myScaleBounds, myOrigin, myScaleType, myTranslateType);

            //
            myLabels = new List<string>();
            myScoreEntries = new Dictionary<StrokeCollection, double>();
            myResults = new List<StrokeCollection>();

            //
            List<Tuple<StrokeCollection, double>> pairs = new List<Tuple<StrokeCollection, double>>();
            double distance, distance1, distance2, score;
            foreach (StrokeCollection template in myTemplates)
            {
                // calculate the template's score
                distance1 = GreedyDistance(input, template);
                distance2 = GreedyDistance(template, input);
                distance = Math.Min(distance1, distance2);
                score = 100.0 - (distance / (0.5 * (Math.Sqrt(myScaleBounds * myScaleBounds + myScaleBounds * myScaleBounds))));

                // add the label, template+score pair, and score dictionary
                pairs.Add(new Tuple<StrokeCollection, double>(template, score));
            }

            // get the sorted results in ascending order
            pairs.Sort((x, y) => y.Item2.CompareTo(x.Item2));
            foreach (var pair in pairs)
            {
                //
                StrokeCollection t = pair.Item1;
                double s = pair.Item2;

                //
                myResults.Add(t);
                myLabels.Add((string)t.GetPropertyData(SketchTools.LABEL_GUID));
                myScoreEntries.Add(t, s);
            }
        }

        #endregion

        #region Accessors

        public string Label() { return myLabels[0]; }
        public List<string> Labels() { return myLabels; }
        public StrokeCollection Result() { return myResults[0]; }
        public List<StrokeCollection> Results() { return myResults; }
        public double Score(StrokeCollection template) { return myScoreEntries[template]; }
        public StrokeCollection TrainingData(StrokeCollection template) { return myTemplateToDatum[template]; }

        #endregion

        #region Helper Methods

        private double GreedyDistance(StrokeCollection alphaStrokes, StrokeCollection betaStrokes)
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
            double minDistance, weight, distance;
            int index;
            StylusPoint minPoint = betaPoints[0];
            foreach (var alphaPoint in alphaPoints)
            {
                minDistance = Double.MaxValue;

                // iterate through each beta point to find the min beta point to the alpha point
                index = 1;
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
                weight = 1 - ((index - 1) / alphaPoints.Count);
                if (myIsWeighted)
                {
                    distances += minDistance * weight;
                }
                else
                {
                    distances += minDistance;
                }

                // pair the alpha point to the min beta point and remove min beta point from list of beta points
                pairs.Add(new Tuple<StylusPoint, StylusPoint>(alphaPoint, minPoint));
                betaPoints.Remove(minPoint);
            }

            //
            return distances;
        }

        private string ToString(StrokeCollection sketch)
        {
            string output = "" + sketch.GetPropertyData(SketchTools.LABEL_GUID) + ":\n";

            int index = 0;
            foreach (Stroke stroke in sketch)
            {
                output += "Stroke #" + index;

                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    output += "   (" + point.X + ", " + point.Y + ")\n";
                }

                ++index;
            }

            return output;
        }

        #endregion

        #region Fields

        private int myResampleSize;
        private double myScaleBounds;
        private StylusPoint myOrigin;
        private SketchTools.ScaleType myScaleType;
        private SketchTools.TranslateType myTranslateType;
        private bool myIsWeighted;

        private List<StrokeCollection> myTrainingData;
        private List<StrokeCollection> myTemplates;
        private Dictionary<StrokeCollection, StrokeCollection> myTemplateToDatum;

        private List<StrokeCollection> myResults;
        private List<string> myLabels;
        private Dictionary<StrokeCollection, double> myScoreEntries;

        #endregion
    }
}
