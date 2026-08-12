using System.Globalization;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WinWidget.Views;

public partial class ClockWidgetView : UserControl
{
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
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
        var dateTimeFormat = EnglishCulture.DateTimeFormat;
        var weekday = dateTimeFormat.GetDayName(now.DayOfWeek);
        var standaloneMonth = dateTimeFormat.MonthNames[now.Month - 1];

        // Build the date from standalone names so it never inherits a locale's
        // genitive month form. English title casing also guarantees a capital
        // initial for both the weekday and month.
        DateLabel.Text = $"{ToTitleCase(weekday)}, {ToTitleCase(standaloneMonth)} {now.Day}";
        TimeLabel.Text = now.ToString("H:mm", EnglishCulture);
        DayHintLabel.Text = now.ToString("yyyy", EnglishCulture);
    }

    private static string ToTitleCase(string value) =>
        EnglishCulture.TextInfo.ToTitleCase(value.ToLower(EnglishCulture));
}
