using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FinalProject
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class kInputWindow : Window
    {
        public string ResultText { get; set; }
        public int kMax { get; set; }

        public kInputWindow(string question,int kMax)
        {
            InitializeComponent();
            lblQuestion.Content = question;
            this.DataContext = this;
            this.kMax = kMax;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Answer))
                MessageBox.Show("Please enter number!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            else if (Convert.ToInt32(Answer) <= 0 || Convert.ToInt32(Answer) > kMax)
                MessageBox.Show(string.Format("Please enter a valid number: greater than 0 and less than number of lines ({0}) !", kMax), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtAnswer.Focus();
        }

        public string Answer
        {
            get { return txtAnswer.Text; }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}

