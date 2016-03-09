using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FinalProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {

            InitializeComponent();
        }
        public float[][] Jdistance; //matrix of all jaccard distance values
        public int linesNumber; //size of rows
        public string[][] FileMatrix; //matrix of the file readed
        public int threadCounter = 3; //number of thread

        public void calc_JDistance (int start, int end, int numOfThread)
        {
            IEnumerable<string> union, intersect;
            float interCount = 0, unionCount = 0;
            float[][] tempDis;
            string[][] temp;
            lock (thisLock)
            {
                temp = FileMatrix;
                tempDis = Jdistance;
            }

            for (int i = start; i < end; i++)
            {
                Jdistance[i] = new float[linesNumber];
                for (int j = i; j < linesNumber; j++)
                {
                    union = temp[i].Union(temp[j]);
                    unionCount = union.Count<string>();
                    intersect = temp[i].Intersect(temp[j]);
                    interCount = intersect.Count<string>();

                    if (unionCount != 0)
                        tempDis[i][j] = interCount / unionCount;
                    else
                        tempDis[i][j] = 0;
                }
            }
            lock (thisLock)
            {
                Array.Copy(tempDis, start, Jdistance, start, end - start);
                threadCounter--; 
                UiInvoke(() => txtEditor.Text += "Thread number " + numOfThread +" is finish\n");
                if (threadCounter == 0)
                {
                    MessageBox.Show("Finish - Jaccard distance", "Thread", MessageBoxButton.OK, MessageBoxImage.Information);
                    UiInvoke(() => txtEditor.Text = String.Join(" ", Jdistance[0].Select(p => p.ToString()).ToArray()));
                }

            }
        }
        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            string FileBuff; //buffer for read the file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FileBuff = File.ReadAllText(openFileDialog.FileName);
                txtEditor.Text = FileBuff; //show the file on txt editor

                string[] lines = FileBuff.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                FileMatrix = new string[lines.Length][];

                for (int i = 0; i < lines.Length; i++)
                {
                    FileMatrix[i] = lines[i].Split(new string[] { "\t", " " }, StringSplitOptions.None);
                }

                Jdistance = new float[lines.Length][];
                linesNumber = lines.Length;
                //Thread creation
                Thread tt1 = new Thread(() => calc_JDistance(0, linesNumber / 5, 1));
                Thread tt2 = new Thread(() => calc_JDistance(linesNumber / 5, (linesNumber / 5) * 2, 2));
                Thread tt3 = new Thread(() => calc_JDistance((linesNumber / 5) * 2, linesNumber, 3));
          
                tt1.Start();
                tt2.Start();
                tt3.Start();
            }

        }
        //INVOKE
        public static void UiInvoke(Action a)
        {
            Application.Current.Dispatcher.Invoke(a);
        }

        private Object thisLock = new Object(); //object lock critical section
        
    }
}
