using System;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using Srl.Tools;

namespace Srl.Xml
{
    /// <summary>
    /// The SketchXmlProcessor class reads sketch data from XML files and writes sketch data to XML
    /// files. The list of constructors and methods below:
    /// * SketchXmlProcessor()
    /// * SketchXmlProcessor(Guid, Guid)
    /// * StrokeCollection Read(string)
    /// * void Write(string, StrokeCollection)
    /// * void Write(string, string, StrokeCollection)
    /// </summary>
    public class SketchXmlProcessor
    {
        /// <summary>
        /// The default constructor.
        /// </summary>
        public SketchXmlProcessor() : this(SketchTools.LABEL_GUID, SketchTools.TIMES_GUID)
        {
        }

        /// <summary>
        /// The constructor that takes in a label and times GUID.
        /// </summary>
        /// <param name="labelGuid">The label GUID.</param>
        /// <param name="timesGuid">The times GUID.</param>
        public SketchXmlProcessor(Guid labelGuid, Guid timesGuid)
        {
            myLabelGuid = labelGuid;
            myTimesGuid = timesGuid;
        }

        /// <summary>
        /// Reads the sketch data from the XML file.
        /// </summary>
        /// <param name="filePath">The location of the XML file.</param>
        /// <returns></returns>
        public StrokeCollection Read(string filePath)
        {
            // initialize the list of strokes
            StrokeCollection strokes = new StrokeCollection();

            // obtain the <sketch> document
            XDocument sketchDocument = XDocument.Load(filePath);

            // add the label to the list of strokes
            string label = sketchDocument.Root.Attribute("label").Value;
            strokes.AddPropertyData(myLabelGuid, label);

            // itereate through each stroke element
            foreach (XElement strokeElement in sketchDocument.Root.Elements())
            {
                // initialize the stylus point and time lists
                StylusPointCollection points = new StylusPointCollection();
                List<int> times = new List<int>();

                // iterate through each point element
                foreach (XElement pointElement in strokeElement.Elements())
                {
                    double x, y;
                    StylusPoint point;
                    int time;

                    x = Double.Parse(pointElement.Attribute("x").Value);
                    y = Double.Parse(pointElement.Attribute("y").Value);
                    point = new StylusPoint(x, y);
                    time = Int32.Parse(pointElement.Attribute("time").Value);

                    points.Add(point);
                    times.Add(time);
                }

                Stroke stroke = new Stroke(points);
                stroke.AddPropertyData(myTimesGuid, times.ToArray());
                strokes.Add(stroke);
            }

            // return the loaded strokes
            return strokes;
        }

        /// <summary>
        /// Writes the sketch data and its label to the XML file.
        /// </summary>
        /// <param name="filePath">The location of the XML file.</param>
        /// <param name="label">The label name.</param>
        /// <param name="strokes">The sketch data.</param>
        public void Write(string filePath, string label, StrokeCollection strokes)
        {
            XmlWriter writer = XmlWriter.Create(filePath);

            // <xml>
            writer.WriteStartDocument();

            // <sketch>
            writer.WriteStartElement("sketch");
            writer.WriteAttributeString("label", label);

            foreach (Stroke stroke in strokes)
            {
                // <stroke>
                writer.WriteStartElement("stroke");

                int[] times = (int[])stroke.GetPropertyData(myTimesGuid);
                for (int i = 0; i < stroke.StylusPoints.Count; ++i)
                {
                    StylusPoint point = stroke.StylusPoints[i];

                    // <point>
                    writer.WriteStartElement("point");

                    writer.WriteAttributeString("x", "" + point.X);
                    writer.WriteAttributeString("y", "" + point.Y);
                    writer.WriteAttributeString("time", "" + times[i]);

                    // </point>
                    writer.WriteEndElement();
                }

                // </stroke>
                writer.WriteEndElement();
            }

            // </sketch>
            writer.WriteEndElement();

            // </xml>
            writer.WriteEndDocument();

            writer.Close();
        }

        private Guid myLabelGuid;
        private Guid myTimesGuid;
    }
}