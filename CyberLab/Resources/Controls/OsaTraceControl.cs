using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberLab.Resources.Controls
{
    [TemplatePart(Name = "OsaTraceControl_ComboBox", Type = typeof(ComboBox))]
    public class OsaTraceControl : Control
    {
        public static readonly DependencyProperty IsSelectionEnabledProperty =
            DependencyProperty.Register(
                nameof(IsSelectionEnabled),
                typeof(bool),
                typeof(OsaTraceControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(OsaTraceControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TraceLetterProperty =
            DependencyProperty.Register(
                nameof(TraceLetter),
                typeof(string),
                typeof(OsaTraceControl),
                new PropertyMetadata("A"));

        public static readonly DependencyProperty TraceColorBrushProperty =
            DependencyProperty.Register(
                nameof(TraceColorBrush),
                typeof(Brush),
                typeof(OsaTraceControl),
                new PropertyMetadata(Brushes.DodgerBlue));

        public static readonly DependencyProperty SelectedComboBoxItemStringProperty =
            DependencyProperty.Register(
                nameof(SelectedComboBoxItemString),
                typeof(string),
                typeof(OsaTraceControl),
                new PropertyMetadata(string.Empty));

        public static readonly RoutedEvent SelectionChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(SelectionChanged),
                RoutingStrategy.Bubble,
                typeof(SelectionChangedEventHandler),
                typeof(OsaTraceControl));

        static OsaTraceControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OsaTraceControl), new FrameworkPropertyMetadata(typeof(OsaTraceControl)));
        }

        public bool IsSelectionEnabled
        {
            get => (bool)GetValue(IsSelectionEnabledProperty);
            set => SetValue(IsSelectionEnabledProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string TraceLetter
        {
            get => (string)GetValue(TraceLetterProperty);
            set => SetValue(TraceLetterProperty, value);
        }

        public Brush TraceColorBrush
        {
            get => (Brush)GetValue(TraceColorBrushProperty);
            set => SetValue(TraceColorBrushProperty, value);
        }

        public string SelectedComboBoxItemString
        {
            get => (string)GetValue(SelectedComboBoxItemStringProperty);
            set => SetValue(SelectedComboBoxItemStringProperty, value);
        }

        public event SelectionChangedEventHandler SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value);
            remove => RemoveHandler(SelectionChangedEvent, value);
        }

        public ComboBox? ComboBoxInstance { get; private set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("OsaTraceControl_ComboBox") is ComboBox innerComboBox)
            {
                innerComboBox.SelectionChanged += OnInnerComboBoxSelectionChanged;
                ComboBoxInstance = innerComboBox;
            }
        }

        private void OnInnerComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxInstance?.SelectedItem is ComboBoxItem selectedItem)
            {
                SelectedComboBoxItemString = selectedItem.Content?.ToString() ?? string.Empty;
            }
            else if (ComboBoxInstance?.SelectedItem != null)
            {
                SelectedComboBoxItemString = ComboBoxInstance.SelectedItem.ToString() ?? string.Empty;
            }

            var newEventArgs = new SelectionChangedEventArgs(SelectionChangedEvent, e.RemovedItems, e.AddedItems);
            RaiseEvent(newEventArgs);
        }
    }
}

