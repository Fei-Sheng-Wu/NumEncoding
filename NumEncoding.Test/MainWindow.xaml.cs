using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NumEncoding.Test
{
    public partial class MainWindow : Window
    {
        //Rate of data recording in milliseconds
        private const int DATA_STEP = 15;

        //Drawing data
        private readonly List<DrawingData> drawingData = [];
        private readonly List<byte> encodedData = [];
        private readonly DataStructure dataStructure = DataStructure.CreateFromClass<DrawingData>();

        private bool isRecording;
        private Point? drawingPosition;
        private double drawingThickness;

        public MainWindow()
        {
            InitializeComponent();

            isRecording = false;

            //Record drawing data
            new DispatcherTimer(TimeSpan.FromMilliseconds(DATA_STEP), DispatcherPriority.Normal, (sender, e) =>
            {
                if (!isRecording)
                {
                    return;
                }
                else if (!drawingPosition.HasValue)
                {
                    drawingPosition = Mouse.GetPosition(recordCanvas);
                    drawingData.Add(new(drawingPosition.Value.X, drawingPosition.Value.Y, (byte)drawingThickness));
                    return;
                }

                drawingThickness = Math.Clamp(drawingThickness +
                    (Mouse.LeftButton == MouseButtonState.Pressed ? 0.1 : -0.1), 1, byte.MaxValue / 2);

                Point newPosition = Mouse.GetPosition(recordCanvas);
                recordCanvas.Children.Add(new Line()
                {
                    X1 = drawingPosition.Value.X,
                    Y1 = drawingPosition.Value.Y,
                    X2 = newPosition.X,
                    Y2 = newPosition.Y,
                    Stroke = Brushes.Red,
                    StrokeThickness = drawingThickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                });
                drawingPosition = newPosition;
                drawingData.Add(new(drawingPosition.Value.X, drawingPosition.Value.Y, (byte)drawingThickness));
            }, Dispatcher).Start();
        }

        private void Record(object sender, RoutedEventArgs e)
        {
            isRecording = !isRecording;
            if (isRecording)
            {
                //Clear previous data
                drawingData.Clear();
                drawingThickness = 1;
                drawingPosition = null;

                recordCanvas.Children.Clear();
                dataText.Text = string.Empty;
            }
            else
            {
                //Encode recorded data
                encodedData.Clear();
                new BytesEncoder(dataStructure, encodedData)
                    .WriteAllWithHeading(drawingData.Select(x => dataStructure.CastData(x)));

                dataText.Text = string.Concat(encodedData.Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
            }

            recordButton.Content = isRecording ? "Save" : "Record";
            replayButton.IsEnabled = !isRecording;
        }

        private async void Replay(object sender, RoutedEventArgs e)
        {
            recordButton.IsEnabled = false;
            replayButton.IsEnabled = false;
            replayCanvas.Children.Clear();

            //Decode recorded data
            Point? lastPoint = null;
            foreach (DataEntry data in new BytesDecoder(dataStructure, encodedData).ReadAllWithHeading())
            {
                DrawingData drawingData = data.CastData<DrawingData>(dataStructure);
                if (lastPoint.HasValue)
                {
                    replayCanvas.Children.Add(new Line()
                    {
                        X1 = lastPoint.Value.X,
                        Y1 = lastPoint.Value.Y,
                        X2 = drawingData.X,
                        Y2 = drawingData.Y,
                        Stroke = Brushes.Red,
                        StrokeThickness = drawingData.Thickness,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    });
                }

                lastPoint = new(drawingData.X, drawingData.Y);
                await Task.Delay(DATA_STEP);
            }

            recordButton.IsEnabled = true;
            replayButton.IsEnabled = true;
        }

        private void DataTextUpdate(object sender, TextChangedEventArgs e)
        {
            //Calculate the size of encoded data
            int byteLength = dataText.Text.Length / 8;
            if (byteLength > 1024)
            {
                dataSizeText.Text = $"{byteLength / 1024.0:0.00} kilobytes";
            }
            else
            {
                dataSizeText.Text = $"{byteLength} bytes";
            }
        }
    }
}