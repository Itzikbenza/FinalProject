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
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace FinalProject
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        DispatcherTimer timer = new DispatcherTimer();
        public int timeSpan=0;
        public float[][] Jdistance; //matrix of all jaccard distance values
        public int linesNumber; //size of rows
        public string[][] FileMatrix; //matrix of the file readed
        public int threadCounter = 3; //number of thread
        public Object thisLock = new Object(); //object lock critical section
        public string FileBuff;

        //INVOKE
        public static void UiInvoke(Action a)
        {
            Application.Current.Dispatcher.Invoke(a);
        }

        private void Open_File_Click(object sender, RoutedEventArgs e)
        {
            //string FileBuff; //buffer for read the file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FileBuff = File.ReadAllText(openFileDialog.FileName);
                txtEditor.Text = FileBuff; //show the file on txt editor
            }
        }

        public void calc_JDistance(int start, int end, int numOfThread)
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
                UiInvoke(() => txtEditor.Text += "Thread number " + numOfThread + " is finish\n");
                if (threadCounter == 0)
                {
                    timer.Stop();
                    MessageBox.Show("Finish - Jaccard distance", "Thread", MessageBoxButton.OK, MessageBoxImage.Information);
                    UiInvoke(() => txtEditor.Text = String.Join(" ", Jdistance[0].Select(p => p.ToString()).ToArray()));
                    UiInvoke(() => jccard_button.IsEnabled = true);
                    threadCounter = 3; 
                }
            }

        }

        private void jccard_button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FileBuff))
                MessageBox.Show("Please choose some DataSet by clicking on Browse button", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            else
            {
                jccard_button.IsEnabled = false;
                seconds_leb.Visibility = Visibility.Visible;
                timeSpan = 0;
                timer.Interval = new TimeSpan(0,0,1);
                timer.Tick += timer_Tick;
                timer.Start();
               
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

        private void cosine_button_Click(object sender, RoutedEventArgs e)
        {

            //double dotProduct = 0.0;
            //double normA = 0.0;
            //double normB = 0.0;
            //for (int i = 0; i < vectorA.length; i++)
            //{
            //    dotProduct += vectorA[i] * vectorB[i];
            //    normA += Math.pow(vectorA[i], 2);
            //    normB += Math.pow(vectorB[i], 2);
            //}
            //return dotProduct / (Math.sqrt(normA) * Math.sqrt(normB));


            //CosineSimilarity sim = new CosineSimilarity();
            //// create two vectors for inputs
            //double[] p = new double[] { 2.5, 3.5, 3.0, 3.5, 2.5, 3.0 };
            //double[] q = new double[] { 3.0, 3.5, 1.5, 5.0, 3.5, 3.0 };
            //// get similarity between the two vectors
            //double similarityScore = sim.GetSimilarityScore(p, q);
        }

        public void timer_Tick(object sender, EventArgs e)
        {
            time_leb.Content = timeSpan++;
            CommandManager.InvalidateRequerySuggested();
        }

    }


}
