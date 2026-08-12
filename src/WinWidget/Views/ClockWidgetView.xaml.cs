using System.Globalization;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WinWidget.Views;

public partial class ClockWidgetView : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public ClockWidgetView()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => { Refresh(); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Refresh()
    {
        var now = DateTime.Now;
        var culture = CultureInfo.CurrentUICulture;
        DateLabel.Text = now.ToString("dddd, MMMM d", culture);
        TimeLabel.Text = now.ToString("H:mm", culture);
    }
}
