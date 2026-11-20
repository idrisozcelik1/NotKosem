
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NotKosem.Models;
using NotKosem.Services;

namespace NotKosem
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Note> _notes;

        private Note? _currentNote;

        private bool _isLoadingNote = false;

        private bool _storageReady = true;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // Kayıtlı notları yükle.
                _notes = NoteStorageService.LoadNotes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Notlar yüklenirken bir hata oluştu.\n" +
                    "Mevcut kayıt dosyasını kontrol et.\n\n" +
                    ex.Message,
                    "Yükleme Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                _notes = new ObservableCollection<Note>();

                // Hatalı kayıt dosyasının üzerine yazılmasını önle.
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

        // Yeni not oluşturur.
        private void CreateNewNote()
        {
            Note newNote = new Note();

            _notes.Add(newNote);

            NoteList.SelectedItem = newNote;

            LoadNote(newNote);

            SaveNotes();
        }

        // Seçilen notu ekrana yükler.
        private void LoadNote(Note note)
        {
            _isLoadingNote = true;

            _currentNote = note;

            NoteTitleTextBox.Text = note.Title;

            NoteTextBox.Text = note.Content;

            UpdateNoteCount();

            _isLoadingNote = false;
        }

        // Not sayısını günceller.
        private void UpdateNoteCount()
        {
            int currentIndex = NoteList.SelectedIndex + 1;

            NoteCountText.Text =
                $"Not {currentIndex} / {_notes.Count}";
        }

        // Notları kaydeder.
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
                    "Notlar kaydedilirken hata oluştu:\n" + ex.Message,
                    "Kayıt Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // Yeni not butonu.
        private void NewNoteButton_Click(
            object sender, RoutedEventArgs e)
        {
            CreateNewNote();
        }

        // Not listesinde seçim değiştiğinde çalışır.
        private void NoteList_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (NoteList.SelectedItem is Note selectedNote)
            {
                LoadNote(selectedNote);
            }
        }

        // Not başlığı değiştiğinde çalışır.
        private void NoteTitleTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            _currentNote.Title = NoteTitleTextBox.Text;

            SaveNotes();
        }

        // Not içeriği değiştiğinde çalışır.
        private void NoteTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            _currentNote.Content = NoteTextBox.Text;

            SaveNotes();
        }

        // Önceki nota geçer.
        private void PreviousNoteButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (NoteList.SelectedIndex > 0)
            {
                NoteList.SelectedIndex--;
            }
        }

        // Sonraki nota geçer.
        private void NextNoteButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (NoteList.SelectedIndex < _notes.Count - 1)
            {
                NoteList.SelectedIndex++;
            }
        }

        // Seçili notu siler.
        private void DeleteNoteButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (_currentNote == null)
                return;

            Note noteToDelete = _currentNote;

            MessageBoxResult result = MessageBox.Show(
                "Bu notu silmek istediğine emin misin?",
                "Not Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

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

        // Başlık alanından pencereyi sürükler.
        private void Header_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Pencereyi kapatır.
        private void CloseButton_Click(
            object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Pencerenin üstte kalma özelliğini değiştirir.
        private void PinButton_Click(
            object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
        }
    }
}
