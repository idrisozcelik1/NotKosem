
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NotKosem.Models;

namespace NotKosem
{
    public partial class MainWindow : Window
    {
        // Uygulamadaki bütün notları tutar.
        private ObservableCollection<Note> _notes;

        // Şu anda üzerinde çalıştığımız not.
        private Note? _currentNote;

        // Arayüzü güncellerken olayların birbirini tetiklemesini önler.
        private bool _isLoadingNote = false;

        // Pencere oluşturulduğunda çalışır.
        public MainWindow()
        {
            InitializeComponent();

            _notes = new ObservableCollection<Note>();

            // Not listesini koleksiyona bir kez bağla.
            NoteList.ItemsSource = _notes;

            CreateNewNote();
        }

        // Yeni not oluşturur.
        private void CreateNewNote()
        {
            Note newNote = new Note();

            // Yeni notu listeye ekle.
            _notes.Add(newNote);

            

            // Yeni oluşturulan notu seç.
            NoteList.SelectedItem = newNote;

            // Notu ekranda göster.
            LoadNote(newNote);
        }

        // Seçilen notu ekrana yükler.
        private void LoadNote(Note note)
        {
            _isLoadingNote = true;

            _currentNote = note;

            // Başlığı ekrana yaz.
            NoteTitleTextBox.Text = note.Title;

            // İçeriği ekrana yaz.
            NoteTextBox.Text = note.Content;

            // Not sayısını güncelle.
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
        }

        // Not içeriği değiştiğinde çalışır.
        private void NoteTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (_isLoadingNote || _currentNote == null)
                return;

            _currentNote.Content = NoteTextBox.Text;
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

            // Silinecek notu sakla.
            Note noteToDelete = _currentNote;

            // Onay iste.
            MessageBoxResult result = MessageBox.Show(
                "Bu notu silmek istediğine emin misin?",
                "Not Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // Notu koleksiyondan sil.
            _notes.Remove(noteToDelete);

            // Hiç not kalmadıysa yeni not oluştur.
            if (_notes.Count == 0)
            {
                CreateNewNote();
                return;
            }

            // İlk notu seç.
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
