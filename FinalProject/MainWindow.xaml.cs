using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;


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
            timer.Tick += new EventHandler(dt_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
           
        }

        DispatcherTimer timer = new DispatcherTimer();
        Stopwatch stopWatch = new Stopwatch();
        string currentTime;

        float[][] Jdistance; //matrix of all jaccard distance values
        float[][] CosineSimilarity; //matrix of all cosine similarity values
        int linesNumber; //size of rows
        string[][] FileMatrix; //matrix of the file readed
        int threadCounter = 3; //number of thread
        Object thisLock = new Object(); //object lock critical section
        string FileBuff;

        //INVOKE
        public static void UiInvoke(Action a)
        {
            Application.Current.Dispatcher.Invoke(a);
        }
        //Open DataSet file
        private void Open_File_Click(object sender, RoutedEventArgs e)
        {
            //string FileBuff; //buffer for read the file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FileBuff = File.ReadAllText(openFileDialog.FileName);
                txtEditor.Text = FileBuff; //show the file on txt editor
                jccard_button.IsEnabled = true;
                cosine_button.IsEnabled = true;
            }
        }

        public void calc_CosineSimilarity(int start, int end, int numOfThread)
        {
            HashSet<string> hashCosine= new HashSet<string>();
            float[][] tempDis;
            string[][] temp;
            lock (thisLock)
            {
                temp = FileMatrix;
                tempDis = CosineSimilarity;
            }
            
            for (int i = start; i < end; i++)
            {
                CosineSimilarity[i] = new float[linesNumber];
                for (int j = i; j < linesNumber; j++)
                {
                    
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
                    stopWatch.Stop();
                    UiInvoke(() => loding_progrss.Value = 100);
                    UiInvoke(() => MessageBox.Show(String.Format("Finish - Cosine similarity on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
                    UiInvoke(() => txtEditor.Text = String.Join(" ", Jdistance[0].Select(p => p.ToString()).ToArray()));
                    UiInvoke(() => jccard_button.IsEnabled = true);
                    threadCounter = 3;
                }
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
                    stopWatch.Stop();
                    UiInvoke(() => loding_progrss.Value = 100);
                    UiInvoke(() => MessageBox.Show(String.Format("Finish - Jaccard distance on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
                    UiInvoke(() => txtEditor.Text = String.Join(" ", Jdistance[0].Select(p => p.ToString()).ToArray()));
                    UiInvoke(() => loding_progrss.Visibility = Visibility.Hidden);
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
                cosine_button.IsEnabled = false;
                loding_progrss.Visibility = Visibility.Visible;
                currentTime = string.Empty;
                stopWatch.Reset();
                stopWatch.Start();  
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
            cosine_button.IsEnabled = false;
            jccard_button.IsEnabled = false;
            HashSet<string> hashCosine = new HashSet<string>();
            Dictionary<string, int > init_dict = new Dictionary<string, int >();//define defulat dictioanry for all the dataset
            
            string[] lines = FileBuff.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            FileMatrix = new string[lines.Length][];
            for (int i = 0; i < lines.Length; i++)
            {
                FileMatrix[i] = lines[i].Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);
            }
            for ( int i = 0 ; i<lines.Length; i++)
            {
                for (int j = 0 ; j<FileMatrix[i].Length ; j++)
                {
                    hashCosine.Add(FileMatrix[i][j]);
                }
            }
            txtEditor.Clear();
            
            foreach (string i in hashCosine)
            {
                txtEditor.Text += string.Format (" {0}",i);
            }

            txtEditor.Text += "\n" + hashCosine.Count.ToString();
            foreach (string  key in hashCosine)
            {
                init_dict[key] = 0;
            }
            Dictionary<string, int>[] arr_dict = new Dictionary<string, int>[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                arr_dict[i] = new Dictionary<string, int>(init_dict);// init array of dictionaris by the file 
            }
            for (int i =0 ; i<lines.Length; i++)
            {
                for (int j =0 ; j< FileMatrix[i].Length ; j++)
                {
                    if (arr_dict[i].ContainsKey(FileMatrix[i][j]))
                    {
                        arr_dict[i][FileMatrix[i][j]] += 1;// cehck if the key is exsit in the line and up the value of the key 
                    }
                }
            }
            CosineSimilarity = new float[lines.Length][];
            float numerator;
            float denominatorA, denominatorB;
            for (int i = 0; i < arr_dict.Length; i++)
            {
                CosineSimilarity[i] = new float[lines.Length];  
                for (int j = i; j < arr_dict.Length; j++)
                {
                    numerator = 0;
                    denominatorA = 0;
                    denominatorB = 0;

                    foreach (var item in arr_dict[i].Values)
                    {
                        item += 1; 
                        //numerator += arr_dict[j][item.Key] * item.Value;
                        //denominatorA += (float)Math.Pow(item.Value, 2);
                        //denominatorB += (float)Math.Pow(arr_dict[j][item.Key], 2);
                    }
                //    CosineSimilarity[i][j] = numerator / (float)(Math.Sqrt(denominatorA) * (float)Math.Sqrt(denominatorB));
                }

            }
            txtEditor.Text = " ";
            //for (int i = 0; i < arr_dict.Length; i++)
            //{
            //    foreach (KeyValuePair<string, int> kvp in arr_dict[i])
            //    {
            //        txtEditor.Text = string.Format("{0}", kvp.Value);
            //    }
            //}






                        ///double dotProduct = 0.0;
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

        void dt_Tick(object sender, EventArgs e)
        {
            if (stopWatch.IsRunning)
            {
                TimeSpan ts = stopWatch.Elapsed;
                currentTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                ClockTextBlock.Text = currentTime;
                loding_progrss.Value = ts.Seconds;
            }
        }

        private void loding_progrss_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }



        }


}
