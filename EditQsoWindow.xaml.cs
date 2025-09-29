using System.Windows;

namespace SimpleLogger
{
    public partial class EditQsoWindow : Window
    {
        private QSO _qso;

        public EditQsoWindow(QSO qso)
        {
            InitializeComponent();
            _qso = qso;

            DateTextBox.Text = _qso.Date;
            TimeTextBox.Text = _qso.Time;
            CallsignTextBox.Text = _qso.Callsign;
            NameTextBox.Text = _qso.TheirName;
            LocationTextBox.Text = _qso.TheirLocation;
            GridTextBox.Text = _qso.TheirGrid;
            FrequencyBandTextBox.Text = _qso.FrequencyBand;
            ModeTextBox.Text = _qso.Mode;
            RstSentTextBox.Text = _qso.RstSent;
            RstRcvdTextBox.Text = _qso.RstReceived;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _qso.Date = DateTextBox.Text;
            _qso.Time = TimeTextBox.Text;
            _qso.Callsign = CallsignTextBox.Text;
            _qso.TheirName = NameTextBox.Text;
            _qso.TheirLocation = LocationTextBox.Text;
            _qso.TheirGrid = GridTextBox.Text;
            _qso.FrequencyBand = FrequencyBandTextBox.Text;
            _qso.Mode = ModeTextBox.Text;
            _qso.RstSent = RstSentTextBox.Text;
            _qso.RstReceived = RstRcvdTextBox.Text;

            DialogResult = true;
        }
    }
}

