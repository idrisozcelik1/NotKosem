
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NotKosem.Models;
using NotKosem.Services;

using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;

namespace NotKosem
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Note> _notes;

        private Note? _currentNote;

        private bool _isLoadingNote = false;
        private bool _isExiting = false;
        private bool _storageReady = true;

        private System.Windows.Forms.NotifyIcon? _trayIcon;

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
                    "Notlar yüklenirken hata oluştu:\n" + ex.Message,
                    "NotKöşem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                _notes = new ObservableCollection<Note>();
                _storageReady = false;
            }

            NoteList.ItemsSource = _notes;

            if (_notes.Count == 0)
            {
                CreateNewNote();
            }
            else
            {
                NoteList.SelectedIndex = 0;
            }
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

            var openItem = new System.Windows.Forms.ToolStripMenuItem("NotKöşem'i Aç");
            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Çıkış");

            openItem.Click += (sender, e) => ShowWindow();

            exitItem.Click += (sender, e) =>
            {
                _isExiting = true;

                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }

                System.Windows.Application.Current.Shutdown();
            };

            menu.Items.Add(openItem);
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;

            _trayIcon.DoubleClick += (sender, e) => ShowWindow();
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
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
            }
        }

        private void CreateNewNote()
        {
            Note newNote = new Note();

            _notes.Add(newNote);

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

            UpdateNoteCount();

            _isLoadingNote = false;
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
            {
                LoadNote(selectedNote);
            }
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

        private void PreviousNoteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (NoteList.SelectedIndex > 0)
            {
                NoteList.SelectedIndex--;
            }
        }

        private void NextNoteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (NoteList.SelectedIndex < _notes.Count - 1)
            {
                NoteList.SelectedIndex++;
            }
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

        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
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
