using System.Windows;
using System.Windows.Controls;

namespace WinWidget.Views;

public partial class NotesWidgetView : UserControl
{
    public NotesWidgetView() => InitializeComponent();

    public event EventHandler? NoteChanged;

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
}
