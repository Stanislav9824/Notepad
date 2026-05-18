using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Notepad.Models
{
    public class Note : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Guid Id { get; set; } = Guid.NewGuid();

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set { if (_title != value) { _title = value; OnPropertyChanged(); } }
        }

        private string _content = string.Empty;
        public string Content
        {
            get => _content;
            set { if (_content != value) { _content = value; OnPropertyChanged(); } }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        public string FontFamily { get; set; } = "Segoe Print";
        public double FontSize { get; set; } = 14;
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public bool IsUnderline { get; set; } = false;
        public string TextColor { get; set; } = "#212121";

        public Note Clone() => new Note
        {
            Id = Id,
            Title = Title,
            Content = Content,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            FontFamily = FontFamily,
            FontSize = FontSize,
            IsBold = IsBold,
            IsItalic = IsItalic,
            IsUnderline = IsUnderline,
            TextColor = TextColor
        };
    }
}
