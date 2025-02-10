// LiveChartWindow.xaml.cs
using ScottPlot;
using System;
using System.Threading;
using System.Windows;
using static ScottPlot.Generate;
using System.Windows.Threading;

namespace WpfLiveChart
{
    public partial class LiveChartWindow : Window
    {
        private double[] dataY;
        private int dataCount = 800;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private const double MIN_SCALE = -1e-9;
        private DispatcherTimer dataTimer;

        public LiveChartWindow()
        {
            InitializeComponent();
            dataCount = 300;
            InitializePlot();
            //StartTestData();
        }

        public void SetTitle(string title)
        {
            wpfPlot1.Plot.Axes.Title.Label.Text= title;
            wpfPlot1.Plot.Axes.Title.Label.FontSize = 32;
        }

        private static string FormatAxisValue(double value)
        {
            double absValue = Math.Abs(value);
            if (absValue < 1e-9)
                return $"{(value * 1e12).ToString("0.0")}pA";
            if (absValue < 1e-6)
                return $"{(value * 1e9).ToString("0.0")}nA";
            if (absValue < 1e-3)
                return $"{(value * 1e6).ToString("0.0")}µA";
            if (absValue < 1)
                return $"{(value * 1e3).ToString("0.0")}mA";
            return $"{value.ToString("0.0")}A";
        }

        private void InitializePlot()
        {
            dataY = new double[dataCount];
            wpfPlot1.Plot.Add.Signal(dataY);
            wpfPlot1.Plot.Title("Real Time Data");
            wpfPlot1.Plot.Axes.Title.Label.FontSize = 32;
            wpfPlot1.Plot.Axes.Bottom.Label.Text = "Point";
            wpfPlot1.Plot.Axes.Left.Label.Text = "Current";

            // Set custom Y axis formatter
            wpfPlot1.Plot.Axes.Left.TickGenerator = myTickGenerator;

            // Set initial axis limits
            wpfPlot1.Plot.Axes.SetLimitsY(MIN_SCALE, Math.Max(MIN_SCALE * 10, 1e-3));

            // Enable auto scale
            wpfPlot1.Plot.Axes.AutoScale();

        }
        // create a custom tick generator using your custom label formatter
        ScottPlot.TickGenerators.NumericAutomatic myTickGenerator = new()
        {
            LabelFormatter = FormatAxisValue
        };

        public void UpdateChart(double newValue)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateChart(newValue));
                return;
            }

            Array.Copy(dataY, 1, dataY, 0, dataY.Length - 1);
            dataY[dataY.Length - 1] = newValue;

            var currentLimits = wpfPlot1.Plot.Axes.GetLimits();
            wpfPlot1.Plot.Axes.AutoScale();
            AxisLimits newLimits = wpfPlot1.Plot.Axes.GetLimits();
            double xMin = newLimits.Left;
            double xMax = newLimits.Right;
            double yMin = newLimits.Bottom;
            double yMax = newLimits.Top;

            // Ensure newYMax is always more positive (or less negative) than MIN_SCALE
            double newYMax = Math.Max(yMax, MIN_SCALE);

            // Always ensure yMax is at least this much larger than yMin
            double minimumRange = Math.Abs(MIN_SCALE);

            if (newYMax <= MIN_SCALE)
            {
                // For negative values, make yMax less negative than MIN_SCALE
                newYMax = MIN_SCALE + minimumRange;
            }

            // Set initial axis limits
            wpfPlot1.Plot.Axes.SetLimitsY(MIN_SCALE, newYMax);
            wpfPlot1.Refresh();
        }
        private void StartTestData()
        {
            // Initialize timer for test data
            dataTimer = new DispatcherTimer();
            dataTimer.Interval = TimeSpan.FromMilliseconds(100); // Update every 50ms

            double time = 0;
            dataTimer.Tick += (s, e) =>
            {
                // Generate a complex waveform with multiple frequencies
                double newValue = 1e-6 * ( // Scale to microamps
                    Math.Sin(2 * Math.PI * time) + // Base frequency
                    0.5 * Math.Sin(4 * Math.PI * time) + // Double frequency
                    0.25 * Math.Sin(8 * Math.PI * time) + // Quadruple frequency
                    0.1 * (new Random().NextDouble() - 0.5) // Add some noise
                );

                UpdateChart(newValue);
                time += 0.1;
            };

            dataTimer.Start();
        }
        protected override void OnClosed(EventArgs e)
        {
            cts.Cancel();
            base.OnClosed(e);
        }
    }
}