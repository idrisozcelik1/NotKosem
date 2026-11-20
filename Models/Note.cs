
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NotKosem.Models
{
    public class Note : INotifyPropertyChanged
    {
        // Notun benzersiz kimliği.
        public Guid Id { get; set; } = Guid.NewGuid();

        // Notun oluşturulma tarihi.
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Not başlığı.
        private string _title = "Yeni Not";

        public string Title
        {
            get { return _title; }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        // Not içeriği.
        private string _content = "";

        public string Content
        {
            get { return _content; }
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged();
                }
            }
        }

        // Değişiklik olduğunda arayüze haber verir.
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
