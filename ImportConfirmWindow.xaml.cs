using System.Windows;

namespace SimpleLogger
{
    public partial class ImportConfirmWindow : Window
    {
        public bool Merge { get; private set; }

        public ImportConfirmWindow(int qsoCount)
        {
            InitializeComponent();
            MessageTextBlock.Text = $"You are about to import {qsoCount} QSOs. Would you like to merge them with your current log or replace it entirely?";
        }

        private void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            Merge = true;
            DialogResult = true;
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            Merge = false;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

