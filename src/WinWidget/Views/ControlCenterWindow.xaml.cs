using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WinWidget.Models;
using WinWidget.Services;

namespace WinWidget.Views;

public partial class ControlCenterWindow : Window
{
    private readonly WidgetWindowManager _manager;
    private bool _allowClose;
    private bool _isRefreshing;
    private bool _isApplyingAppearance;

    public ControlCenterWindow(WidgetWindowManager manager)
    {
        InitializeComponent();
        _manager = manager;
        _manager.WidgetsChanged += OnWidgetsChanged;
        _manager.WidgetSelected += OnWidgetSelected;
        Closing += OnClosing;
        Loaded += (_, _) => RefreshList();
    }

    public void ShowAndActivate()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    public void ClosePermanently()
    {
        _allowClose = true;
        _manager.WidgetsChanged -= OnWidgetsChanged;
        _manager.WidgetSelected -= OnWidgetSelected;
        Close();
    }

    private WidgetListItem? SelectedItem => WidgetList.SelectedItem as WidgetListItem;

    private void RefreshList(string? preferredId = null)
    {
        _isRefreshing = true;
        try
        {
            preferredId ??= SelectedItem?.Id;
            var items = _manager.Widgets.Select(WidgetListItem.From).ToList();
            WidgetList.ItemsSource = items;
            WidgetList.SelectedItem = items.FirstOrDefault(item => item.Id == preferredId) ?? items.FirstOrDefault();
            SnapToGridCheckBox.IsChecked = _manager.SnapToGrid;
            GridSizeComboBox.SelectedItem = GridSizeComboBox.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(item => int.TryParse(item.Tag?.ToString(), out var size) && size == _manager.GridSize);
            UpdateSelection();
        }
        finally { _isRefreshing = false; }
    }

    private void UpdateSelection()
    {
        var selected = SelectedItem;
        var hasSelection = selected is not null;
        AppearancePanel.IsEnabled = hasSelection;
        VisibilityButton.IsEnabled = hasSelection;
        NoSelectionHint.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
        if (selected is null) return;

        VisibilityButton.Content = selected.IsVisible ? "Скрыть" : "Показать";
        AppearancePanel.Title = selected.DisplayName;
        AppearancePanel.SetAppearance(ParseColor(selected.Settings.TextColor, Color.FromRgb(35, 71, 139)),
            ParseColor(selected.Settings.BackgroundColor, Colors.White), selected.Settings.BackgroundOpacity);
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        AddMenu.PlacementTarget = AddButton;
        AddMenu.IsOpen = true;
    }

    private void OnAddWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string value } || !Enum.TryParse<WidgetKind>(value, out var kind)) return;
        var added = _manager.AddWidget(kind);
        RefreshList(added.Id);
        _manager.FocusWidget(added.Id);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing) return;
        UpdateSelection();
        if (SelectedItem is { } item) _manager.SelectWidget(item.Id);
    }

    private void OnVisibilityClick(object sender, RoutedEventArgs e)
    {
        if (SelectedItem is not { } item) return;
        _manager.SetVisibility(item.Id, !item.IsVisible);
        RefreshList(item.Id);
    }

    private void OnFindClick(object sender, RoutedEventArgs e)
    {
        if (SelectedItem is not { } item) return;
        _manager.FocusWidget(item.Id);
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (SelectedItem is not { } item) return;
        var answer = MessageBox.Show($"Удалить «{item.DisplayName}»?", "WinWidget",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes) _manager.RemoveWidget(item.Id);
    }

    private void OnAppearanceChanged(object sender, AppearanceChangedEventArgs e)
    {
        if (_isRefreshing || SelectedItem is not { } item) return;
        item.Settings.TextColor = $"#{e.TextColor.R:X2}{e.TextColor.G:X2}{e.TextColor.B:X2}";
        item.Settings.BackgroundColor = $"#{e.BackgroundColor.R:X2}{e.BackgroundColor.G:X2}{e.BackgroundColor.B:X2}";
        item.Settings.BackgroundOpacity = e.BackgroundOpacity;
        _isApplyingAppearance = true;
        try { _manager.UpdateAppearance(item.Id); }
        finally { _isApplyingAppearance = false; }
    }

    private void OnSnapToGridChanged(object sender, RoutedEventArgs e)
    {
        if (!_isRefreshing) _manager.SnapToGrid = SnapToGridCheckBox.IsChecked == true;
    }

    private void OnGridSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing || GridSizeComboBox.SelectedItem is not ComboBoxItem item ||
            !int.TryParse(item.Tag?.ToString(), out var size)) return;
        _manager.GridSize = size;
    }

    private void OnAlignAllClick(object sender, RoutedEventArgs e) => _manager.AlignAllToGrid();

    private void OnWidgetsChanged(object? sender, EventArgs e)
    {
        if (!_isApplyingAppearance) Dispatcher.Invoke(() => RefreshList());
    }
    private void OnWidgetSelected(WidgetWindow window) => Dispatcher.Invoke(() => RefreshList(window.Settings.Id));

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        Hide();
    }

    private static Color ParseColor(string value, Color fallback)
    {
        try { return (Color)ColorConverter.ConvertFromString(value); }
        catch { return fallback; }
    }

    private sealed record WidgetListItem(string Id, string DisplayName, string KindLabel, bool IsVisible,
        string Status, Brush StatusBackground, Brush StatusForeground, WidgetSettings Settings)
    {
        public static WidgetListItem From(WidgetSettings settings) => new(settings.Id, settings.DisplayName,
            settings.Kind switch { WidgetKind.Clock => "Дата и время", WidgetKind.Calendar => "Календарь", _ => "Текстовая заметка" },
            settings.IsVisible, settings.IsVisible ? "На экране" : "Скрыт",
            new SolidColorBrush(settings.IsVisible ? Color.FromRgb(231, 244, 236) : Color.FromRgb(240, 241, 243)),
            new SolidColorBrush(settings.IsVisible ? Color.FromRgb(45, 109, 70) : Color.FromRgb(99, 103, 110)), settings);
    }
}
