using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WinWidget.Views;

public partial class NotesWidgetView : UserControl
{
    public NotesWidgetView() => InitializeComponent();

    public event EventHandler? NoteChanged;
    public event MouseButtonEventHandler? DragRequested;

    public string NoteText
    {
        get => Editor.Text;
        set => Editor.Text = value;
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        Placeholder.Visibility = string.IsNullOrEmpty(Editor.Text) ? Visibility.Visible : Visibility.Collapsed;
        NoteChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDragHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        DragRequested?.Invoke(this, e);
        e.Handled = true;
    }
}
