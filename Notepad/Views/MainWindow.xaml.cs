using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Notepad.ViewModels;

namespace Notepad.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            SetupKeyboardShortcuts();
        }

        // ── Keyboard shortcuts (Code-Behind) ─────────────────────────
        private void SetupKeyboardShortcuts()
        {
            // Ctrl+S → Save
            var saveGesture = new KeyGesture(Key.S, ModifierKeys.Control);
            var saveBinding = new KeyBinding(ViewModel.SaveNoteCommand, saveGesture);
            InputBindings.Add(saveBinding);

            // Ctrl+N → New note
            var newGesture = new KeyGesture(Key.N, ModifierKeys.Control);
            var newBinding = new KeyBinding(ViewModel.NewNoteCommand, newGesture);
            InputBindings.Add(newBinding);

            // Ctrl+Z → Undo
            var undoGesture = new KeyGesture(Key.Z, ModifierKeys.Control);
            var undoBinding = new KeyBinding(ViewModel.UndoNoteCommand, undoGesture);
            InputBindings.Add(undoBinding);

            // Ctrl+Delete → Delete note
            var delGesture = new KeyGesture(Key.Delete, ModifierKeys.Control);
            var delBinding = new KeyBinding(ViewModel.DeleteNoteCommand, delGesture);
            InputBindings.Add(delBinding);

            // Ctrl+B → Toggle Bold
            var boldGesture = new KeyGesture(Key.B, ModifierKeys.Control);
            var boldCommand = new RelayKeyCommand(() => ViewModel.NoteIsBold = !ViewModel.NoteIsBold);
            var boldBinding = new KeyBinding(boldCommand, boldGesture);
            InputBindings.Add(boldBinding);

            // Ctrl+I → Toggle Italic
            var italicGesture = new KeyGesture(Key.I, ModifierKeys.Control);
            var italicCommand = new RelayKeyCommand(() => ViewModel.NoteIsItalic = !ViewModel.NoteIsItalic);
            var italicBinding = new KeyBinding(italicCommand, italicGesture);
            InputBindings.Add(italicBinding);

            // Ctrl+U → Toggle Underline
            var underlineGesture = new KeyGesture(Key.U, ModifierKeys.Control);
            var underlineCommand = new RelayKeyCommand(() => ViewModel.NoteIsUnderline = !ViewModel.NoteIsUnderline);
            var underlineBinding = new KeyBinding(underlineCommand, underlineGesture);
            InputBindings.Add(underlineBinding);
        }

        // Focus the editor when a note is selected from the list (Code-Behind)
        private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.HasSelectedNote)
            {
                NoteEditor?.Focus();
            }
        }

        // Auto-save on note content change after 2 seconds idle (Code-Behind)
        private System.Windows.Threading.DispatcherTimer? _autoSaveTimer;

        private void NoteEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            _autoSaveTimer?.Stop();
            _autoSaveTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(3)
            };
            _autoSaveTimer.Tick += (s, args) =>
            {
                _autoSaveTimer.Stop();
                if (ViewModel.HasSelectedNote && ViewModel.SaveNoteCommand.CanExecute(null))
                    ViewModel.SaveNoteCommand.Execute(null);
            };
            _autoSaveTimer.Start();
        }
    }

    // Simple relay command for code-behind use
    internal class RelayKeyCommand : ICommand
    {
        private readonly System.Action _action;
        public RelayKeyCommand(System.Action action) => _action = action;
        public event System.EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _action();
    }
}
