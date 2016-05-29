using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for CalculateWindow.xaml
    /// </summary>
    public partial class CalculateWindow : Window
    {
        public CalculateWindow(string PR, string Kmeans)
        {
            InitializeComponent();
            textBox_pagerank.Text = PR;
            textBox_kmeans.Text = Kmeans;
        }
    }
}
