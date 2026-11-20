using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NotKosem.Models;
using NotKosem.Services;

namespace NotKosem
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Note> _notes = new();

        private ObservableCollection<string> _categories = new();

        private Note? _currentNote;

        private bool _isLoadingNote = false;
        private bool _isExiting = false;
        private bool _storageReady = true;

        private System.Windows.Forms.NotifyIcon? _trayIcon;
        private DispatcherTimer? _reminderTimer;

        public MainWindow()
        {
            InitializeComponent();

            InitializeTray();

            try
            {
                _notes = NoteStorageService.LoadNotes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Notlar yüklenirken hata oluştu:\n" + ex.Message);

                _notes = new ObservableCollection<Note>();
                _storageReady = false;
            }

            NoteList.ItemsSource = _notes;

            InitializeCategories();

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
        }

        private void InitializeTray()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "NotKöşem",
                Visible = true
            };

            var menu = new System.Windows.Forms.ContextMenuStrip();

            var openItem = new System.Windows.Forms.ToolStripMenuItem(
                "NotKöşem'i Aç");

            var startupItem = new System.Windows.Forms.ToolStripMenuItem(
                "Windows ile başlat")
            {
                CheckOnClick = true,
                Checked = IsStartupEnabled()
            };

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Çıkış");

            openItem.Click += (sender, e) => ShowWindow();

            startupItem.Click += (sender, e) =>
            {
                SetWindowsStartup(startupItem.Checked);
            };

            exitItem.Click += (sender, e) =>
            {
                _isExiting = true;
                _reminderTimer?.Stop();

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
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run");

            return key?.GetValue("NotKosem") != null;
        }

        private void SetWindowsStartup(bool enabled)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true)
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
            if (!_isExiting)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                _reminderTimer?.Stop();

                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
            }
        }

        // KATEGORİ İŞLEMLERİ

        private void InitializeCategories()
        {
            _categories.Clear();

            _categories.Add("Kişisel");
            _categories.Add("İş");
            _categories.Add("Okul");
            _categories.Add("Yazılım");
            _categories.Add("Diğer");

            foreach (Note note in _notes)
            {
                if (string.IsNullOrWhiteSpace(note.Category))
                    note.Category = "Diğer";

                if (!_categories.Contains(note.Category))
                    _categories.Add(note.Category);
            }

            CategorySelector.ItemsSource = _categories;
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

            if (_categories.Any(
                x => string.Equals(
                    x,
                    category,
                    StringComparison.CurrentCultureIgnoreCase)))
            {
                MessageBox.Show("Bu kategori zaten mevcut.");
                return;
            }

            _categories.Add(category);

            if (_currentNote != null)
            {
                _currentNote.Category = category;
                CategorySelector.SelectedItem = category;
                SaveNotes();
            }

            NewCategoryTextBox.Clear();
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

        // NOT OLUŞTURMA VE YÜKLEME

        private void CreateNewNote()
        {
            Note newNote = new Note
            {
                Category = "Diğer"
            };

            _notes.Add(newNote);

            if (!_categories.Contains("Diğer"))
                _categories.Add("Diğer");

            NoteList.SelectedItem = newNote;

            LoadNote(newNote);

            SaveNotes();
        }

        private void LoadNote(Note note)
        {
            _isLoadingNote = true;

            _currentNote = note;

            NoteTitleTextBox.Text = note.Title;
            NoteTextBox.Text = note.Content;

            MainBorder.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    note.BackgroundColor));

            MainBorder.Opacity = note.Opacity;

            OpacitySlider.Value = note.Opacity;

            OpacityValueText.Text =
                $"%{Math.Round(note.Opacity * 100)}";

            SelectColor(note.BackgroundColor);

            if (!_categories.Contains(note.Category))
                _categories.Add(note.Category);

            CategorySelector.SelectedItem = note.Category;

            UpdateReminderStatus();
            UpdateNoteCount();

            CloseSettingsPanels();

            _isLoadingNote = false;
        }

        // AYAR PANELLERİ

        private void CloseSettingsPanels()
        {
            SettingsPanel.Visibility = Visibility.Collapsed;

            ColorSettingsPanel.Visibility = Visibility.Collapsed;
            OpacitySettingsPanel.Visibility = Visibility.Collapsed;
            ReminderSettingsPanel.Visibility = Visibility.Collapsed;
            CategorySettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void OpenSettingsPanel(StackPanel selectedPanel)
        {
            bool alreadyOpen =
                SettingsPanel.Visibility == Visibility.Visible &&
                selectedPanel.Visibility == Visibility.Visible;

            CloseSettingsPanels();

            if (!alreadyOpen)
            {
                SettingsPanel.Visibility = Visibility.Visible;
                selectedPanel.Visibility = Visibility.Visible;
            }
        }

        private void ColorSettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenSettingsPanel(ColorSettingsPanel);
        }

        private void OpacitySettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenSettingsPanel(OpacitySettingsPanel);
        }

        private void ReminderSettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenSettingsPanel(ReminderSettingsPanel);
        }

        private void CategorySettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenSettingsPanel(CategorySettingsPanel);
        }

        // RENK İŞLEMLERİ

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

        private void ColorSelector_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            if (ColorSelector.SelectedItem is ComboBoxItem item)
            {
                string? color = item.Tag?.ToString();

                if (color == null)
                    return;

                _currentNote.BackgroundColor = color;

                MainBorder.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(color));

                SaveNotes();
            }
        }

        // SAYDAMLIK İŞLEMLERİ

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

        private void ResetOpacityButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_currentNote == null)
                return;

            OpacitySlider.Value = 1.0;
            _currentNote.Opacity = 1.0;
            MainBorder.Opacity = 1.0;

            OpacityValueText.Text = "%100";

            SaveNotes();
        }

        // HATIRLATICI İŞLEMLERİ

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

            foreach (Note note in _notes)
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

            string message = string.IsNullOrWhiteSpace(note.Content)
                ? "Hatırlatma zamanı geldi."
                : note.Content;

            if (message.Length > 180)
                message = message.Substring(0, 180) + "...";

            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = message;

            _trayIcon.BalloonTipIcon =
                System.Windows.Forms.ToolTipIcon.Info;

            _trayIcon.ShowBalloonTip(5000);
        }

        // NOT KAYIT İŞLEMLERİ

        private void UpdateNoteCount()
        {
            int currentIndex = NoteList.SelectedIndex + 1;

            NoteCountText.Text =
                $"Not {currentIndex} / {_notes.Count}";
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

        // BUTON VE METİN İŞLEMLERİ

        private void NewNoteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CreateNewNote();
        }

        private void NoteList_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (NoteList.SelectedItem is Note selectedNote)
                LoadNote(selectedNote);
        }

        private void NoteTitleTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            _currentNote.Title = NoteTitleTextBox.Text;

            SaveNotes();
        }

        private void NoteTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            _currentNote.Content = NoteTextBox.Text;

            SaveNotes();
        }

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

            _notes.Remove(noteToDelete);

            SaveNotes();

            if (_notes.Count == 0)
            {
                CreateNewNote();
                return;
            }

            NoteList.SelectedIndex = 0;
        }

        // PENCERE İŞLEMLERİ

        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Hide();
        }

        private void PinButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Topmost = !Topmost;
        }
    }
}