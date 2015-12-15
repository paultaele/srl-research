using Srl.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Ink;
using System.Windows.Input;

namespace Srl.Tools
{
    public abstract class BaseMatcher : Matcher
    {
        #region Constructors

        protected BaseMatcher() : this(RESAMPLE_SIZE, SCALE_BOUNDS, ORIGIN, SCALE_TYPE, TRANSLATE_TYPE) { }

        protected BaseMatcher(int resampleSize, double scaleBounds, StylusPoint origin, SketchTools.ScaleType scaleType, SketchTools.TranslateType translateType)
        {
            myResampleSize = resampleSize;
            myScaleBounds = scaleBounds;
            myOrigin = origin;
            myScaleType = scaleType;
            myTranslateType = translateType;
        }

        #endregion

        #region Match

        public abstract void Run(StrokeCollection test);

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

        #endregion

        #region Accessors

        public abstract string Label();
        public abstract List<string> Labels();
        public abstract StrokeCollection Result();
        public abstract List<StrokeCollection> Results();
        public abstract double Score(StrokeCollection template);
        public abstract StrokeCollection TrainingData(StrokeCollection template);

        #endregion

        #region Helper Methods

        protected double ToScore(double distance)
        {
            return 100.0 - (distance / (0.5 * (Math.Sqrt(myScaleBounds * myScaleBounds + myScaleBounds * myScaleBounds))));
        }

        #endregion

        #region Fields

        protected List<StrokeCollection> myTrainingData;
        protected List<StrokeCollection> myTemplates;
        protected Dictionary<StrokeCollection, StrokeCollection> myTemplateToDatum;

        protected List<StrokeCollection> myResults;
        protected List<string> myLabels;
        protected Dictionary<StrokeCollection, double> myScoreEntries;

        protected int myResampleSize;
        protected double myScaleBounds;
        protected StylusPoint myOrigin;
        protected SketchTools.ScaleType myScaleType;
        protected SketchTools.TranslateType myTranslateType;

        public static readonly int RESAMPLE_SIZE = 128;
        public static readonly double SCALE_BOUNDS = 500;
        public static readonly StylusPoint ORIGIN = new StylusPoint(0.0, 0.0);
        public static readonly SketchTools.ScaleType SCALE_TYPE = SketchTools.ScaleType.Proportional;
        public static readonly SketchTools.TranslateType TRANSLATE_TYPE = SketchTools.TranslateType.Centroid;

        #endregion
    }
}