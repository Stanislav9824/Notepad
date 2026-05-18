using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Notepad.Commands;
using Notepad.Localization;
using Notepad.Models;
using Notepad.Services;

namespace Notepad.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly NoteService _noteService = new();
        private readonly Stack<Note> _undoStack = new();
        private const int MaxUndoStackSize = 50;

        public ObservableCollection<Note> Notes { get; } = new();
        public ObservableCollection<Note> FilteredNotes { get; } = new();

        private Note? _selectedNote;
        public Note? SelectedNote
        {
            get => _selectedNote;
            set
            {
                if (SetProperty(ref _selectedNote, value))
                    RefreshNoteProperties();
            }
        }

        public bool HasSelectedNote => _selectedNote != null;

        // ── Formatted properties for direct binding (no converters needed) ──

        private string _noteTitle = string.Empty;
        public string NoteTitle
        {
            get => _noteTitle;
            set
            {
                if (SetProperty(ref _noteTitle, value) && _selectedNote != null)
                {
                    _selectedNote.Title = value;
                    // Do NOT call RefreshFilteredNotes() here - it rebuilds the collection
                    // and resets SelectedItem, closing the note on every keystroke.
                    // The list item text updates automatically via the Note object reference.
                }
            }
        }

        private string _noteContent = string.Empty;
        public string NoteContent
        {
            get => _noteContent;
            set
            {
                if (SetProperty(ref _noteContent, value) && _selectedNote != null)
                    _selectedNote.Content = value;
            }
        }

        private string _noteFontFamily = "Segoe UI";
        public string NoteFontFamily
        {
            get => _noteFontFamily;
            set
            {
                if (SetProperty(ref _noteFontFamily, value) && _selectedNote != null)
                    _selectedNote.FontFamily = value;
            }
        }

        private double _noteFontSize = 14;
        public double NoteFontSize
        {
            get => _noteFontSize;
            set
            {
                if (SetProperty(ref _noteFontSize, value) && _selectedNote != null)
                    _selectedNote.FontSize = value;
            }
        }

        private bool _noteIsBold;
        public bool NoteIsBold
        {
            get => _noteIsBold;
            set
            {
                if (SetProperty(ref _noteIsBold, value))
                {
                    if (_selectedNote != null) _selectedNote.IsBold = value;
                    OnPropertyChanged(nameof(NoteFontWeight));
                }
            }
        }

        private bool _noteIsItalic;
        public bool NoteIsItalic
        {
            get => _noteIsItalic;
            set
            {
                if (SetProperty(ref _noteIsItalic, value))
                {
                    if (_selectedNote != null) _selectedNote.IsItalic = value;
                    OnPropertyChanged(nameof(NoteFontStyle));
                }
            }
        }

        private bool _noteIsUnderline;
        public bool NoteIsUnderline
        {
            get => _noteIsUnderline;
            set
            {
                if (SetProperty(ref _noteIsUnderline, value))
                {
                    if (_selectedNote != null) _selectedNote.IsUnderline = value;
                    OnPropertyChanged(nameof(NoteTextDecoration));
                }
            }
        }

        private string _noteTextColor = "#212121";
        public string NoteTextColor
        {
            get => _noteTextColor;
            set
            {
                if (SetProperty(ref _noteTextColor, value))
                {
                    if (_selectedNote != null) _selectedNote.TextColor = value;
                    OnPropertyChanged(nameof(NoteTextBrush));
                }
            }
        }

        // ── Computed display properties (bound directly, no converters) ──

        public FontWeight NoteFontWeight => _noteIsBold ? FontWeights.Bold : FontWeights.Normal;
        public FontStyle NoteFontStyle => _noteIsItalic ? FontStyles.Italic : FontStyles.Normal;
        public TextDecorationCollection? NoteTextDecoration => _noteIsUnderline ? TextDecorations.Underline : null;
        public SolidColorBrush NoteTextBrush
        {
            get
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(_noteTextColor)); }
                catch { return new SolidColorBrush(Colors.Black); }
            }
        }

        // ── Search ──
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) RefreshFilteredNotes(); }
        }

        // ── Language ──
        private string _currentLanguage = "en";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (SetProperty(ref _currentLanguage, value))
                {
                    LocalizationManager.Instance.ApplyLanguage(value);
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public string StatusText
        {
            get
            {
                if (_selectedNote == null) return LocalizationManager.Get("NoNoteSelected");
                return $"{LocalizationManager.Get("Modified")}: {_selectedNote.ModifiedAt:g}   {LocalizationManager.Get("Created")}: {_selectedNote.CreatedAt:g}";
            }
        }

        // ── Font / color lists ──
        public List<string> FontFamilies { get; } = new()
        {
            "Times New Roman",
            "Consolas",
            "Segoe Print"
        };

        public List<double> FontSizes { get; } = new() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 28, 32, 36, 48, 72 };

        public List<ColorItem> TextColors { get; } = new()
        {
            new("#212121", "Black"),
            new("#1565C0", "Blue"),
            new("#B71C1C", "Red"),
            new("#1B5E20", "Green"),
            new("#F57F17", "Orange"),
            new("#4A148C", "Purple"),
            new("#00838F", "Teal"),
            new("#37474F", "Gray"),
        };

        // ── Commands ──
        public ICommand NewNoteCommand { get; }
        public ICommand SaveNoteCommand { get; }
        public ICommand DeleteNoteCommand { get; }
        public ICommand UndoNoteCommand { get; }
        public ICommand ChangeLanguageCommand { get; }
        public ICommand SetColorCommand { get; }

        public MainViewModel()
        {
            NewNoteCommand    = new RelayCommand(NewNote);
            SaveNoteCommand   = new RelayCommand(SaveNote,   () => HasSelectedNote);
            DeleteNoteCommand = new RelayCommand(DeleteNote, () => HasSelectedNote);
            UndoNoteCommand   = new RelayCommand(UndoNote,   () => _undoStack.Count > 0);
            ChangeLanguageCommand = new RelayCommand<string>(lang => CurrentLanguage = lang ?? "en");
            SetColorCommand       = new RelayCommand<string>(color => { if (color != null) NoteTextColor = color; });
            LoadNotes();
        }

        private void LoadNotes()
        {
            var notes = _noteService.LoadAll();
            Notes.Clear();
            foreach (var n in notes) Notes.Add(n);
            RefreshFilteredNotes();
        }

        private void RefreshFilteredNotes()
        {
            FilteredNotes.Clear();
            var query = string.IsNullOrWhiteSpace(_searchText)
                ? Notes
                : Notes.Where(n =>
                    n.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    n.Content.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
            foreach (var n in query.OrderByDescending(n => n.ModifiedAt))
                FilteredNotes.Add(n);
        }

        private void RefreshNoteProperties()
        {
            var n = _selectedNote;
            _noteTitle      = n?.Title      ?? string.Empty;
            _noteContent    = n?.Content    ?? string.Empty;
            _noteFontFamily = n?.FontFamily ?? "Segoe UI";
            _noteFontSize   = n?.FontSize   ?? 14;
            _noteIsBold     = n?.IsBold     ?? false;
            _noteIsItalic   = n?.IsItalic   ?? false;
            _noteIsUnderline= n?.IsUnderline?? false;
            _noteTextColor  = n?.TextColor  ?? "#212121";

            OnPropertyChanged(nameof(HasSelectedNote));
            OnPropertyChanged(nameof(NoteTitle));
            OnPropertyChanged(nameof(NoteContent));
            OnPropertyChanged(nameof(NoteFontFamily));
            OnPropertyChanged(nameof(NoteFontSize));
            OnPropertyChanged(nameof(NoteIsBold));
            OnPropertyChanged(nameof(NoteIsItalic));
            OnPropertyChanged(nameof(NoteIsUnderline));
            OnPropertyChanged(nameof(NoteTextColor));
            OnPropertyChanged(nameof(NoteFontWeight));
            OnPropertyChanged(nameof(NoteFontStyle));
            OnPropertyChanged(nameof(NoteTextDecoration));
            OnPropertyChanged(nameof(NoteTextBrush));
            OnPropertyChanged(nameof(StatusText));
        }

        private void NewNote()
        {
            var note = new Note { Title = LocalizationManager.Get("UntitledNote") };
            Notes.Add(note);
            RefreshFilteredNotes();
            SelectedNote = note;
        }

        private void SaveNote()
        {
            if (_selectedNote == null) return;
            _undoStack.Push(_selectedNote.Clone());
            if (_undoStack.Count > MaxUndoStackSize)
            {
                var temp = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = 0; i < MaxUndoStackSize; i++)
                    _undoStack.Push(temp[i]);
            }
            _selectedNote.ModifiedAt = DateTime.Now;
            OnPropertyChanged(nameof(StatusText));
            _noteService.SaveAll(Notes);
        }

        private void DeleteNote()
        {
            if (_selectedNote == null) return;
            var result = MessageBox.Show(
                LocalizationManager.Get("ConfirmDelete"),
                LocalizationManager.Get("ConfirmDeleteTitle"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            Notes.Remove(_selectedNote);
            RefreshFilteredNotes();
            _noteService.SaveAll(Notes);
            SelectedNote = FilteredNotes.FirstOrDefault();
        }

        private void UndoNote()
        {
            if (_undoStack.Count == 0) return;
            var prev = _undoStack.Pop();
            var existing = Notes.FirstOrDefault(n => n.Id == prev.Id);
            if (existing == null) return;
            var idx = Notes.IndexOf(existing);
            Notes[idx] = prev;
            RefreshFilteredNotes();
            if (SelectedNote?.Id == prev.Id) SelectedNote = prev;
            _noteService.SaveAll(Notes);
        }
    }

    public record ColorItem(string Hex, string Name)
    {
        public SolidColorBrush Brush
        {
            get
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(Hex)); }
                catch { return new SolidColorBrush(Colors.Black); }
            }
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        { _execute = execute; _canExecute = canExecute; }
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute((T?)parameter);
        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}
