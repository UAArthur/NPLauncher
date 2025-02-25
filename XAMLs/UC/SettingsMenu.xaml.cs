using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BPLauncher.XAMLs.UC
{
    public partial class SettingsMenu : UserControl
    {
        private DispatcherTimer? _scrollTimer;
        private double _targetOffset;
        private const double AnimationDuration = 200.0;
        private const double FrameRate = 120.0;
        private const double WheelDivisor = 1.0;
        private DateTime _startTime;
        private double _startOffset;

        public SettingsMenu()
        {
            InitializeComponent();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer) return;

            e.Handled = true;
            var delta = e.Delta / WheelDivisor;
            var newOffset = scrollViewer.VerticalOffset - delta;
            _targetOffset = Math.Max(0, Math.Min(newOffset, scrollViewer.ScrollableHeight));
            StartSmoothScroll(scrollViewer);
        }

        public void SmoothScrollTo(double targetOffset)
        {
            if (ScrollViewerControl == null) return;

            _targetOffset = Math.Max(0, Math.Min(targetOffset, ScrollViewerControl.ScrollableHeight));
            StartSmoothScroll(ScrollViewerControl);
        }

        private void StartSmoothScroll(ScrollViewer scrollViewer)
        {
            _scrollTimer?.Stop();
            _startOffset = scrollViewer.VerticalOffset;
            _startTime = DateTime.Now;
            _scrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / FrameRate)
            };

            _scrollTimer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - _startTime).TotalMilliseconds;
                if (elapsed >= AnimationDuration)
                {
                    scrollViewer.ScrollToVerticalOffset(_targetOffset);
                    _scrollTimer.Stop();
                    return;
                }

                var progress = elapsed / AnimationDuration;
                var easedProgress = Math.Sin(progress * Math.PI / 2);
                var currentOffset = _startOffset + (_targetOffset - _startOffset) * easedProgress;
                scrollViewer.ScrollToVerticalOffset(currentOffset);
            };

            _scrollTimer.Start();
        }
    }
}
