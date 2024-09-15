using System.Windows;

namespace ERD_Visualizer
{
    /// <summary>
    /// Interaktionslogik für CustomInputDialog.xaml
    /// </summary>
    public partial class CustomInputDialog : Window
    {
        public string InputValue { get; private set; }

        public CustomInputDialog(string title, string message)
        {
            InitializeComponent();
            this.Title = title;
            msgLabel.Text = message;
            txtInput.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Speichere den eingegebenen Wert und schließe das Dialogfenster
            InputValue = txtInput.Text;
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Schließe das Dialogfenster ohne einen Wert zu speichern
            this.DialogResult = false;
        }
        public static CustomInputDialog Show(string caption, string message)
        {
            return new CustomInputDialog(caption,message);

        }
    }
}
