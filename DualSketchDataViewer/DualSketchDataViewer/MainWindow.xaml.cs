using Ookii.Dialogs.Wpf;
using Srl.Tools;
using Srl.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace DualSketchDataViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constructor and Loader

        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            ResizeMode = ResizeMode.NoResize;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            myIsMainLoaded = false;
            myIsOtherLoaded = false;

            double length = MyCanvasBorder.ActualHeight > MyCanvasBorder.ActualWidth
                ? MyCanvasBorder.ActualWidth : MyCanvasBorder.ActualHeight;
            MyCanvasBorder.Width = length;
            MyCanvasBorder.Height = length;
        }

        #endregion

        #region Transformation Controls

        private void MyResampleSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Transform();
        }

        private void MyResampleButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyMappingButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyScaleButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyScaleHybridButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyScaleProportionalButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyScaleSquareButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyScaleSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Transform();
        }

        private void MyTranslateButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyTranslateCentroidButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyTranslateMedianButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }


        private void MyDisplayPointsButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        #endregion

        #region Data Loaders

        private void MyMainLoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!LoadPaths(ref myMainSketches, ref myIsMainLoaded))
            {
                return;
            }

            var items = new List<string>();
            items.AddRange(myMainSketches.Keys);
            MyMainListBox.ItemsSource = items;
            MyMainListBox.SelectedIndex = 0;

            Transform();
        }

        private void MyOtherLoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!LoadPaths(ref myOtherSketches, ref myIsOtherLoaded))
            {
                return;
            }

            var items = new List<string>();
            items.AddRange(myMainSketches.Keys);
            MyOtherListBox.ItemsSource = items;
            MyOtherListBox.SelectedIndex = 0;

            Transform();
        }

        private bool LoadPaths(ref Dictionary<string, StrokeCollection> sketches, ref bool isLoaded)
        {
            // display the folder browser dialog
            var dialog = new VistaFolderBrowserDialog();
            if (!dialog.ShowDialog().Value)
            {
                return false;
            }

            // get the xml directory path
            string relative;
            StrokeCollection sketch;
            var processor = new SketchXmlProcessor();
            sketches = new Dictionary<string, StrokeCollection>();
            foreach (var absolute in Directory.GetFiles(dialog.SelectedPath))
            {
                if (absolute.EndsWith("xml"))
                {
                    relative = Path.GetFileNameWithoutExtension(absolute);
                    sketch = processor.Read(absolute);
                    sketches.Add(relative, sketch);
                }
            }

            //
            if (sketches.Count == 0)
            {
                return false;
            }

            //
            isLoaded = true;

            //
            return true;
        }

        private void MyOtherListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Transform();
        }

        private void MyMainListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Transform();
        }

        #endregion

        private void Transform()
        {
            //
            if (!myIsMainLoaded || !myIsOtherLoaded)
            {
                return;
            }

            //
            StrokeCollection main = SketchTools.Clone(myMainSketches[(string)MyMainListBox.SelectedItem]);
            StrokeCollection other = SketchTools.Clone(myOtherSketches[(string)MyOtherListBox.SelectedItem]);

            // transform strokes
            if (MyResampleButton.IsChecked.Value)
            {
                main = SketchTools.Resample(main, RESAMPLE_SIZE[(int)MyResampleSlider.Value]);
                other = SketchTools.Resample(other, RESAMPLE_SIZE[(int)MyResampleSlider.Value]);
            }
            if (MyScaleButton.IsChecked.Value)
            {
                int scaleSize = SCALE_SIZE[(int)MyScaleSlider.Value];

                if (MyScaleProportionalButton.IsChecked.Value)
                {
                    main = SketchTools.Scale(main, scaleSize, SketchTools.ScaleType.Proportional);
                    other = SketchTools.Scale(other, scaleSize, SketchTools.ScaleType.Proportional);
                }
                else if (MyScaleSquareButton.IsChecked.Value)
                {
                    main = SketchTools.Scale(main, scaleSize, SketchTools.ScaleType.Square);
                    other = SketchTools.Scale(other, scaleSize, SketchTools.ScaleType.Square);
                }
                else
                {
                    main = SketchTools.Scale(main, scaleSize, SketchTools.ScaleType.Hybrid);
                    other = SketchTools.Scale(other, scaleSize, SketchTools.ScaleType.Hybrid);
                }
            }
            if (MyTranslateButton.IsChecked.Value)
            {
                var center = new StylusPoint(MyCanvas.ActualWidth / 2.0, MyCanvas.ActualHeight / 2.0);

                if (MyTranslateCentroidButton.IsChecked.Value)
                {
                    main = SketchTools.Translate(main, center, SketchTools.TranslateType.Centroid);
                    other = SketchTools.Translate(other, center, SketchTools.TranslateType.Centroid);
                }
                else
                {
                    main = SketchTools.Translate(main, center, SketchTools.TranslateType.Median);
                    other = SketchTools.Translate(other, center, SketchTools.TranslateType.Median);
                }
            }

            // display strokes
            MyCanvas.Strokes.Clear();
            SetStrokeProperties(other, SKETCH_BRUSH_SIZE, OTHER_BRUSH_COLOR);
            MyCanvas.Strokes.Add(other);
            SetStrokeProperties(main, SKETCH_BRUSH_SIZE, MAIN_BRUSH_COLOR);
            MyCanvas.Strokes.Add(main);

            if (MyResampleButton.IsChecked.Value && MyMappingButton.IsChecked.Value)
            {
                StrokeCollection mapStrokes = MappingStrokes(main, other);
                MyCanvas.Strokes.Add(mapStrokes);
            }
            if (MyDisplayPointsButton.IsChecked.Value)
            {
                var mainDispayPoints = DisplayPoints(main, POINT_BUSH_SIZE, MAIN_POINT_BRUSH_COLOR);
                var otherDispayPoints = DisplayPoints(other, POINT_BUSH_SIZE, OTHER_POINT_BRUSH_COLOR);

                foreach (var displayPoint in mainDispayPoints)
                {
                    MyCanvas.Strokes.Add(displayPoint);
                }
                foreach (var displayPoint in otherDispayPoints)
                {
                    MyCanvas.Strokes.Add(displayPoint);
                }
            }
        }

        private void SetStrokeProperties(Stroke stroke, double size, Color color)
        {
            stroke.DrawingAttributes.Width = size;
            stroke.DrawingAttributes.Height = size;
            stroke.DrawingAttributes.Color = color;
        }

        private void SetStrokeProperties(StrokeCollection strokes, double size, Color color)
        {
            foreach (var stroke in strokes)
            {
                SetStrokeProperties(stroke, size, color);
            }
        }

        private List<Stroke> DisplayPoints(StrokeCollection strokes, double size, Color color)
        {
            var displayPoints = new List<Stroke>();

            foreach (Stroke stroke in strokes)
            {
                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    Stroke displayPoint
                        = new Stroke(new StylusPointCollection() { new StylusPoint(point.X, point.Y) });
                    SetStrokeProperties(displayPoint, size, color);
                    displayPoints.Add(displayPoint);
                }
            }

            return displayPoints;
        }

        private StrokeCollection MappingStrokes(StrokeCollection main, StrokeCollection other)
        {
            //
            var mainPoints = new StylusPointCollection();
            var otherPoints = new StylusPointCollection();
            foreach (var stroke in main)
            {
                mainPoints.Add(stroke.StylusPoints);
            }
            foreach (var stroke in other)
            {
                otherPoints.Add(stroke.StylusPoints);
            }

            //
            var pairs = new List<Tuple<StylusPoint, StylusPoint>>();
            double minDistance, distance;
            StylusPoint minPoint = otherPoints[0];
            foreach (var mainPoint in mainPoints)
            {
                minDistance = Double.MaxValue;

                foreach (var otherPoint in otherPoints)
                {
                    distance = SketchTools.Distance(mainPoint, otherPoint);

                    if (minDistance > distance)
                    {
                        minDistance = distance;
                        minPoint = otherPoint;
                    }
                }

                pairs.Add(new Tuple<StylusPoint, StylusPoint>(mainPoint, minPoint));
                otherPoints.Remove(minPoint);
            }

            //
            var mapStrokes = new StrokeCollection();
            foreach (var pair in pairs)
            {
                Stroke stroke = new Stroke(new StylusPointCollection() { pair.Item1, pair.Item2 });
                SetStrokeProperties(stroke, MAP_BRUSH_SIZE, MAP_BRUSH_COLOR);
                mapStrokes.Add(stroke);
            }

            return mapStrokes;
        }

        #region Fields

        private Dictionary<string, StrokeCollection> myMainSketches;
        private Dictionary<string, StrokeCollection> myOtherSketches;
        private bool myIsMainLoaded;
        private bool myIsOtherLoaded;

        public static readonly double SKETCH_BRUSH_SIZE = 5.0;
        public static readonly double MAP_BRUSH_SIZE = 1.0;
        public static readonly double POINT_BUSH_SIZE = 3.0;
        public static readonly Color MAIN_BRUSH_COLOR = Colors.Black;
        public static readonly Color OTHER_BRUSH_COLOR = Colors.Blue;
        public static readonly Color MAIN_POINT_BRUSH_COLOR = Colors.Gray;
        public static readonly Color OTHER_POINT_BRUSH_COLOR = Colors.Green;
        public static readonly Color MAP_BRUSH_COLOR = Colors.Red;

        public static readonly int[] RESAMPLE_SIZE = new int[] { 16, 32, 64, 128, 256 };
        public static readonly int[] SCALE_SIZE = new int[] { 300, 400, 500, 600 };

        #endregion
    }
}
