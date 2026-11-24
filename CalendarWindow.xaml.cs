using NotKosem.Models;
using NotKosem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;

namespace NotKosem
{
    public partial class CalendarWindow : Window
    {
        private List<CalendarEvent> _events = new();
        private DateTime _displayedMonth = DateTime.Today;
        private DateTime _selectedDate = DateTime.Today;
        private CalendarEvent? _selectedEvent;

        public CalendarWindow()
            : this(DateTime.Today, null)
        {
        }

        public CalendarWindow(DateTime selectedDate, string? selectedEventId)
        {
            InitializeComponent();

            _events = CalendarStorageService.LoadEvents();

            _selectedDate = selectedDate.Date;
            _displayedMonth = selectedDate;

            CreateCalendarHeaders();
            DrawCalendar();
            RefreshEventList();
            ResetEditorForm();

            if (selectedEventId != null)
            {
                CalendarEvent? selectedEvent = _events
                    .FirstOrDefault(x => x.Id == selectedEventId);

                if (selectedEvent != null)
                {
                    EventList.SelectedItem = selectedEvent;
                }
            }
        }

        private void ResetEditorForm()
        {
            _selectedEvent = null;
            EventList.SelectedItem = null;
            EditorHeaderText.Text = "Yeni Etkinlik Ekle";
            EventTitleTextBox.Text = "";
            EventDescriptionTextBox.Text = "";
            EventDatePicker.SelectedDate = _selectedDate;
            EventTimeTextBox.Text = "09:00";
            EventEditor.Visibility = Visibility.Visible;
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedDate = DateTime.Today;
            _displayedMonth = DateTime.Today;

            DrawCalendar();
            RefreshEventList();
            ResetEditorForm();
        }

        private void CreateCalendarHeaders()
        {
            string[] days = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };

            for (int i = 0; i < days.Length; i++)
            {
                TextBlock header = new TextBlock
                {
                    Text = days[i],
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(92, 81, 50))
                };

                Grid.SetRow(header, 0);
                Grid.SetColumn(header, i);

                CalendarGrid.Children.Add(header);
            }
        }

        private void DrawCalendar()
        {
            MonthYearText.Text = _displayedMonth.ToString("MMMM yyyy");

            CalendarGrid.Children.Clear();
            CreateCalendarHeaders();

            DateTime firstDay = new DateTime(
                _displayedMonth.Year,
                _displayedMonth.Month,
                1);

            int startOffset = ((int)firstDay.DayOfWeek + 6) % 7;

            int daysInMonth = DateTime.DaysInMonth(
                _displayedMonth.Year,
                _displayedMonth.Month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date = new DateTime(
                    _displayedMonth.Year,
                    _displayedMonth.Month,
                    day);

                int position = startOffset + day - 1;

                int row = position / 7 + 1;
                int column = position % 7;

                int eventCount = _events.Count(x => x.EventDate.Date == date.Date);

                StackPanel dayContent = new StackPanel
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                dayContent.Children.Add(new TextBlock
                {
                    Text = day.ToString(),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    FontSize = 12,
                    LineHeight = 14,
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                    FontWeight = date.Date == DateTime.Today
                        ? FontWeights.Bold
                        : FontWeights.Normal
                });

                if (eventCount > 0)
                {
                    dayContent.Children.Add(new TextBlock
                    {
                        Text = eventCount.ToString(),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        FontSize = 9,
                        LineHeight = 10,
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(170, 105, 45))
                    });
                }

                Button button = new Button
                {
                    Content = dayContent,
                    Margin = new Thickness(2),
                    Padding = new Thickness(0),
                    Background = date.Date == _selectedDate.Date
                        ? new SolidColorBrush(Color.FromRgb(214, 196, 157))
                        : date.Date == DateTime.Today
                            ? new SolidColorBrush(Color.FromRgb(244, 238, 220))
                            : new SolidColorBrush(Color.FromRgb(255, 253, 248)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(216, 203, 170)),
                    Foreground = new SolidColorBrush(Color.FromRgb(92, 81, 50)),
                    Tag = date
                };

                button.Click += CalendarDayButton_Click;

                Grid.SetRow(button, row);
                Grid.SetColumn(button, column);

                CalendarGrid.Children.Add(button);
            }
        }

        private void CalendarDayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DateTime date)
            {
                _selectedDate = date;

                DrawCalendar();
                RefreshEventList();
                ResetEditorForm();
            }
        }

        private void RefreshEventList()
        {
            SelectedDateText.Text = _selectedDate.ToString("dd MMMM yyyy");

            EventList.ItemsSource = null;

            EventList.ItemsSource = _events
                .Where(x => x.EventDate.Date == _selectedDate.Date)
                .OrderBy(x => x.EventDate)
                .ToList();
        }

        private void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _displayedMonth = _displayedMonth.AddMonths(-1);
            DrawCalendar();
        }

        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _displayedMonth = _displayedMonth.AddMonths(1);
            DrawCalendar();
        }

        private void AddEventButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditorForm();
            EventTitleTextBox.Focus();
        }

        private void SaveEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EventTitleTextBox.Text))
            {
                MessageBox.Show(
                    "Etkinlik başlığı giriniz.",
                    "NotKöşem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (EventDatePicker.SelectedDate == null)
            {
                MessageBox.Show(
                    "Etkinlik tarihi seçiniz.",
                    "NotKöşem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (!TimeSpan.TryParse(EventTimeTextBox.Text, out TimeSpan time))
            {
                MessageBox.Show(
                    "Geçerli bir saat giriniz. Örnek: 14:30",
                    "NotKöşem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            DateTime eventDate = EventDatePicker.SelectedDate.Value.Date.Add(time);

            if (_selectedEvent == null)
            {
                CalendarEvent newEvent = new CalendarEvent
                {
                    Title = EventTitleTextBox.Text.Trim(),
                    Description = EventDescriptionTextBox.Text.Trim(),
                    EventDate = eventDate
                };

                _events.Add(newEvent);
            }
            else
            {
                _selectedEvent.Title = EventTitleTextBox.Text.Trim();
                _selectedEvent.Description = EventDescriptionTextBox.Text.Trim();
                _selectedEvent.EventDate = eventDate;
            }

            CalendarStorageService.SaveEvents(_events);

            _selectedDate = eventDate.Date;
            _displayedMonth = eventDate;

            DrawCalendar();
            RefreshEventList();
            ResetEditorForm();
        }

        private void EventList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EventList.SelectedItem is CalendarEvent calendarEvent)
            {
                _selectedEvent = calendarEvent;
                EditorHeaderText.Text = "Etkinliği Düzenle";

                EventTitleTextBox.Text = calendarEvent.Title;
                EventDescriptionTextBox.Text = calendarEvent.Description;
                EventDatePicker.SelectedDate = calendarEvent.EventDate.Date;
                EventTimeTextBox.Text = calendarEvent.EventDate.ToString("HH:mm");

                EventEditor.Visibility = Visibility.Visible;
            }
        }

        private void DeleteEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (EventList.SelectedItem is not CalendarEvent calendarEvent)
            {
                MessageBox.Show(
                    "Silmek için bir etkinlik seçiniz.",
                    "NotKöşem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Bu etkinliği silmek istediğinize emin misiniz?",
                "Etkinlik Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            _events.Remove(calendarEvent);

            CalendarStorageService.SaveEvents(_events);

            DrawCalendar();
            RefreshEventList();
            ResetEditorForm();
        }

        private void CancelEventButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditorForm();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}