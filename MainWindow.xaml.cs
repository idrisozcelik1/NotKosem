using NotKosem.Models;
using NotKosem.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using DataFormats = System.Windows.DataFormats;
using FontFamily = System.Windows.Media.FontFamily;

namespace NotKosem
{
    public partial class MainWindow : Window
    {
        private const string RichContentPrefix = "RICH1:";

        private readonly ObservableCollection<Note> _notes = new();

        private readonly Dictionary<string, WidgetWindow> _widgetWindows = new();

        private readonly List<string> _defaultCategories = new()
        {
            "Kişisel",
            "İş",
            "Okul",
            "Yazılım",
            "Diğer"
        };

        private Note? _currentNote;

        private bool _isLoadingNote;
        private bool _isExiting;
        private bool _storageReady = true;
        private bool _isPinned;

        private System.Windows.Forms.NotifyIcon? _trayIcon;
        private DispatcherTimer? _reminderTimer;

        public MainWindow()
        {
            InitializeComponent();

            InitializeFontFamilies();
            InitializeTray();

            try
            {
                var loadedNotes = NoteStorageService.LoadNotes();

                if (loadedNotes != null)
                {
                    foreach (Note note in loadedNotes)
                    {
                        if (string.IsNullOrWhiteSpace(note.Category))
                            note.Category = "Diğer";

                        _notes.Add(note);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Notlar yüklenirken hata oluştu:\n" + ex.Message,
                    "NotKöşem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                _storageReady = false;
            }

            NoteList.ItemsSource = _notes;

            RefreshCategorySelector();

            if (_notes.Count == 0)
            {
                CreateNewNote();
            }
            else
            {
                NoteList.SelectedIndex = 0;
            }

            _reminderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15)
            };

            _reminderTimer.Tick += ReminderTimer_Tick;
            _reminderTimer.Start();

            Loaded += MainWindow_Loaded;
        }

        // ----------------------------------------------------
        // UYGULAMA AÇILDIĞINDA WIDGET'LARI YÜKLE
        // ----------------------------------------------------

        private void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            foreach (Note note in _notes.Where(n => n.IsWidgetOpen).ToList())
            {
                ShowWidget(note);
            }
        }

        // ----------------------------------------------------
        // YAZI TİPİ LİSTESİ
        // ----------------------------------------------------

        private void InitializeFontFamilies()
        {
            FontFamilySelector.ItemsSource = Fonts.SystemFontFamilies
                .OrderBy(font => font.Source)
                .ToList();

            FontFamilySelector.SelectedItem =
                Fonts.SystemFontFamilies.FirstOrDefault(
                    font => font.Source.Equals(
                        "Segoe UI",
                        StringComparison.OrdinalIgnoreCase))
                ?? Fonts.SystemFontFamilies.FirstOrDefault();
        }

        // ----------------------------------------------------
        // SİSTEM TEPSİSİ
        // ----------------------------------------------------

        private void InitializeTray()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "NotKöşem",
                Visible = true
            };

            var menu = new System.Windows.Forms.ContextMenuStrip();

            var openItem =
                new System.Windows.Forms.ToolStripMenuItem("NotKöşem'i Aç");

            var startupItem =
                new System.Windows.Forms.ToolStripMenuItem("Windows ile başlat")
                {
                    CheckOnClick = true,
                    Checked = IsStartupEnabled()
                };

            var exitItem =
                new System.Windows.Forms.ToolStripMenuItem("Çıkış");

            openItem.Click += (sender, e) => ShowWindow();

            startupItem.Click += (sender, e) =>
            {
                SetWindowsStartup(startupItem.Checked);
            };

            exitItem.Click += (sender, e) =>
            {
                _isExiting = true;

                _reminderTimer?.Stop();

                foreach (var widget in _widgetWindows.Values.ToList())
                {
                    widget.Close();
                }

                SaveNotes();

                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }

                System.Windows.Application.Current.Shutdown();
            };

            menu.Items.Add(openItem);
            menu.Items.Add(startupItem);
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;

            _trayIcon.DoubleClick += (sender, e) => ShowWindow();
        }

        private bool IsStartupEnabled()
        {
            using var key =
                Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run");

            return key?.GetValue("NotKosem") != null;
        }

        private void SetWindowsStartup(bool enabled)
        {
            try
            {
                using var key =
                    Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Run",
                        true)
                    ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Run");

                if (key == null)
                    throw new Exception(
                        "Windows başlangıç ayarlarına erişilemedi.");

                if (enabled)
                {
                    string? exePath = Environment.ProcessPath;

                    if (string.IsNullOrWhiteSpace(exePath))
                        throw new Exception(
                            "Uygulama dosyasının yolu bulunamadı.");

                    key.SetValue("NotKosem", $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue("NotKosem", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Başlangıç ayarı değiştirilemedi:\n" + ex.Message,
                    "NotKöşem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ShowWindow()
        {
            Dispatcher.Invoke(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
        }

        private void MainWindow_Closing(
    object? sender,
    CancelEventArgs e)
        {
            _isExiting = true;

            _reminderTimer?.Stop();

            foreach (var widget in _widgetWindows.Values.ToList())
            {
                widget.Close();
            }

            SaveNotes();

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        // ----------------------------------------------------
        // NOT OLUŞTURMA VE YÜKLEME
        // ----------------------------------------------------

        private void CreateNewNote()
        {
            Note newNote = new Note
            {
                Title = "Yeni Not",
                Category = "Diğer",
                BackgroundColor = "#FFFFFF",
                Opacity = 1.0
            };

            _notes.Add(newNote);

            NoteList.SelectedItem = newNote;

            LoadNote(newNote);

            SaveNotes();
        }

        private void LoadNote(Note note)
        {
            _isLoadingNote = true;

            try
            {
                _currentNote = note;

                NoteTitleTextBox.Text = note.Title;

                LoadRichTextContent(note.Content);

                MainBorder.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        note.BackgroundColor));

                MainBorder.Opacity = note.Opacity;

                OpacitySlider.Value = note.Opacity;

                OpacityValueText.Text =
                    $"%{Math.Round(note.Opacity * 100)}";

                SelectColor(note.BackgroundColor);

                RefreshCategorySelector(note.Category);

                UpdateReminderStatus();
                UpdateNoteCount();
            }
            finally
            {
                _isLoadingNote = false;
            }
        }

        // ----------------------------------------------------
        // ZENGİN METİN KAYDETME VE YÜKLEME
        // ----------------------------------------------------

        private void LoadRichTextContent(string? content)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(0),
                LineHeight = double.NaN,
                LineStackingStrategy = LineStackingStrategy.MaxHeight
            };

            var listStyle = new Style(typeof(List));
            listStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0)));
            listStyle.Setters.Add(new Setter(Block.PaddingProperty, new Thickness(16, 0, 0, 0)));
            listStyle.Setters.Add(new Setter(List.MarkerOffsetProperty, 6.0));
            doc.Resources.Add(typeof(List), listStyle);

            var listItemStyle = new Style(typeof(ListItem));
            listItemStyle.Setters.Add(new Setter(ListItem.MarginProperty, new Thickness(0)));
            listItemStyle.Setters.Add(new Setter(ListItem.PaddingProperty, new Thickness(0)));
            doc.Resources.Add(typeof(ListItem), listItemStyle);

            NoteRichTextBox.Document = doc;

            if (string.IsNullOrEmpty(content))
                return;

            try
            {
                if (content.StartsWith(
                    RichContentPrefix,
                    StringComparison.Ordinal))
                {
                    string base64 = content.Substring(
                        RichContentPrefix.Length);

                    byte[] data = Convert.FromBase64String(base64);

                    using MemoryStream stream = new MemoryStream(data);

                    TextRange range = new TextRange(
                        NoteRichTextBox.Document.ContentStart,
                        NoteRichTextBox.Document.ContentEnd);

                    range.Load(stream, DataFormats.XamlPackage);

                    NormalizeLists();
                    return;
                }

                NoteRichTextBox.Document.Blocks.Clear();

                NoteRichTextBox.Document.Blocks.Add(
                    new Paragraph(new Run(content)));
            }
            catch
            {
                NoteRichTextBox.Document.Blocks.Clear();

                NoteRichTextBox.Document.Blocks.Add(
                    new Paragraph(
                        new Run(content.StartsWith(
                            RichContentPrefix,
                            StringComparison.Ordinal)
                            ? "Not içeriği okunamadı."
                            : content)));
            }
        }

        private string SaveRichTextContent()
        {
            TextRange range = new TextRange(
                NoteRichTextBox.Document.ContentStart,
                NoteRichTextBox.Document.ContentEnd);

            using MemoryStream stream = new MemoryStream();

            range.Save(stream, DataFormats.XamlPackage);

            return RichContentPrefix +
                   Convert.ToBase64String(stream.ToArray());
        }

        private string GetPlainTextFromContent(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            if (!content.StartsWith(
                RichContentPrefix,
                StringComparison.Ordinal))
            {
                return content;
            }

            try
            {
                byte[] data = Convert.FromBase64String(
                    content.Substring(RichContentPrefix.Length));

                using MemoryStream stream = new MemoryStream(data);

                FlowDocument document = new FlowDocument();

                TextRange range = new TextRange(
                    document.ContentStart,
                    document.ContentEnd);

                range.Load(stream, DataFormats.XamlPackage);

                return range.Text.Trim();
            }
            catch
            {
                return "Hatırlatma zamanı geldi.";
            }
        }

        // ----------------------------------------------------
        // NOT SEÇİMİ VE KAYDI
        // ----------------------------------------------------

        private void NoteList_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (NoteList.SelectedItem is Note selectedNote)
                LoadNote(selectedNote);
        }

        private void NewNoteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CreateNewNote();
        }

        private void NoteTitleTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            _currentNote.Title = NoteTitleTextBox.Text;

            NoteList.Items.Refresh();

            SaveNotes();
        }

        private void NoteRichTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            _currentNote.Content = SaveRichTextContent();

            SaveNotes();
        }

        private void SaveNoteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_currentNote == null)
                return;

            _currentNote.Title = NoteTitleTextBox.Text;
            _currentNote.Content = SaveRichTextContent();

            SaveNotes();

            MessageBox.Show(
                "Not başarıyla kaydedildi.",
                "NotKöşem",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void SaveNotes()
        {
            if (!_storageReady)
                return;

            try
            {
                NoteStorageService.SaveNotes(_notes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Kayıt hatası:\n" + ex.Message,
                    "NotKöşem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateNoteCount()
        {
            int currentIndex = NoteList.SelectedIndex + 1;

            NoteCountText.Text =
                $"Not {Math.Max(0, currentIndex)} / {_notes.Count}";
        }

        // ----------------------------------------------------
        // KATEGORİ İŞLEMLERİ
        // ----------------------------------------------------

        private void RefreshCategorySelector(
            string? selectedCategory = null)
        {
            string currentCategory =
                selectedCategory
                ?? _currentNote?.Category
                ?? "Diğer";

            var categories = new List<string>(_defaultCategories);

            foreach (Note note in _notes)
            {
                if (!string.IsNullOrWhiteSpace(note.Category) &&
                    !categories.Contains(
                        note.Category,
                        StringComparer.OrdinalIgnoreCase))
                {
                    categories.Add(note.Category);
                }
            }

            CategorySelector.ItemsSource = categories;

            CategorySelector.SelectedItem =
                categories.FirstOrDefault(
                    category => category.Equals(
                        currentCategory,
                        StringComparison.OrdinalIgnoreCase))
                ?? "Diğer";
        }

        private void CategorySelector_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            if (CategorySelector.SelectedItem is string category)
            {
                _currentNote.Category = category;
                SaveNotes();
            }
        }

        private void AddCategoryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string category = NewCategoryTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("Lütfen kategori adı gir.");
                return;
            }

            if (category.Length > 30)
            {
                MessageBox.Show(
                    "Kategori adı en fazla 30 karakter olabilir.");
                return;
            }

            bool exists = CategorySelector.Items
                .Cast<string>()
                .Any(item => item.Equals(
                    category,
                    StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                MessageBox.Show("Bu kategori zaten mevcut.");
                return;
            }

            if (_currentNote == null)
                return;

            _currentNote.Category = category;

            RefreshCategorySelector(category);

            NewCategoryTextBox.Clear();

            SaveNotes();
        }

        private void DeleteCategoryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (CategorySelector.SelectedItem is not string category)
                return;

            if (_defaultCategories.Contains(
                category,
                StringComparer.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Varsayılan kategoriler silinemez.",
                    "NotKöşem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"'{category}' kategorisini silmek istiyor musun?\n\n" +
                "Bu kategorideki notlar 'Diğer' kategorisine taşınacak.",
                "Kategori Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            foreach (Note note in _notes)
            {
                if (string.Equals(
                    note.Category,
                    category,
                    StringComparison.OrdinalIgnoreCase))
                {
                    note.Category = "Diğer";
                }
            }

            RefreshCategorySelector("Diğer");

            SaveNotes();
        }

        private void ToggleCategoryPanel_Click(
            object sender,
            RoutedEventArgs e)
        {
            CategoryPanel.Visibility =
                CategoryPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // ----------------------------------------------------
        // GÖRÜNÜM AYARLARI
        // ----------------------------------------------------

        private void ToggleAppearancePanel_Click(
            object sender,
            RoutedEventArgs e)
        {
            AppearancePanel.Visibility =
                AppearancePanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void ColorSelector_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            if (ColorSelector.SelectedItem is ComboBoxItem item &&
                item.Tag is string color)
            {
                _currentNote.BackgroundColor = color;

                MainBorder.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(color));

                SaveNotes();
            }
        }

        private void SelectColor(string color)
        {
            foreach (ComboBoxItem item in ColorSelector.Items)
            {
                if (item.Tag?.ToString() == color)
                {
                    ColorSelector.SelectedItem = item;
                    return;
                }
            }

            ColorSelector.SelectedIndex = 0;
        }

        private void OpacitySlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            double opacity = OpacitySlider.Value;

            MainBorder.Opacity = opacity;

            _currentNote.Opacity = opacity;

            OpacityValueText.Text =
                $"%{Math.Round(opacity * 100)}";

            SaveNotes();
        }

        // ----------------------------------------------------
        // YAZI BİÇİMLENDİRME
        // ----------------------------------------------------

        private void BoldButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            NoteRichTextBox.Focus();

            EditingCommands.ToggleBold.Execute(
                null,
                NoteRichTextBox);

            UpdateFormattingButtons();
        }

        private void ItalicButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteRichTextBox.Focus();

            EditingCommands.ToggleItalic.Execute(
                null,
                NoteRichTextBox);

            UpdateFormattingButtons();
        }

        private void UnderlineButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteRichTextBox.Focus();

            EditingCommands.ToggleUnderline.Execute(
                null,
                NoteRichTextBox);

            UpdateFormattingButtons();
        }

        private void BulletButton_Click(object sender, RoutedEventArgs e)
        {
            NoteRichTextBox.Focus();

            Paragraph? paragraph = NoteRichTextBox.CaretPosition.Paragraph;

            // Eğer satırda To-Do kutusu varsa önce onu temizle
            if (paragraph != null)
            {
                TextRange range = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
                string text = range.Text.TrimEnd('\r', '\n');

                if (text.StartsWith("☐ ") || text.StartsWith("☑ "))
                {
                    range.Text = text.Substring(2);
                    NoteRichTextBox.CaretPosition = paragraph.ContentEnd;
                }
            }

            EditingCommands.ToggleBullets.Execute(null, NoteRichTextBox);
            NormalizeLists();
        }

        private void TodoButton_Click(object sender, RoutedEventArgs e)
        {
            NoteRichTextBox.Focus();

            Paragraph? paragraph = NoteRichTextBox.CaretPosition.Paragraph;
            if (paragraph == null)
                return;

            // Eğer satır madde işaretli liste (ListItem) içindeyse önce madde işaretini kapat
            if (paragraph.Parent is ListItem)
            {
                EditingCommands.ToggleBullets.Execute(null, NoteRichTextBox);
                paragraph = NoteRichTextBox.CaretPosition.Paragraph;
                if (paragraph == null)
                    return;
            }

            TextRange paragraphRange = new TextRange(
                paragraph.ContentStart,
                paragraph.ContentEnd);

            string text = paragraphRange.Text.TrimEnd('\r', '\n');

            if (text.StartsWith("☐ "))
            {
                text = "☑ " + text.Substring(2);
            }
            else if (text.StartsWith("☑ "))
            {
                text = text.Substring(2); // 3. tıklamada kutuyu tamamen kaldırır
            }
            else
            {
                text = "☐ " + text;
            }

            paragraphRange.Text = text;
            NoteRichTextBox.CaretPosition = paragraph.ContentEnd;
        }

        private void NormalizeLists()
        {
            foreach (List list in NoteRichTextBox.Document.Blocks.OfType<List>())
            {
                list.MarkerStyle = TextMarkerStyle.Disc;
                list.MarkerOffset = 6;
                list.Margin = new Thickness(0);
                list.Padding = new Thickness(16, 0, 0, 0);

                foreach (ListItem item in list.ListItems)
                {
                    item.Margin = new Thickness(0);
                    item.Padding = new Thickness(0);

                    foreach (Paragraph p in item.Blocks.OfType<Paragraph>())
                    {
                        p.Margin = new Thickness(0);
                        p.Padding = new Thickness(0);
                    }
                }
            }
        }

        private void NoteRichTextBox_SelectionChanged(
    object sender,
    RoutedEventArgs e)
        {
            UpdateFormattingButtons();
        }

        private void UpdateFormattingButtons()
        {
            if (NoteRichTextBox == null ||
                BoldButton == null ||
                ItalicButton == null ||
                UnderlineButton == null)
                return;

            object bold = NoteRichTextBox.Selection.GetPropertyValue(
                TextElement.FontWeightProperty);

            object italic = NoteRichTextBox.Selection.GetPropertyValue(
                TextElement.FontStyleProperty);

            object underline = NoteRichTextBox.Selection.GetPropertyValue(
                Inline.TextDecorationsProperty);

            SetFormattingButton(
                BoldButton,
                bold is FontWeight fw && fw == FontWeights.Bold);

            SetFormattingButton(
                ItalicButton,
                italic is System.Windows.FontStyle fs && fs == FontStyles.Italic);

            bool isUnderline = false;

            if (underline is TextDecorationCollection decorations)
            {
                isUnderline = decorations.Count > 0;
            }

            SetFormattingButton(
                UnderlineButton,
                isUnderline);
        }

        private void SetFormattingButton(
            Button button,
            bool isActive)
        {
            button.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    isActive ? "#D6C49D" : "#FFFDF8"));

            button.BorderBrush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    isActive ? "#A9956C" : "#D8CBAA"));

            button.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    isActive ? "#302719" : "#5C5132"));
        }

        private void FontFamilySelector_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoadingNote ||
                NoteRichTextBox == null ||
                FontFamilySelector.SelectedItem is not FontFamily font)
                return;

            NoteRichTextBox.Focus();

            NoteRichTextBox.Selection.ApplyPropertyValue(
                TextElement.FontFamilyProperty,
                font);
        }

        private void FontSizeSelector_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoadingNote ||
                NoteRichTextBox == null ||
                FontSizeSelector.SelectedItem is not ComboBoxItem item)
                return;

            if (!double.TryParse(
                item.Content?.ToString(),
                CultureInfo.InvariantCulture,
                out double fontSize))
                return;

            NoteRichTextBox.Focus();

            NoteRichTextBox.Selection.ApplyPropertyValue(
                TextElement.FontSizeProperty,
                fontSize);
        }

        private void TextColorSelector_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoadingNote ||
                NoteRichTextBox == null ||
                TextColorSelector.SelectedItem is not ComboBoxItem item)
                return;

            if (item.Tag is not string color)
                return;

            NoteRichTextBox.Focus();

            NoteRichTextBox.Selection.ApplyPropertyValue(
                TextElement.ForegroundProperty,
                new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(color)));
        }

        // ----------------------------------------------------
        // HATIRLATICI
        // ----------------------------------------------------

        private void ToggleReminderPanel_Click(
            object sender,
            RoutedEventArgs e)
        {
            ReminderPanel.Visibility =
                ReminderPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateReminderStatus()
        {
            if (_currentNote?.ReminderAt is DateTime reminder)
            {
                ReminderDatePicker.SelectedDate = reminder.Date;

                ReminderTimeTextBox.Text =
                    reminder.ToString("HH:mm");

                ReminderStatusText.Text =
                    "Hatırlatıcı: " +
                    reminder.ToString("dd.MM.yyyy HH:mm");
            }
            else
            {
                ReminderDatePicker.SelectedDate = null;

                ReminderTimeTextBox.Text = "09:00";

                ReminderStatusText.Text =
                    "Hatırlatıcı ayarlanmadı.";
            }
        }

        private void SetReminderButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_currentNote == null)
                return;

            if (ReminderDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Lütfen bir tarih seç.");
                return;
            }

            if (!TimeSpan.TryParseExact(
                ReminderTimeTextBox.Text.Trim(),
                @"hh\:mm",
                CultureInfo.InvariantCulture,
                out TimeSpan time))
            {
                MessageBox.Show(
                    "Saat formatı hatalı. Örnek: 14:30");

                return;
            }

            DateTime reminder =
                ReminderDatePicker.SelectedDate.Value.Date.Add(time);

            if (reminder <= DateTime.Now)
            {
                MessageBox.Show(
                    "Hatırlatıcı gelecekte bir tarih ve saat olmalı.");

                return;
            }

            _currentNote.ReminderAt = reminder;

            UpdateReminderStatus();

            SaveNotes();

            MessageBox.Show(
                "Hatırlatıcı başarıyla oluşturuldu.",
                "NotKöşem",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CancelReminderButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_currentNote == null)
                return;

            _currentNote.ReminderAt = null;

            UpdateReminderStatus();

            SaveNotes();
        }

        private void ReminderTimer_Tick(
            object? sender,
            EventArgs e)
        {
            DateTime now = DateTime.Now;

            foreach (Note note in _notes.ToList())
            {
                if (note.ReminderAt.HasValue &&
                    note.ReminderAt.Value <= now)
                {
                    ShowReminderNotification(note);

                    note.ReminderAt = null;

                    SaveNotes();
                }
            }

            if (_currentNote != null)
                UpdateReminderStatus();
        }

        private void ShowReminderNotification(Note note)
        {
            if (_trayIcon == null)
                return;

            string title = string.IsNullOrWhiteSpace(note.Title)
                ? "NotKöşem"
                : note.Title;

            string message = GetPlainTextFromContent(note.Content);

            if (string.IsNullOrWhiteSpace(message))
                message = "Hatırlatma zamanı geldi.";

            if (message.Length > 180)
                message = message.Substring(0, 180) + "...";

            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = message;
            _trayIcon.BalloonTipIcon =
                System.Windows.Forms.ToolTipIcon.Info;

            _trayIcon.ShowBalloonTip(5000);
        }

        // ----------------------------------------------------
        // WIDGET İŞLEMLERİ
        // ----------------------------------------------------

        private void OpenWidgetButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_currentNote == null)
                return;

            ShowWidget(_currentNote);
        }

        private void ShowWidget(Note note)
        {
            if (_widgetWindows.TryGetValue(
                note.Id,
                out WidgetWindow? existing))
            {
                if (existing.IsVisible)
                {
                    existing.Activate();
                    return;
                }

                _widgetWindows.Remove(note.Id);
            }

            note.IsWidgetOpen = true;

            WidgetWindow widget = new WidgetWindow(
                note,
                SaveNotes,
                OnWidgetClosed);

            _widgetWindows[note.Id] = widget;

            SaveNotes();

            widget.Show();
        }

        private void OnWidgetClosed(string noteId)
        {
            _widgetWindows.Remove(noteId);
        }

        // ----------------------------------------------------
        // NOT SİLME
        // ----------------------------------------------------

        private void DeleteNoteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_currentNote == null)
                return;

            Note noteToDelete = _currentNote;

            MessageBoxResult result = MessageBox.Show(
                "Bu notu silmek istediğine emin misin?",
                "Not Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            int deletedIndex = NoteList.SelectedIndex;

            if (_widgetWindows.TryGetValue(
                noteToDelete.Id,
                out WidgetWindow? widget))
            {
                noteToDelete.IsWidgetOpen = false;
                widget.Close();
            }

            _notes.Remove(noteToDelete);

            SaveNotes();

            if (_notes.Count == 0)
            {
                CreateNewNote();
                return;
            }

            NoteList.SelectedIndex =
                Math.Min(deletedIndex, _notes.Count - 1);
        }

        // ----------------------------------------------------
        // PIN VE PENCERE KONTROLLERİ
        // ----------------------------------------------------

        private void PinButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _isPinned = !_isPinned;

            Topmost = _isPinned;

            PinButton.Content = _isPinned ? "📍" : "📌";

            PinButton.ToolTip = _isPinned
                ? "Sabitlemeyi kaldır"
                : "Pencereyi sabitle";
        }

        private void MinimizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed &&
                e.OriginalSource is not Button)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            Close();
        }
    }
}
