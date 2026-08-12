using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace WinWidget.Views;

public partial class CalendarWidgetView : UserControl
{
    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    private DateTime _renderedDate;

    public CalendarWidgetView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            BuildCalendar();
            _refreshTimer.Start();
        };
        Unloaded += (_, _) => _refreshTimer.Stop();
        _refreshTimer.Tick += (_, _) =>
        {
            if (_renderedDate != DateTime.Today) BuildCalendar();
        };
    }

    private void BuildCalendar()
    {
        var today = DateTime.Today;
        _renderedDate = today;
        var culture = CultureInfo.CurrentUICulture;
        MonthLabel.Text = today.ToString("MMMM", culture).ToUpper(culture);
        WeekdayGrid.Children.Clear();
        DaysGrid.Children.Clear();

        var firstDay = culture.DateTimeFormat.FirstDayOfWeek;
        for (var i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)(((int)firstDay + i) % 7);
            WeekdayGrid.Children.Add(CreateText(culture.DateTimeFormat.GetAbbreviatedDayName(day)[..1].ToUpper(culture), 11, 0.62));
        }

        var first = new DateTime(today.Year, today.Month, 1);
        var offset = ((int)first.DayOfWeek - (int)firstDay + 7) % 7;
        for (var cell = 0; cell < 42; cell++)
        {
            var date = first.AddDays(cell - offset);
            var label = CreateText(date.Day.ToString(culture), 13, date.Month == today.Month ? 0.94 : 0.25);
            var host = new Border { Width = 27, Height = 27, CornerRadius = new CornerRadius(14), Child = label };
            if (date == today)
            {
                host.Background = new SolidColorBrush(Color.FromArgb(58, 255, 255, 255));
                host.BorderBrush = new SolidColorBrush(Color.FromArgb(88, 255, 255, 255));
                host.BorderThickness = new Thickness(1);
                label.FontWeight = FontWeights.Bold;
                label.Opacity = 1;
            }
            DaysGrid.Children.Add(host);
        }
    }

    private static TextBlock CreateText(string value, double size, double opacity)
    {
        var text = new TextBlock { Text = value, FontSize = size, FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = opacity };
        return text;
    }
}
