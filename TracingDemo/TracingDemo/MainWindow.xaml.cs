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
using System.Windows.Media.Imaging;

namespace TracingDemo
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
            ResizeMode = ResizeMode.NoResize;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //
            ResizeControls();

            //
            LoadContent(IMAGES_PATH, MODELS_PATH);

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

        #endregion

        #region Button Interactions

        private void MyCheckButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyBackButton_Click(object sender, RoutedEventArgs e)
        {
            //
            MyCanvas.Children.Remove(myImages[myIndexer]);

            //
            --myIndexer;
            MyNextButton.IsEnabled = true;
            if (myIndexer == 0)
            {
                MyBackButton.IsEnabled = false;
            }

            //
            MyCanvas.Children.Add(myImages[myIndexer]);
        }

        private void MyNextButton_Click(object sender, RoutedEventArgs e)
        {
            //
            MyCanvas.Children.Remove(myImages[myIndexer]);

            //
            ++myIndexer;
            MyBackButton.IsEnabled = true;
            if (myIndexer == myLabels.Count - 1)
            {
                MyNextButton.IsEnabled = false;
            }

            //
            MyCanvas.Children.Add(myImages[myIndexer]);

        }

        private void EnableButtons(bool enable)
        {
            MyCheckButton.IsEnabled = enable;
            MyClearButton.IsEnabled = enable;
            MyUndoButton.IsEnabled = enable;
        }

        #endregion

        #region Helper Methods

        private void ResizeControls()
        {
            // resize canvas
            MyCanvasBorder.Height = MyCanvasBorder.ActualHeight;
            MyCanvasBorder.Width = MyCanvasBorder.ActualHeight;

            // resize instructions
            MyInstructionsBorder.Width = MyCanvasBorder.Width;

            // resize buttons
            MyButtonsBorder.Width = MyCanvasBorder.Width;
            MyBackButton.Width = MyBackButton.Height = MyButtonsBorder.ActualHeight;
            MyClearButton.Width = MyClearButton.Height = MyButtonsBorder.ActualHeight;
            MyUndoButton.Width = MyUndoButton.Height = MyButtonsBorder.ActualHeight;
            MyCheckButton.Width = MyCheckButton.Height = MyButtonsBorder.ActualHeight;
            MyNextButton.Width = MyNextButton.Height = MyButtonsBorder.ActualHeight;
        }

        private void LoadContent(string imagesPath, string modelsPath)
        {
            LoadImages(imagesPath);
            LoadModels(modelsPath);

            //
            if (myLabels.Count > 1)
            {
                MyNextButton.IsEnabled = true;
            }
        }

        private void LoadImages(string imagesPath)
        {
            // 
            List<string> labels = new List<string>();
            List<Image> images = new List<Image>();
            string label;
            Image image;
            foreach (var imagePath in Directory.GetFiles(imagesPath))
            {
                if (imagePath.EndsWith(".png"))
                {
                    // get the label and image
                    label = Path.GetFileNameWithoutExtension(imagePath);
                    image = CreateImage(imagePath);

                    labels.Add(label);
                    images.Add(image);
                }
            }

            //
            myLabels = labels;
            myImages = images;
            myIndexer = 12;

            // load the first image onto the canvas
            //MyCanvas.Children.Add(myImages[myIndexer]);
        }

        private void LoadModels(string modelsPath)
        {
            List<StrokeCollection> models = new List<StrokeCollection>();
            StrokeCollection model;
            SketchXmlProcessor processor = new SketchXmlProcessor();
            foreach (var modelPath in Directory.GetFiles(modelsPath))
            {
                if (modelPath.EndsWith(".xml"))
                {
                    // get the label and model
                    model = processor.Read(modelPath);

                    models.Add(model);
                }
            }

            //
            myModels = models;

            //
            model = SketchTools.Clone(myModels[myIndexer]);
            model = SketchTools.Resample(model, 128);
            model = SketchTools.Scale(model, MyCanvasBorder.Width * 0.6, SketchTools.ScaleType.Proportional);
            model = SketchTools.Translate(model, new StylusPoint(MyCanvasBorder.Width / 2.0, MyCanvasBorder.Height / 2.0), SketchTools.TranslateType.Median);
            foreach (Stroke stroke in model)
            {
                stroke.DrawingAttributes.Color = Colors.Black;
                stroke.DrawingAttributes.Width = 40;
                stroke.DrawingAttributes.Height = 40;
            }
            MyCanvas.Strokes.Add(model);
        }

        private Image CreateImage(string filePath)
        {
            Image image = new Image();
            image.Source = new BitmapImage(new Uri(filePath));
            double imageWidth = image.Source.Width;
            double imageHeight = image.Source.Height;

            double canvasWidth = MyCanvasBorder.Width;
            double canvasHeight = MyCanvasBorder.Height;
            double scaleFactor = ScaleFactor(canvasWidth, canvasHeight, imageWidth, imageHeight);

            image.Width = imageWidth * scaleFactor;
            image.Height = imageHeight * scaleFactor;
            InkCanvas.SetLeft(image, (canvasWidth / 2.0) - (image.Width / 2.0));
            InkCanvas.SetTop(image, (canvasHeight / 2.0) - (image.Height / 2.0));

            return image;
        }

        private double ScaleFactor(double controlWidth, double controlHeight, double imageWidth, double imageHeight)
        {
            double canvasRatio = controlHeight / controlWidth;
            double imageRatio = imageHeight / imageWidth;
            double scaleFactor = canvasRatio <= imageRatio
                ? controlHeight / imageHeight
                : controlWidth / imageWidth;

            return scaleFactor;
        }

        #endregion

        #region Fields

        private List<string> myLabels;
        private List<Image> myImages;
        private List<StrokeCollection> myModels;
        private int myIndexer;

        public static readonly string DATA_PATH = @"C:\Users\pault\Documents\GitHub\srl-research\data\hiragana";
        public static readonly string MODELS_PATH = DATA_PATH + Path.DirectorySeparatorChar + "models" + Path.DirectorySeparatorChar;
        public static readonly string IMAGES_PATH = DATA_PATH + Path.DirectorySeparatorChar + "images" + Path.DirectorySeparatorChar;

        #endregion
    }
}
