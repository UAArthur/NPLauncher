using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BPLauncher.XAMLs.UC;

public partial class SettingsMenu : UserControl
{
    private DispatcherTimer? _scrollTimer;
    private double _currentOffset;
    private double _targetOffset;

    public SettingsMenu()
    {
        InitializeComponent();
    }

    public void SmoothScroll(double targetOffset)
    {
        if (ScrollViewerControl == null) return;

        _currentOffset = ScrollViewerControl.VerticalOffset;
        _targetOffset = targetOffset;

        if (_scrollTimer != null)
        {
            _scrollTimer.Stop();
            _scrollTimer = null;
        }

        _scrollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };

        _scrollTimer.Tick += (s, e) =>
        {
            _currentOffset = (_currentOffset * 0.8) + (_targetOffset * 0.2);

            if (Math.Abs(_currentOffset - _targetOffset) < 0.5)
            {
                _currentOffset = _targetOffset;
                _scrollTimer.Stop();
            }

            ScrollViewerControl.ScrollToVerticalOffset(_currentOffset);
        };

        _scrollTimer.Start();
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            double newOffset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
            SmoothScroll(newOffset);
            e.Handled = true; 
        }
    }
}