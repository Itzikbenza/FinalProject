﻿using Microsoft.Win32;
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
        Dictionary<string, int>[] arr_dict;
        int linesNumber; //size of rows
        string[][] FileMatrix; //matrix of the file readed
        int threadCounter = 3; //number of thread
        Object thisLock = new Object(); //object lock critical section
        string FileBuff;
        int flag = 0;

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

        public void splitBySpacesAndLines(string text)
        {
            string[] lines = text.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            linesNumber = lines.Length;
            FileMatrix = new string[linesNumber][];
            for (int i = 0; i < linesNumber; i++)
                FileMatrix[i] = lines[i].Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);
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
                    UiInvoke(() => MessageBox.Show(String.Format("Finish - Jaccard distance on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
                    UiInvoke(() => txtEditor.Text = String.Join(" ", Jdistance[0].Select(p => p.ToString()).ToArray()));
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
                currentTime = string.Empty;
                flag = 1;
                stopWatch.Reset();
                stopWatch.Start();
                timer.Start();
                splitBySpacesAndLines(FileBuff);

                Jdistance = new float[linesNumber][];

                //Thread creation
                Thread tt1 = new Thread(() => calc_JDistance(0, (int)(linesNumber * 0.2), 1));
                Thread tt2 = new Thread(() => calc_JDistance((int)(linesNumber * 0.2), 2 * (int)(linesNumber * 0.2)  , 2));
                Thread tt3 = new Thread(() => calc_JDistance(2 * (int)(linesNumber * 0.2), linesNumber, 3));
               
                tt1.Start();
                tt2.Start();
                tt3.Start();
            }
        }

        public void cosineReadData()
        {
            HashSet<string> hashCosine = new HashSet<string>();
            Dictionary<string, int> init_dict = new Dictionary<string, int>();//define defulat dictioanry for all the dataset

            splitBySpacesAndLines(FileBuff);

            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; j < FileMatrix[i].Length; j++)
                    hashCosine.Add(FileMatrix[i][j]);

            string text_show = null;
            text_show = "The hash set include the values : ";
            foreach (string i in hashCosine)
                text_show += string.Format("{0} ", i);

            text_show += string.Format("\nThe hash set include {0} values", hashCosine.Count.ToString());
            UiInvoke(() => txtEditor.Text = text_show);

            foreach (string key in hashCosine)
                init_dict[key] = 0;

            arr_dict = new Dictionary<string, int>[linesNumber];
            for (int i = 0; i < linesNumber; i++)
                arr_dict[i] = new Dictionary<string, int>(init_dict);// init array of dictionaris by the file 

            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; j < FileMatrix[i].Length; j++)
                    if (arr_dict[i].ContainsKey(FileMatrix[i][j]))
                        arr_dict[i][FileMatrix[i][j]] += 1;// cehck if the key is exsit in the line and up the value of the key
        }

        public void calc_CosineSimilarity(Dictionary<string, int>[] arr_dict)
        {
            CosineSimilarity = new float[linesNumber][];
            double numerator;
            double denominatorA, denominatorB;
            for (int i = 0; i < arr_dict.Length; i++)
            {
                CosineSimilarity[i] = new float[linesNumber];
                for (int j = i; j < arr_dict.Length; j++)
                {
                    numerator = 0.0;
                    denominatorA = 0.0;
                    denominatorB = 0.0;
                   
                    foreach (var item in arr_dict[i])
                    {
                        numerator += arr_dict[j][item.Key] * item.Value;
                        denominatorA += Math.Pow(item.Value, 2);
                        denominatorB += Math.Pow(arr_dict[j][item.Key], 2);
                    }
                    if (denominatorA == 0 || denominatorB == 0) //checking Division by zero
                        CosineSimilarity[i][j] = 0;
                    else
                        CosineSimilarity[i][j] = (float)(numerator / (Math.Sqrt(denominatorA) * Math.Sqrt(denominatorB)));
                }
            }
            UiInvoke(() => txtEditor.Clear());
            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - Cosine similarity on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));

        }
        private void cosine_button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FileBuff))
                MessageBox.Show("Please choose some DataSet by clicking on Browse button", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            else
            {
                //initials
                cosine_button.IsEnabled = false;
                jccard_button.IsEnabled = false;
                currentTime = string.Empty;
                flag = 2;
                stopWatch.Reset();
                stopWatch.Start();
                timer.Start();
                txtEditor.Clear();

                cosineReadData();

                Thread cosine_thread = new Thread(() => calc_CosineSimilarity(arr_dict));
                cosine_thread.Start();
                //cosine_thread.Join();

            }
        }

        void dt_Tick(object sender, EventArgs e)
        {
            if (stopWatch.IsRunning)
            {
                TimeSpan ts = stopWatch.Elapsed;
                currentTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                ClockTextBlock.Text = currentTime;
            }
        }

        private void calc_pageRank_jdistance()
        {
            stopWatch.Reset();
            stopWatch.Start();
            timer.Start();
            double[] PageRank;
            double rank = 0.0;
            double d = 0.85;
            PageRank = new double[linesNumber];
            for (int i = 0; i < Jdistance.Length; i++)
            {
                rank = 0;
                for (int j = 0; j < Jdistance[i].Length; j++)
                {
                    if (Jdistance[i][j] == 0)
                    {
                        rank += Jdistance[j][i] / linesNumber;
                        PageRank[i] = 1 - d + d * rank;
                    }
                    else
                    {
                        rank += Jdistance[i][j] / linesNumber;
                        PageRank[i] = 1 - d + d * rank;
                    }

                }
            }
            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - PageRank jaccard on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
            UiInvoke(() => txtEditor.Clear());
            for (int i = 0; i < PageRank.Length; i++)
                UiInvoke(() => txtEditor.Text += string.Format("{0} ", PageRank[i]));

        }

        private void calc_pageRank_cosineSimilarity()
        {
            stopWatch.Reset();
            stopWatch.Start();
            timer.Start();
            float[] PageRank;
            float rank = 0;
            float d = (float)0.85;
            PageRank = new float[linesNumber];
            for (int i = 0; i < linesNumber; i++)
            {
                rank = 0;
                for (int j = 0; j < linesNumber; j++)
                {
                    if (CosineSimilarity[i][j] == 0.0)
                    {
                        rank += CosineSimilarity[j][i] / linesNumber;
                        PageRank[i] = 1 - d + d * rank;
                    }
                    else
                    {
                        rank += CosineSimilarity[i][j] / linesNumber;
                        PageRank[i] = 1 - d + d * rank;
                    }

                }
            }
            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - PageRank Cosine on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
            UiInvoke(() => txtEditor.Clear());
            for (int i = 0; i < PageRank.Length; i++)
            {
                UiInvoke(() => txtEditor.Text += string.Format("{0} ", PageRank[i]));
            }
        }



        private void PageRank_button_Click(object sender, RoutedEventArgs e)
        {
            if (flag == 1)
            {
                Thread pagerank_jaccard_thread = new Thread(() => calc_pageRank_jdistance());
                pagerank_jaccard_thread.Start();
            }
            if (flag == 2)
            {
                Thread pagerank_cosine_thread = new Thread(() => calc_pageRank_cosineSimilarity());
                pagerank_cosine_thread.Start();
            }
        }

        private void kmeans_button_Click(object sender, RoutedEventArgs e)
        {
            string question = "How many clusters do you want to create?";
            Window2 win = new Window2(question);
            win.Show();
            
        }

    }


}
