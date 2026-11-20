
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NotKosem.Models
{
    public class Note : INotifyPropertyChanged
    {
        private string _title = "Yeni Not";
        private string _content = string.Empty;
        private string _backgroundColor = "#FFFFFF";
        private double _opacity = 1.0;
        private DateTime? _reminderAt;
        private string _category = "Diğer";

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }

        public string BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                OnPropertyChanged();
            }
        }

        public double Opacity
        {
            get => _opacity;
            set
            {
                _opacity = value;
                OnPropertyChanged();
            }
        }

        public DateTime? ReminderAt
        {
            get => _reminderAt;
            set
            {
                _reminderAt = value;
                OnPropertyChanged();
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
