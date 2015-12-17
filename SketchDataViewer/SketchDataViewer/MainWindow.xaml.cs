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

namespace SketchDataViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            ResizeMode = ResizeMode.NoResize;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HasLoaded = false;

            MyCanvasBorder.Width = MyCanvasBorder.ActualWidth;
            MyButtonsBorder.Width = MyCanvasBorder.ActualWidth;
        }

        private void MyLoadMenu_Click(object sender, RoutedEventArgs e)
        {
            // display the folder browser dialog
            var dialog = new VistaFolderBrowserDialog();
            if (!dialog.ShowDialog().Value)
            {
                return;
            }
            
            // get the images directory path
            Symbols = new List<StrokeCollection>();
            FilePaths = new List<string>();
            var processor = new SketchXmlProcessor();
            foreach (var filePath in Directory.GetFiles(dialog.SelectedPath))
            {
                if (filePath.EndsWith("xml"))
                {
                    Symbols.Add(processor.Read(filePath));
                    FilePaths.Add(filePath);
                }
            }

            //
            if (Symbols.Count == 0)
            {
                return;
            }

            //
            HasLoaded = true;
            Index = 0;
            MyPrevButton.IsEnabled = false;
            MyNextButton.IsEnabled = (Symbols.Count > 1);

            //
            MyLabelBlock.Text = (string)Symbols[Index].GetPropertyData(SketchTools.LABEL_GUID);
            MyFileNameBlock.Text = Path.GetFileName((string)FilePaths[Index]);

            //
            Transform();
        }

        private void Transform()
        {
            //
            if (!HasLoaded)
            {
                return;
            }

            // clone strokes
            StrokeCollection clone = Clone(Symbols[Index]);

            // transform strokes
            if (MyResampleButton.IsChecked.Value)
            {
                clone = SketchTools.Resample(clone, RESAMPLE_SIZE[(int)MyResampleSlider.Value]);
            }
            if (MyScaleButton.IsChecked.Value)
            {
                int scaleSize = SCALE_SIZE[(int)MyScaleSlider.Value];

                if (MyScaleProportionalButton.IsChecked.Value)
                {
                    clone = SketchTools.Scale(clone, scaleSize, SketchTools.ScaleType.Proportional);
                }
                else if (MyScaleSquareButton.IsChecked.Value)
                {
                    clone = SketchTools.Scale(clone, scaleSize, SketchTools.ScaleType.Square);
                }
                else
                {
                    clone = SketchTools.Scale(clone, scaleSize, SketchTools.ScaleType.Hybrid);
                }
            }
            if (MyTranslateButton.IsChecked.Value)
            {
                var center = new StylusPoint(MyCanvas.ActualWidth / 2.0, MyCanvas.ActualHeight / 2.0);

                if (MyTranslateCentroidButton.IsChecked.Value)
                {
                    clone = SketchTools.Translate(clone, center, SketchTools.TranslateType.Centroid);
                }
                else
                {
                    clone = SketchTools.Translate(clone, center, SketchTools.TranslateType.Median);
                }
            }

            // display strokes
            MyCanvas.Strokes.Clear();
            SetStrokeProperties(clone);
            MyCanvas.Strokes.Add(clone);

            if (MyDisplayPointsButton.IsChecked.Value)
            {
                DisplayPoints(clone);
            }
        }

        private void MyResampleButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyScaleButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyTranslateButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyDisplayPointsButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyResampleSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Transform();
        }

        private void MyScaleSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Transform();
        }

        private void SetStrokeProperties(Stroke stroke)
        {
            stroke.DrawingAttributes.Width = MyCanvas.DefaultDrawingAttributes.Width;
            stroke.DrawingAttributes.Height = MyCanvas.DefaultDrawingAttributes.Height;
            stroke.DrawingAttributes.Color = MyCanvas.DefaultDrawingAttributes.Color;
        }

        private void SetStrokeProperties(StrokeCollection strokes)
        {
            foreach (var stroke in strokes)
            {
                SetStrokeProperties(stroke);
            }
        }

        public StrokeCollection Clone(StrokeCollection others)
        {
            // get GUIDs
            Guid labelGuid = SketchTools.LABEL_GUID;
            Guid timesGuid = SketchTools.TIMES_GUID;

            // initialize clone list
            StrokeCollection strokes = new StrokeCollection();
            strokes.AddPropertyData(labelGuid, others.GetPropertyData(labelGuid));

            // iterate through each original strokes
            foreach (Stroke other in others)
            {
                // clone the points
                var points = new StylusPointCollection();
                foreach (StylusPoint point in other.StylusPoints)
                {
                    points.Add(new StylusPoint(point.X, point.Y));
                }

                // clone the times
                var times = new List<int>();
                int[] otherTimes = (int[])other.GetPropertyData(timesGuid);
                foreach (int time in otherTimes)
                {
                    times.Add(time);
                }

                // clone the stroke
                Stroke stroke = new Stroke(points);
                stroke.AddPropertyData(timesGuid, times.ToArray());
                strokes.Add(stroke);
            }

            return strokes;
        }

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

        private void MyPrevButton_Click(object sender, RoutedEventArgs e)
        {
            --Index;
            if (Index == 0)
            {
                MyPrevButton.IsEnabled = false;
            }
            MyNextButton.IsEnabled = true;

            Transform();

            //
            MyLabelBlock.Text = (string)Symbols[Index].GetPropertyData(SketchTools.LABEL_GUID);
            MyFileNameBlock.Text = Path.GetFileName((string)FilePaths[Index]);
        }

        private void MyNextButton_Click(object sender, RoutedEventArgs e)
        {
            ++Index;
            if (Index == Symbols.Count - 1)
            {
                MyNextButton.IsEnabled = false;
            }
            MyPrevButton.IsEnabled = true;

            Transform();

            //
            MyLabelBlock.Text = (string)Symbols[Index].GetPropertyData(SketchTools.LABEL_GUID);
            MyFileNameBlock.Text = Path.GetFileName((string)FilePaths[Index]);
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

        private void MyTranslateMedianButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private void MyTranslateCentroidButton_Click(object sender, RoutedEventArgs e)
        {
            Transform();
        }

        private bool HasLoaded
        {
            get;
            set;
        }

        private int Index
        {
            get;
            set;
        }

        private List<StrokeCollection> Symbols
        {
            get;
            set;
        }

        private List<string> FilePaths
        {
            get;
            set;
        }

        public static readonly int[] RESAMPLE_SIZE = new int[] { 16, 32, 64, 128, 256 };
        public static readonly int[] SCALE_SIZE = new int[] { 300, 400, 500, 600 };
    }
}
