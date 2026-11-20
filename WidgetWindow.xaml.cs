using NotKosem.Models;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using DataFormats = System.Windows.DataFormats;
using TextBox = System.Windows.Controls.TextBox;

namespace NotKosem
{
    public partial class WidgetWindow : Window
    {
        private const string RichContentPrefix = "RICH1:";

        private readonly Note _note;
        private readonly Action _saveNotes;
        private readonly Action<string> _onClosed;

        private bool _isLoading;

        public string NoteId => _note.Id;

        public WidgetWindow(
            Note note,
            Action saveNotes,
            Action<string> onClosed)
        {
            InitializeComponent();

            _note = note;
            _saveNotes = saveNotes;
            _onClosed = onClosed;

            _isLoading = true;

            WidgetTitleTextBox.Text = _note.Title;

            LoadContent();

            WidgetBorder.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    _note.BackgroundColor));

            Opacity = _note.Opacity;

            Left = _note.WidgetLeft;
            Top = _note.WidgetTop;

            Width = Math.Max(220, _note.WidgetWidth);
            Height = Math.Max(160, _note.WidgetHeight);

            Topmost = _note.IsWidgetPinned;

            WidgetPinButton.Content =
                Topmost ? "📍" : "📌";

            _isLoading = false;

            LocationChanged += WidgetWindow_LocationChanged;
            SizeChanged += WidgetWindow_SizeChanged;
            Closed += WidgetWindow_Closed;
        }

        private void LoadContent()
        {
            WidgetRichTextBox.Document = new FlowDocument
            {
                PagePadding = new Thickness(0)
            };

            if (string.IsNullOrEmpty(_note.Content))
                return;

            try
            {
                if (_note.Content.StartsWith(
                    RichContentPrefix,
                    StringComparison.Ordinal))
                {
                    byte[] data = Convert.FromBase64String(
                        _note.Content.Substring(RichContentPrefix.Length));

                    using MemoryStream stream = new MemoryStream(data);

                    TextRange range = new TextRange(
                        WidgetRichTextBox.Document.ContentStart,
                        WidgetRichTextBox.Document.ContentEnd);

                    range.Load(stream, DataFormats.XamlPackage);
                }
                else
                {
                    WidgetRichTextBox.Document.Blocks.Clear();

                    WidgetRichTextBox.Document.Blocks.Add(
                        new Paragraph(new Run(_note.Content)));
                }
            }
            catch
            {
                WidgetRichTextBox.Document.Blocks.Clear();

                WidgetRichTextBox.Document.Blocks.Add(
                    new Paragraph(new Run("Not içeriği okunamadı.")));
            }
        }

        private string GetRichContent()
        {
            TextRange range = new TextRange(
                WidgetRichTextBox.Document.ContentStart,
                WidgetRichTextBox.Document.ContentEnd);

            using MemoryStream stream = new MemoryStream();

            range.Save(stream, DataFormats.XamlPackage);

            return RichContentPrefix +
                   Convert.ToBase64String(stream.ToArray());
        }

        private void WidgetTitleTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_isLoading)
                return;

            _note.Title = WidgetTitleTextBox.Text;

            _saveNotes();
        }

        private void WidgetRichTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_isLoading || _note == null)
                return;

            _note.Content = GetRichContent();

            _saveNotes();
        }

        private void WidgetWindow_LocationChanged(
            object? sender,
            EventArgs e)
        {
            if (_isLoading || _note == null)
                return;

            _note.WidgetLeft = Left;
            _note.WidgetTop = Top;

            _saveNotes();
        }

        private void WidgetWindow_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            if (_isLoading || _note == null)
                return;

            _note.WidgetWidth = Width;
            _note.WidgetHeight = Height;

            _saveNotes();
        }

        private void WidgetPinButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Topmost = !Topmost;

            _note.IsWidgetPinned = Topmost;

            WidgetPinButton.Content =
                Topmost ? "📍" : "📌";

            WidgetPinButton.ToolTip = Topmost
                ? "Sabitlemeyi kaldır"
                : "Widget'i sabitle";

            _saveNotes();
        }

        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed &&
                e.OriginalSource is not Button &&
                e.OriginalSource is not TextBox)
            {
                DragMove();
            }
        }

        private void CloseWidgetButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _note.IsWidgetOpen = false;

            _saveNotes();

            Close();
        }

        private void WidgetWindow_Closed(
            object? sender,
            EventArgs e)
        {
            _onClosed(_note.Id);
        }
    }
}
