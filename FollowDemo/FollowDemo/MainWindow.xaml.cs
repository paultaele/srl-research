using Srl.Tools;
using Srl.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FollowDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Window Behaviors

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //
            if (CAN_INTERRUPT) { MyCanvas.IsEnabled = true; }

            // 0. initialize the control size
            ResizeControls();

            // 1. set stroke visual properties
            InitializeVisuals(out myModelVisuals, out myMapVisuals, out myMask);

            // 2. initialize labels list and models and images dictionaries
            InitializeData(out myLabels, out myModelsDictionary, out myImagesDictionary, MODELS_DIR, IMAGES_DIR);

            // 3. initialize the indexer
            myIndexer = 0;

            // 4. add the image and mask
            MyCanvas.Children.Add(myImagesDictionary[myLabels[myIndexer]]);
            MyCanvas.Children.Add(myMask);

            // 5. enable next button if there is more than one model and image
            if (myLabels.Count > 0)
            {
                MyNextButton.IsEnabled = true;
            }

            #region Boilerplate Code

            // initialize an empty list of strokes
            myStrokes = new StrokeCollection();

            // set the stylus and mouse state flags and states
            IsStylusMove = false;
            IsStylusEnd = false;
            IsMouseDown = false;
            IsMouseMove = false;
            IsMouseUp = false;
            IsMouseReady = false;
            PreviousStylusState = StylusState.StylusUp;
            PreviousMouseState = MouseState.MouseUp;

            // add mouse button event handlers to the canvas for mouse up, mouse down, and mouse move
            MyCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(MyCanvas_PreviewMouseDown), true);
            MyCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(MyCanvas_PreviewMouseMove), true);
            MyCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(MyCanvas_PreviewMouseUp), true);

            #endregion
        }

        private void MyWindow_ContentRendered(object sender, EventArgs e)
        {
            // 6. animate the current model stroke on the canvas
            myAnimationIndexer = 0;
            myAnimatedStroke = AnimateStroke(myModelsDictionary[myLabels[myIndexer]][myAnimationIndexer]);
        }

        private void AnimateStrokes(StrokeCollection strokes)
        {
            //
            double timeSpan = 5;
            double strokeThickness = 10;
            Brush lineColor = Brushes.LightSalmon;

            //
            myAnimatedStroke = new List<Line>();

            // initialize the storyboard for animating the stroke
            Storyboard storyboard;
            storyboard = new Storyboard();

            //
            StylusPointCollection points;
            Line demoStroke;
            double sum = 0;
            double offset;
            for (int i = 0; i < strokes.Count; ++i)
            {
                points = strokes[i].StylusPoints;
                offset = sum;

                for (int j = 0; j < points.Count - 1; ++j)
                {
                    // creat a new line for current line segment
                    demoStroke = new Line();
                    demoStroke.Stroke = lineColor;
                    demoStroke.StrokeThickness = strokeThickness;

                    // data from list
                    StylusPoint startPoint = points[j];
                    StylusPoint endPoint = points[j + 1];

                    //set startpoint = endpoint will result in the line not being drawn
                    demoStroke.X1 = startPoint.X;
                    demoStroke.Y1 = startPoint.Y;
                    demoStroke.X2 = startPoint.X;
                    demoStroke.Y2 = startPoint.Y;
                    myAnimatedStroke.Add(demoStroke);
                    MyCanvas.Children.Add(demoStroke);

                    //Initialize the animations with duration of 1 second for each segment
                    sum += timeSpan;
                    DoubleAnimation daX = new DoubleAnimation(endPoint.X, new Duration(TimeSpan.FromMilliseconds(timeSpan)));
                    DoubleAnimation daY = new DoubleAnimation(endPoint.Y, new Duration(TimeSpan.FromMilliseconds(timeSpan)));

                    //Define the begin time. This is sum of durations of earlier animations + 10 ms delay for each
                    daX.BeginTime = TimeSpan.FromMilliseconds(offset + j * timeSpan);
                    daY.BeginTime = TimeSpan.FromMilliseconds(offset + j * timeSpan);

                    //
                    //daX.Completed += Animation_Completed;

                    //
                    storyboard.Children.Add(daX);
                    storyboard.Children.Add(daY);

                    //Set the targets for the animations
                    Storyboard.SetTarget(daX, demoStroke);
                    Storyboard.SetTarget(daY, demoStroke);
                    Storyboard.SetTargetProperty(daX, new PropertyPath(Line.X2Property));
                    Storyboard.SetTargetProperty(daY, new PropertyPath(Line.Y2Property));
                }
            }

            // for some reason, must define Completed event handling behavior
            // before beginning the animation
            storyboard.Completed += Storyboard_Completed;

            // run the animation
            storyboard.Begin(this);
        }

        private List<Line> AnimateStroke(Stroke staticStroke)
        {
            // set the animated stroke's visual properties and behaviors
            double timeSpan = ANIMATION_TIME_SPAN;
            double lineWidth = ANIMATION_LINE_WIDTH;
            Brush lineColor = ANIMATION_LINE_COLOR;

            // initialize the animated stroke represented as a list of animated mini-lines
            List<Line> animatedStroke = new List<Line>();

            // initialize the storyboard for animating the stroke
            Storyboard storyboard = new Storyboard();

            // iterate through the static stroke's points
            StylusPointCollection staticPoints = staticStroke.StylusPoints;
            Line animatedPoint = null;
            StylusPoint startPoint;
            StylusPoint endPoint;
            DoubleAnimation animationX;
            DoubleAnimation animationY;

            for (int j = 0; j < staticPoints.Count - 1; ++j)
            {
                // initialize a new animated point
                animatedPoint = new Line();
                animatedPoint.Stroke = lineColor;
                animatedPoint.StrokeThickness = lineWidth;

                // set the animated point's start and end behavior
                startPoint = staticPoints[j];
                endPoint = staticPoints[j + 1];

                // set the startpoint != endpoint, or else will result in the line not being drawn
                animatedPoint.X1 = startPoint.X;
                animatedPoint.Y1 = startPoint.Y;
                animatedPoint.X2 = startPoint.X;
                animatedPoint.Y2 = startPoint.Y;

                // add the animated point to the animated stroke and to the canvas
                animatedStroke.Add(animatedPoint);
                MyCanvas.Children.Add(animatedPoint);

                // initialize the animation behaviors
                animationX = new DoubleAnimation(endPoint.X, new Duration(TimeSpan.FromMilliseconds(timeSpan)));
                animationY = new DoubleAnimation(endPoint.Y, new Duration(TimeSpan.FromMilliseconds(timeSpan)));

                // define the animation's begin time
                // this is sum of durations of earlier animations
                animationX.BeginTime = TimeSpan.FromMilliseconds(j * timeSpan);
                animationY.BeginTime = TimeSpan.FromMilliseconds(j * timeSpan);

                // include the animation behaviors into the storyboard
                storyboard.Children.Add(animationX);
                storyboard.Children.Add(animationY);

                // set the targets for the animations
                Storyboard.SetTarget(animationX, animatedPoint);
                Storyboard.SetTarget(animationY, animatedPoint);
                Storyboard.SetTargetProperty(animationX, new PropertyPath(Line.X2Property));
                Storyboard.SetTargetProperty(animationY, new PropertyPath(Line.Y2Property));
            }

            // for some reason, must define Completed event handling behavior
            // before beginning the animation
            storyboard.Completed += Storyboard_Completed;

            // run the animation
            storyboard.Begin(this);

            return animatedStroke;
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            if (!CAN_INTERRUPT) { MyCanvas.IsEnabled = true; }
        }

        private void MyWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeControls();
        }

        private void InitializeVisuals(out DrawingAttributes modelVisuals, out DrawingAttributes mapVisuals, out Rectangle mask)
        {
            //
            MyCanvas.DefaultDrawingAttributes.Color = USER_STROKE_COLOR;
            MyCanvas.DefaultDrawingAttributes.Width = USER_STROKE_SIZE;
            MyCanvas.DefaultDrawingAttributes.Height = USER_STROKE_SIZE;

            //
            modelVisuals = new DrawingAttributes();
            modelVisuals.Color = MODEL_STROKE_COLOR;
            modelVisuals.Width = MyCanvas.DefaultDrawingAttributes.Width;
            modelVisuals.Height = MyCanvas.DefaultDrawingAttributes.Height;

            //
            mapVisuals = new DrawingAttributes();
            mapVisuals.Color = MAP_STROKE_COLOR;
            mapVisuals.Width = 1.0;
            mapVisuals.Height = 1.0;

            //
            mask = new Rectangle();
            mask.Fill = MASK_COLOR;
            mask.Opacity = MASK_OPACITY;
            mask.Width = MyCanvasBorder.ActualWidth;
            mask.Height = MyCanvasBorder.ActualHeight;
        }

        private void InitializeData(out List<string> labels, out Dictionary<string, StrokeCollection> modelsDictionary, out Dictionary<string, Image> imagesDictionary, string modelsDir, string imagesDir)
        {
            //
            labels = new List<string>();
            modelsDictionary = new Dictionary<string, StrokeCollection>();
            imagesDictionary = new Dictionary<string, Image>();

            // create labels list and models and images dictionaries
            SketchXmlProcessor processor = new SketchXmlProcessor();
            StrokeCollection modelStrokes;
            string sketchName;
            foreach (string modelFilePath in Directory.GetFiles(modelsDir))
            {
                if (modelFilePath.EndsWith(".xml"))
                {
                    modelStrokes = CreateSketch(modelFilePath);
                    sketchName = (string)modelStrokes.GetPropertyData(SketchTools.LABEL_GUID);
                    labels.Add(sketchName);

                    modelsDictionary.Add(sketchName, modelStrokes);
                }
            }

            Image image;
            string imageName;
            foreach (string imageFilePath in Directory.GetFiles(imagesDir))
            {
                if (imageFilePath.EndsWith(".png"))
                {
                    image = CreateImage(imageFilePath);
                    imageName = System.IO.Path.GetFileNameWithoutExtension(imageFilePath);

                    imagesDictionary.Add(imageName, image);
                }
            }
        }

        private void ResizeControls()
        {
            // resize canvas
            MyCanvasBorder.Width = MyCanvasBorder.ActualHeight;
            MyCanvasBorder.Height = MyCanvasBorder.ActualHeight;

            // resize instructions
            MyInstructionsBorder.Width = MyCanvasBorder.Width;

            // resize buttons
            MyButtonsBorder.Width = MyCanvasBorder.Width;
            MyBackButton.Width = MyBackButton.Height = MyButtonsBorder.ActualHeight;
            MyClearButton.Width = MyClearButton.Height = MyButtonsBorder.ActualHeight;
            MyUndoButton.Width = MyUndoButton.Height = MyButtonsBorder.ActualHeight;
            MySaveButton.Width = MySaveButton.Height = MyButtonsBorder.ActualHeight;
            MyCheckButton.Width = MyCheckButton.Height = MyButtonsBorder.ActualHeight;
            MyNextButton.Width = MyNextButton.Height = MyButtonsBorder.ActualHeight;
        }

        #endregion

        #region Menu Bar Interactions

        private void MyExitItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Button Interactions

        private void MySaveButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyCheckButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyBackButton_Click(object sender, RoutedEventArgs e)
        {
            // disable the canvas initially (to allow for animation to finish drawing)
            if (!CAN_INTERRUPT) { MyCanvas.IsEnabled = false; }

            // clear output panel
            MyOutputBlock.Text = "";

            // remove the current image and any drawn strokes, model, and animated strokes
            MyCanvas.Children.Remove(myImagesDictionary[myLabels[myIndexer]]);
            MyCanvas.Children.Remove(myMask);
            if (IsModelDisplayed)
            {
                MyCanvas.Strokes.Remove(myModelsDictionary[myLabels[myIndexer]]);
                IsModelDisplayed = false;
            }
            if (IsMappingDisplayed)
            {
                MyCanvas.Strokes.Remove(myMappingStrokes);
                IsMappingDisplayed = false;
            }
            ClearStrokes();
            foreach (Line animatedPoint in myAnimatedStroke)
            {
                MyCanvas.Children.Remove(animatedPoint);
            }

            // update the symbol indexer and reset the animation indexer
            --myIndexer;
            myAnimationIndexer = 0;

            // add the updated image and animated stroke
            MyCanvas.Children.Add(myImagesDictionary[myLabels[myIndexer]]);
            MyCanvas.Children.Add(myMask);
            myAnimatedStroke = AnimateStroke(myModelsDictionary[myLabels[myIndexer]][myAnimationIndexer]);

            // disable button if at limit of index
            MyNextButton.IsEnabled = true;
            if (myIndexer == 0)
            {
                MyBackButton.IsEnabled = false;
            }
        }

        private void MyNextButton_Click(object sender, RoutedEventArgs e)
        {
            // disable the canvas initially (to allow for animation to finish drawing)
            if (!CAN_INTERRUPT) { MyCanvas.IsEnabled = false; }

            // clear output panel
            MyOutputBlock.Text = "";

            // remove the current image and any drawn strokes, model, mapping and animated strokes
            MyCanvas.Children.Remove(myImagesDictionary[myLabels[myIndexer]]);
            MyCanvas.Children.Remove(myMask);
            if (IsModelDisplayed)
            {
                MyCanvas.Strokes.Remove(myModelsDictionary[myLabels[myIndexer]]);
                IsModelDisplayed = false;
            }
            if (IsMappingDisplayed)
            {
                MyCanvas.Strokes.Remove(myMappingStrokes);
                IsMappingDisplayed = false;
            }
            ClearStrokes();
            foreach (Line animatedPoint in myAnimatedStroke)
            {
                MyCanvas.Children.Remove(animatedPoint);
            }

            // update the symbol indexer and reset the animation indexer
            ++myIndexer;
            myAnimationIndexer = 0;

            // add the updated image and animated stroke
            MyCanvas.Children.Add(myImagesDictionary[myLabels[myIndexer]]);
            MyCanvas.Children.Add(myMask);
            myAnimatedStroke = AnimateStroke(myModelsDictionary[myLabels[myIndexer]][myAnimationIndexer]);

            // disable button if at limit of index
            MyBackButton.IsEnabled = true;
            if (myIndexer == myLabels.Count - 1)
            {
                MyNextButton.IsEnabled = false;
            }
        }

        private void EnableButtons(bool enable)
        {
            //MySaveButton.IsEnabled = enable;
            //MyCheckButton.IsEnabled = enable;
            //MyClearButton.IsEnabled = enable;
            //MyUndoButton.IsEnabled = enable;
        }

        #endregion

        #region Helper Methods

        private double ScaleFactor(double controlWidth, double controlHeight, double imageWidth, double imageHeight)
        {
            double canvasRatio = controlHeight / controlWidth;
            double imageRatio = imageHeight / imageWidth;
            double scaleFactor = canvasRatio <= imageRatio
                ? controlHeight / imageHeight
                : controlWidth / imageWidth;

            return scaleFactor;
        }

        private Image CreateImage(string filePath)
        {
            // initialize the image object and set the dimensions to the original image file
            Image image = new Image();
            image.Source = new BitmapImage(new Uri(filePath));
            image.Width = image.Source.Width;
            image.Height = image.Source.Height;

            double canvasWidth = MyCanvasBorder.Width;
            double canvasHeight = MyCanvasBorder.Height;
            double scaleFactor = ScaleFactor(400, 400, image.Width, image.Width);

            image.Width = MyCanvasBorder.Width;
            image.Height = MyCanvasBorder.Height;
            InkCanvas.SetLeft(image, -5);
            InkCanvas.SetTop(image, -5);

            return image;
        }

        private StrokeCollection CreateSketch(string filePath)
        {
            // initialize XML processor for reading XML file
            SketchXmlProcessor processor = new SketchXmlProcessor();

            // resample the sketch
            StrokeCollection sketch = processor.Read(filePath);
            sketch = SketchTools.Resample(sketch, RESAMPLE_POINTS);

            // wrap a box border around the sketch
            // needed to ensure consistent scaling for each of the originally collected data
            StylusPointCollection boxPoints = new StylusPointCollection();
            boxPoints.Add(new StylusPoint(0, 0));
            boxPoints.Add(new StylusPoint(0, BOX_SIZE));
            boxPoints.Add(new StylusPoint(BOX_SIZE, BOX_SIZE));
            boxPoints.Add(new StylusPoint(BOX_SIZE, 0));
            boxPoints.Add(new StylusPoint(0, 0));
            Stroke box = new Stroke(boxPoints);
            box.AddPropertyData(SketchTools.TIMES_GUID, new int[boxPoints.Count]);
            sketch.Add(box);

            // scale the sketch to the length (i.e., height) of the canvas
            sketch = SketchTools.Scale(sketch, MyCanvasBorder.Height, SketchTools.ScaleType.Proportional);

            // remove the last stroke from the sketch that is the box stroke
            // note: must manually remove the stroke from the index fdsafda
            sketch.Remove(sketch[sketch.Count - 1]);

            //
            foreach (Stroke stroke in sketch)
            {
                stroke.DrawingAttributes = myModelVisuals;
            }

            return sketch;
        }

        private StrokeCollection CreateMapping(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // initialize the list of map strokes
            StrokeCollection mapStrokes = new StrokeCollection();
            string label = (string)modelStrokes.GetPropertyData(SketchTools.LABEL_GUID);

            // iterate through each user and model strokes
            Stroke userStroke, modelStroke, mapStroke;
            for (int i = 0; i < userStrokes.Count; ++i)
            {
                //
                userStroke = userStrokes[i];
                modelStroke = modelStrokes[i];

                // create the corresponding map stroke for each pairwise user and model stroke point
                for (int j = 0; j < modelStroke.StylusPoints.Count; ++j)
                {
                    mapStroke = new Stroke(new StylusPointCollection() { userStroke.StylusPoints[j], modelStroke.StylusPoints[j] });
                    mapStroke.DrawingAttributes = myMapVisuals;

                    mapStrokes.Add(mapStroke);
                }
            }

            //
            return mapStrokes;
        }

        private StrokeCollection CreateMapping3(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // initialize the list of map strokes
            StrokeCollection mapStrokes = new StrokeCollection();
            string label = (string)modelStrokes.GetPropertyData(SketchTools.LABEL_GUID);

            // iterate through each user and model strokes
            int numPoints;
            Stroke userStroke, modelStroke, reverseStroke, mapStroke;
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
                userStroke = SketchTools.Clone(userStrokes[i]);
                modelStroke = SketchTools.Clone(modelStrokes[i]);

                // get the number of mode stroke points
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

                // create the corresponding map stroke for each pairwise user and model stroke point
                for (int j = 0; j < modelStroke.StylusPoints.Count; ++j)
                {
                    mapStroke = new Stroke(new StylusPointCollection() { userStroke.StylusPoints[j], modelStroke.StylusPoints[j] });
                    mapStroke.DrawingAttributes = myMapVisuals;

                    mapStrokes.Add(mapStroke);
                }
            }

            //
            return mapStrokes;
        }

        private StrokeCollection CreateMapping2(StrokeCollection userStrokes, StrokeCollection modelStrokes)
        {
            // initialize the list of map strokes
            StrokeCollection mapStrokes = new StrokeCollection();

            // iterate through each user and model strokes
            int numPoints;
            Stroke userStroke, modelStroke, reverseStroke, mapStroke;
            StylusPoint lastPoint;
            int lastTime;
            double directDistance, reverseDistance;
            StrokeCollection debugStrokes = new StrokeCollection(); // debug
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

                // debug
                debugStrokes.Add(modelStroke);

                // create the reverse stroke and calculate the direct and reverse pairwise stroke distances
                reverseStroke = SketchTools.Reverse(modelStroke);
                directDistance = SketchTools.Distance(userStroke, modelStroke);
                reverseDistance = SketchTools.Distance(userStroke, reverseStroke);

                // reserve the stroke if the reverse pairwise distance is shorter
                if (reverseDistance < directDistance)
                {
                    modelStroke = reverseStroke;
                }

                // create the corresponding map stroke for each pairwise user and model stroke point
                for (int j = 0; j < userStroke.StylusPoints.Count; ++j)
                {
                    mapStroke = new Stroke(new StylusPointCollection() { userStroke.StylusPoints[j], modelStroke.StylusPoints[j] });
                    mapStroke.DrawingAttributes = myMapVisuals;

                    mapStrokes.Add(mapStroke);
                }
            }

            //
            return mapStrokes;
        }

        // debug
        private void DebugStrokes(StrokeCollection strokes)
        {
            string debugFilePath = @"C:\Users\paultaele\Desktop\debug.txt";

            using (StreamWriter file = new StreamWriter(debugFilePath))
            {
                foreach (Stroke stroke in strokes)
                {
                    foreach (StylusPoint points in stroke.StylusPoints)
                    {
                        string line = Math.Round(points.X) + "\t" + Math.Round(points.Y);

                        file.WriteLine(line);
                    }
                }
            }
        }

        // debug
        private void DisplayPoints(StrokeCollection strokes)
        {
            var displayPoints = new List<Stroke>();

            foreach (Stroke stroke in strokes)
            {
                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    Stroke displayPoint
                        = new Stroke(new StylusPointCollection() { new StylusPoint(point.X, point.Y) });
                    displayPoint.DrawingAttributes.Color = Colors.Red;
                    displayPoint.DrawingAttributes.Width = 15;
                    displayPoint.DrawingAttributes.Height = 15;
                    displayPoints.Add(displayPoint);
                }
            }

            foreach (Stroke displayPoint in displayPoints)
            {
                MyCanvas.Strokes.Add(displayPoint);
            }
        }

        #endregion

        #region Fields

        private List<string> myLabels;
        private Dictionary<string, StrokeCollection> myModelsDictionary;
        private Dictionary<string, Image> myImagesDictionary;
        private int myIndexer;
        private int myAnimationIndexer;
        private DrawingAttributes myModelVisuals;
        private DrawingAttributes myMapVisuals;
        private Rectangle myMask;
        private List<Line> myAnimatedStroke;
        private StrokeCollection myMappingStrokes;

        public static readonly string MODELS_DIR = @"C:\Users\paultaele\Documents\GitHub\srl-research\data\katakana\models";
        public static readonly string IMAGES_DIR = @"C:\Users\paultaele\Documents\GitHub\srl-research\data\katakana\images";

        public static readonly Brush ANIMATION_LINE_COLOR = Brushes.DarkBlue;
        public static readonly Color USER_STROKE_COLOR = Colors.Black;
        public static readonly Color MODEL_STROKE_COLOR = Colors.Transparent;
        public static readonly Color MAP_STROKE_COLOR = Colors.Red;
        public static readonly Brush MASK_COLOR = Brushes.White;

        public static readonly double BOX_SIZE = 471.0; // this size is based on the pixel length of the images used from data collection

        public static readonly double ANIMATION_TIME_SPAN = 5.0;
        public static readonly double ANIMATION_LINE_WIDTH = 10.0;
        public static readonly double USER_STROKE_SIZE = 5.0;
        public static readonly double MASK_OPACITY = 0.8;

        public static readonly int RESAMPLE_POINTS = 256;

        #endregion
    }
}
