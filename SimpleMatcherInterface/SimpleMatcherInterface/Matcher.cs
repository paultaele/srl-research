using Srl.Tools;
using Srl.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Ink;
using System.Windows.Input;

namespace SimpleMatcherInterface
{
    public class Matcher
    {
        public Matcher(int resampleSize, double scaleBounds, StylusPoint origin, SketchTools.ScaleType scaleType, SketchTools.TranslateType translateType)
        {
            myResampleSize = resampleSize;
            myScaleBounds = scaleBounds;
            myOrigin = origin;
            myScaleType = scaleType;
            myTranslateType = translateType;
        }

        public void Train(string dirPath)
        {
            // get the file paths
            var filePaths = new List<string>();
            foreach (var filePath in Directory.GetFiles(dirPath))
            {
                if (filePath.EndsWith(".xml"))
                {
                    filePaths.Add(filePath);
                }
            }

            // get the raw training data
            myTrainingData = new List<StrokeCollection>();
            SketchXmlProcessor processor = new SketchXmlProcessor();
            foreach (var filePath in filePaths)
            {
                myTrainingData.Add(processor.Read(filePath));
            }

            // normalize the training data into templates
            myTemplates = new List<StrokeCollection>();
            StrokeCollection template;
            foreach (var train in myTrainingData)
            {
                template = SketchTools.Normalize(train,
                    myResampleSize,
                    myScaleBounds,
                    myOrigin,
                    SketchTools.ScaleType.Proportional,
                    SketchTools.TranslateType.Median);

                myTemplates.Add(template);
            }

            // set up the template to training dictionary pairs
            myTemplateTrainingPairs = new Dictionary<StrokeCollection, StrokeCollection>();
            for (int i = 0; i < myTrainingData.Count; ++i)
            {
                myTemplateTrainingPairs.Add(myTemplates[i], myTrainingData[i]);
            }
        }

        public void Run(StrokeCollection original)
        {
            //
            StrokeCollection input = SketchTools.Normalize(
                SketchTools.Clone(original),
                myResampleSize,
                myScaleBounds,
                myOrigin,
                myScaleType,
                myTranslateType);

            // calculate the distance between input and shallow clone of templates
            List<Tuple<StrokeCollection, double>> pairs
                = Scores(input, new List<StrokeCollection>(myTemplates));

            // sort the distance pairs
            pairs.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            //
            myResults = new List<StrokeCollection>();
            myLabels = new List<string>();
            myScores = new Dictionary<StrokeCollection, double>();

            //
            var template = new StrokeCollection();
            string label = "";
            double score = 0.0;
            foreach (var pair in pairs)
            {
                template = pair.Item1;
                label = (string)template.GetPropertyData(SketchTools.LABEL_GUID);
                score = pair.Item2;

                myResults.Add(template);
                myLabels.Add(label);
                myScores.Add(template, score);
            }
        }

        #region Accessors

        public string Label()
        {
            return myLabels[0];
        }

        public List<string> Labels()
        {
            return myLabels;
        }

        public StrokeCollection Result()
        {
            return myResults[0];
        }

        public List<StrokeCollection> Results()
        {
            return myResults;
        }

        public double Score(StrokeCollection template)
        {
            return myScores[template];
        }

        public StrokeCollection TrainingData(StrokeCollection template)
        {
            return myTemplateTrainingPairs[template];
        }

        #endregion

        #region Helper Methods

        private List<Tuple<StrokeCollection, double>> Scores(StrokeCollection input, List<StrokeCollection> templates)
        {
            var templateScoresPairs = new List<Tuple<StrokeCollection, double>>();

            double distance;
            double score;
            foreach (var template in templates)
            {
                distance = Distance(input, template);
                score = 100.0 - (distance / (0.5 * (Math.Sqrt(myScaleBounds*myScaleBounds + myScaleBounds* myScaleBounds))));
                templateScoresPairs.Add(new Tuple<StrokeCollection, double>(template, score));
            }

            return templateScoresPairs;
        }

        private double Distance(StrokeCollection input, StrokeCollection template)
        {
            StylusPointCollection inputPoints = ToPoints(input);
            StylusPointCollection templatePoints = ToPoints(template);

            double minDistance;
            StylusPoint minPoint;
            double currentDistance, weight;
            double distance = 0.0;
            double index = 1;
            foreach (var inputPoint in inputPoints)
            {
                minDistance = Double.MaxValue;
                minPoint = templatePoints[0];
                foreach (var templatePoint in templatePoints)
                {
                    currentDistance = SketchTools.Distance(inputPoint, templatePoint);
                    if (currentDistance < minDistance)
                    {
                        minDistance = currentDistance;
                        minPoint = templatePoint;
                    }
                }

                weight = 1 - ((index - 1) / inputPoints.Count);
                distance += minDistance * weight;

                templatePoints.Remove(minPoint);
                ++index;
            }

            return distance;
        }

        private StylusPointCollection ToPoints(StrokeCollection strokes)
        {
            var points = new StylusPointCollection();

            foreach (var stroke in strokes)
            {
                foreach (var point in stroke.StylusPoints)
                {
                    points.Add(point);
                }
            }

            return points;
        }

        #endregion

        #region Fields and Enum

        private List<StrokeCollection> myTrainingData;
        private List<StrokeCollection> myTemplates;
        private Dictionary<StrokeCollection, StrokeCollection> myTemplateTrainingPairs;

        private int myResampleSize;
        private double myScaleBounds;
        private StylusPoint myOrigin;
        private SketchTools.ScaleType myScaleType;
        private SketchTools.TranslateType myTranslateType;

        private List<StrokeCollection> myResults;
        private List<string> myLabels;
        private Dictionary<StrokeCollection, double> myScores;

        #endregion
    }
}
