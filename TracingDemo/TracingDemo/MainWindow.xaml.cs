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
            // note: this method call must be first in this method
            ResizeControls();

            //
            myResampleSize = 128;
            myScaleBounds = MyCanvasBorder.Width * 0.6;
            myOrigin = new StylusPoint(MyCanvasBorder.Width / 2.0, MyCanvasBorder.Height / 2.0);
            myScaleType = SketchTools.ScaleType.Proportional;
            myTranslateType = SketchTools.TranslateType.Median;

            //
            LoadContent(MODELS_PATH);

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
            --myIndexer;
            UpdateModel(myIndexer);
            
            //
            MyNextButton.IsEnabled = true;
            if (myIndexer == 0)
            {
                MyBackButton.IsEnabled = false;
            }
        }

        private void MyNextButton_Click(object sender, RoutedEventArgs e)
        {
            //
            ++myIndexer;
            UpdateModel(myIndexer);

            //
            MyBackButton.IsEnabled = true;
            if (myIndexer == myLabels.Count - 1)
            {
                MyNextButton.IsEnabled = false;
            }
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

        private void LoadContent(string modelsPath)
        {
            LoadModels(modelsPath);

            //
            if (myLabels.Count > 1)
            {
                MyNextButton.IsEnabled = true;
            }
        }

        private void LoadModels(string modelsPath)
        {
            // test
            DrawingAttributes da = new DrawingAttributes();
            da.Color = BRUSH_COLOR;
            da.Width = BRUSH_SIZE;
            da.Height = BRUSH_SIZE;

            List<StrokeCollection> models = new List<StrokeCollection>();
            StrokeCollection model;
            SketchXmlProcessor processor = new SketchXmlProcessor();
            List<string> labels = new List<string>();
            string label;
            foreach (var modelPath in Directory.GetFiles(modelsPath))
            {
                if (modelPath.EndsWith(".xml"))
                {
                    // get the label and model
                    label = Path.GetFileNameWithoutExtension(modelPath);
                    model = processor.Read(modelPath);
                    labels.Add(label);
                    models.Add(model);
                }
            }
            myLabels = labels;
            myModels = models;

            //
            myCurrentModel = SketchTools.Normalize(SketchTools.Clone(myModels[myIndexer]), myResampleSize, myScaleBounds, myOrigin, myScaleType, myTranslateType);
            foreach (Stroke stroke in myCurrentModel)
            {
                //stroke.DrawingAttributes.Color = BRUSH_COLOR;
                //stroke.DrawingAttributes.Width = BRUSH_SIZE;
                //stroke.DrawingAttributes.Height = BRUSH_SIZE;
                stroke.DrawingAttributes = da;
            }
            MyCanvas.Strokes.Add(myCurrentModel);
        }

        private void UpdateModel(int index)
        {
            MyCanvas.Strokes.Remove(myCurrentModel);

            myCurrentModel = SketchTools.Normalize(SketchTools.Clone(myModels[index]), myResampleSize, myScaleBounds, myOrigin, myScaleType, myTranslateType);
            foreach (Stroke stroke in myCurrentModel)
            {
                stroke.DrawingAttributes.Color = BRUSH_COLOR;
                stroke.DrawingAttributes.Width = BRUSH_SIZE;
                stroke.DrawingAttributes.Height = BRUSH_SIZE;
            }
            MyCanvas.Strokes.Add(myCurrentModel);
        }

        #endregion

        #region Fields

        private List<string> myLabels;
        private List<StrokeCollection> myModels;
        private int myIndexer;
        private StrokeCollection myCurrentModel;

        private int myResampleSize;
        private double myScaleBounds;
        private StylusPoint myOrigin;
        private SketchTools.ScaleType myScaleType;
        private SketchTools.TranslateType myTranslateType;

        public static readonly string DATA_PATH = @"C:\Users\paultaele\Documents\GitHub\srl-research\data\hiragana";
        public static readonly string MODELS_PATH = DATA_PATH + Path.DirectorySeparatorChar + "models" + Path.DirectorySeparatorChar;
        public static readonly Color BRUSH_COLOR = Colors.Black;
        public static readonly double BRUSH_SIZE = 20;

        #endregion
    }
}
