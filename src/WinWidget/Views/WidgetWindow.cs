using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WinWidget.Models;
using WinWidget.Services;

namespace WinWidget.Views;

public sealed class WidgetWindow : Window
{
    private readonly Border _surface;
    private readonly bool _usesDedicatedDragHandle;
    private bool _allowClose;

    public WidgetSettings Settings { get; }
    public bool SnapToGrid { get; set; }
    public double GridSize { get; set; } = 16;
    public event EventHandler? Selected;
    public event EventHandler? GeometryChanged;

    public WidgetWindow(WidgetSettings settings, UserControl content)
    {
        Settings = settings;
        Title = $"WinWidget — {settings.Kind}";
        Width = settings.Width;
        Height = settings.Height;
        Left = settings.Left;
        Top = settings.Top;
        Topmost = settings.IsAlwaysOnTop;

        WindowBehaviorService.ConfigureWidgetWindow(this);
        _surface = new Border
        {
            // The weather card is intentionally only two thirds as tall as the
            // calendar, so retain the shared horizontal inset while giving its
            // compact three-row layout enough vertical room.
            Padding = settings.Kind switch
            {
                WidgetKind.Weather => new Thickness(22, 12, 22, 12),
                WidgetKind.Image => new Thickness(0),
                _ => new Thickness(22)
            },
            CornerRadius = new CornerRadius(32),
            ClipToBounds = true,
            BorderBrush = new SolidColorBrush(Color.FromArgb(82, 169, 196, 245)),
            BorderThickness = new Thickness(1),
            Child = content
        };
        Content = _surface;
        if (content is NotesWidgetView notes)
        {
            _usesDedicatedDragHandle = true;
            notes.DragRequested += OnNotesDragRequested;
        }
        ApplyAppearance();

        PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseRightButtonUp += (_, _) => Selected?.Invoke(this, EventArgs.Empty);
        LocationChanged += (_, _) => CaptureGeometry();
        SizeChanged += (_, _) =>
        {
            UpdateSurfaceClip();
            CaptureGeometry();
        };
        Closing += OnClosing;
        Loaded += (_, _) => ApplyInteractionState();
    }

    public void ApplyAppearance()
    {
        if (_surface.Child is ImageWidgetView imageView) imageView.RefreshImage();
        TextElement.SetForeground(_surface, new SolidColorBrush(ParseColor(Settings.TextColor, Color.FromRgb(35, 71, 139))));
        var background = ParseColor(Settings.BackgroundColor, Colors.White);
        var backgroundOpacity = Math.Clamp(Settings.BackgroundOpacity, 0, 1);
        background.A = (byte)Math.Round(255 * backgroundOpacity);
        _surface.Background = new SolidColorBrush(background);
        _surface.BorderBrush = new SolidColorBrush(Color.FromArgb(
            (byte)Math.Round(82 * backgroundOpacity), 169, 196, 245));
        Topmost = Settings.IsAlwaysOnTop;
        ApplyInteractionState();
    }

    public void ApplyInteractionState() =>
        WindowBehaviorService.SetClickThrough(this, Settings.IsLocked);

    public void ClosePermanently()
    {
        _allowClose = true;
        Close();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_usesDedicatedDragHandle || Settings.IsLocked || e.ChangedButton != MouseButton.Left ||
            IsTextEditingTarget(e.OriginalSource as DependencyObject))
            return;

        DragWindow();
    }

    private void OnNotesDragRequested(object sender, MouseButtonEventArgs e)
    {
        if (!Settings.IsLocked) DragWindow();
    }

    private void DragWindow()
    {
        try
        {
            DragMove();
            AlignToGrid();
            CaptureGeometry();
            Selected?.Invoke(this, EventArgs.Empty);
        }
        catch (InvalidOperationException)
        {
            // The button may have been released before Windows began the native drag loop.
        }
    }

    public void AlignToGrid(bool force = false)
    {
        if ((!SnapToGrid && !force) || !double.IsFinite(GridSize) || GridSize < 1) return;
        Left = Math.Round(Left / GridSize) * GridSize;
        Top = Math.Round(Top / GridSize) * GridSize;
        CaptureGeometry();
    }

    private static bool IsTextEditingTarget(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is TextBoxBase) return true;
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private void CaptureGeometry()
    {
        if (WindowState != WindowState.Normal) return;
        Settings.Left = Left;
        Settings.Top = Top;
        Settings.Width = ActualWidth;
        Settings.Height = ActualHeight;
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateSurfaceClip()
    {
        if (Settings.Kind != WidgetKind.Image || ActualWidth <= 0 || ActualHeight <= 0) return;
        _surface.Clip = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), 32, 32);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        CaptureGeometry();
        if (_allowClose) return;
        e.Cancel = true;
        Hide();
    }

    private static Color ParseColor(string value, Color fallback)
    {
        try { return (Color)ColorConverter.ConvertFromString(value); }
        catch (FormatException) { return fallback; }
        catch (NotSupportedException) { return fallback; }
    }
}
