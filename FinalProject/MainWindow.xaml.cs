﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Input;

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
        Dictionary<string, int>[] arrayDictionaries;
        HashSet<string> hashSet =new HashSet<string>();
        int linesNumber; //size of rows
        string[][] FileMatrix; //matrix of the file readed
        string[] lines; //array of string - the lines of the file readed
       
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
                NormalizationData(FileBuff);
                jccard_button.IsEnabled = true;
                cosine_button.IsEnabled = true;
                lines = FileBuff.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                linesNumber = lines.Length;
            }
        }
        public void splitBySpacesAndLines(string text)
        {
            lines = text.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            linesNumber = lines.Length;
            FileMatrix = new string[linesNumber][];
            for (int i = 0; i < linesNumber; i++)
                FileMatrix[i] = lines[i].Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);
        }
        public void calc_JDistance()
        {
            int intersect = 0;
            for (int i = 0; i < arrayDictionaries.Length; i++)
            {
                for (int j = i; j < arrayDictionaries.Length; j++)
                {
                    intersect = 0;
                    foreach (KeyValuePair<string, int> item in arrayDictionaries[j])
                    {
                        if (arrayDictionaries[i][item.Key] > 0 && arrayDictionaries[j][item.Key] > 0)
                            intersect += 1;
                    }
                    Jdistance[i][j] = intersect / arrayDictionaries.Length;
                }
            }
            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; j < linesNumber; j++)
                    if (Jdistance[i][j] == 0 && i != j)
                        Jdistance[i][j] = Jdistance[j][i];

            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - Jaccard distance on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
            UiInvoke(() => txtEditor.Text = String.Join(" ", Jdistance[0].Select(p => p.ToString()).ToArray()));
        }
            //IEnumerable<string> union, intersect;
            //float interCount = 0, unionCount = 0;
            //float[][] tempDis;
            //string[][] temp;
            //lock (thisLock)
            //{
            //    temp = FileMatrix;
            //    tempDis = Jdistance;
            //}

            //for (int i = start; i < end; i++)
            //{
            //    Jdistance[i] = new float[linesNumber];
            //    for (int j = i; j < linesNumber; j++)
            //    {
            //        union = temp[i].Union(temp[j]);
            //        unionCount = union.Count<string>();
            //        intersect = temp[i].Intersect(temp[j]);
            //        interCount = intersect.Count<string>();

            //        if (unionCount != 0)
            //            tempDis[i][j] = 1 - (interCount / unionCount);
            //        else
            //            tempDis[i][j] = 0;
            //    }
            //}
            //lock (thisLock)
            //{
            //    Array.Copy(tempDis, start, Jdistance, start, end - start);
            //    threadCounter--;
            //    UiInvoke(() => txtEditor.Text += "Thread number " + numOfThread + " is finish\n");
            //    if (threadCounter == 0)
            //    {
            //        for (int i = 0; i < linesNumber; i++)
            //            for (int j = 0; j < linesNumber; j++)
            //                if (Jdistance[i][j] == 0 && i != j)
            //                    Jdistance[i][j] = Jdistance[j][i];

            //        timer.Stop();
            //        stopWatch.Stop();
            //        UiInvoke(() => MessageBox.Show(String.Format("Finish - Jaccard distance on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
            //        UiInvoke(() => txtEditor.Text = String.Join(" ", Jdistance[0].Select(p => p.ToString()).ToArray()));
            //        threadCounter = 3;
            //    }
            //}
       
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
                for (int  i = 0 ; i < linesNumber ; i++ )
                {
                    Jdistance[i] = new float[linesNumber];
                }

                //Thread creation
                Thread tt1 = new Thread(() => calc_JDistance());

                tt1.Start();

            }
        }
        private void ShowHsahSet()
        {
            string text_show = null;
            text_show = "The hash set include the values : ";
            foreach (string i in hashSet)
                text_show += string.Format("{0} ", i);

            text_show += string.Format("\nThe hash set include {0} values", hashSet.Count.ToString());
            UiInvoke(() => txtEditor.Text = text_show);
        }
        public void NormalizationData(string fileBuff)
        {
            Dictionary<string, int> init_dict = new Dictionary<string, int>();//define defulat dictioanry for all the dataset

            splitBySpacesAndLines(FileBuff);

            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; j < FileMatrix[i].Length; j++)
                    hashSet.Add(FileMatrix[i][j]);
            foreach (string key in hashSet)
                init_dict[key] = 0;

            arrayDictionaries = new Dictionary<string, int>[linesNumber];
            for (int i = 0; i < linesNumber; i++)
                arrayDictionaries[i] = new Dictionary<string, int>(init_dict);// init array of dictionaris by the file 

            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; j < FileMatrix[i].Length; j++)
                    if (arrayDictionaries[i].ContainsKey(FileMatrix[i][j]))
                        arrayDictionaries[i][FileMatrix[i][j]] += 1;// cehck if the key is exsit in the line and up the value of the key
            
        }
        public void calc_CosineSimilarity(Dictionary<string, int>[] arr_dict)
        {
            ShowHsahSet();
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
                        CosineSimilarity[i][j] = 1 - ((float)(numerator / (Math.Sqrt(denominatorA) * Math.Sqrt(denominatorB))));
                }
            }
            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; j < linesNumber; j++)
                    if (CosineSimilarity[i][j] == 0.0 && i != j)
                        CosineSimilarity[i][j] = CosineSimilarity[j][i];
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
               // cosineReadData();

                Thread cosine_thread = new Thread(() => calc_CosineSimilarity(arrayDictionaries));
                cosine_thread.Start();

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
            double d = 0.85;
            double divEpsilon = 1.0, Epsilon = 1.0, newEpsilon = 0.0;
            double[] PageRank = new double[linesNumber];
            double[] newPageRank = new double[linesNumber];
            for (int i = 0; i < linesNumber; i++)
            {
                PageRank[i] = (float)1 / linesNumber;
            }
            UiInvoke(() => txtEditor.Clear());
            int iteration = 0;
            string print;
            while (divEpsilon > 0.01)
            {
                print = string.Format("\n\nIteration number {0}: \nThe 5 max pageRank values are:\n", iteration);
                newEpsilon = 0.0;
                newPageRank = ResetRank(newPageRank);
                for (int i = 0; i < linesNumber; i++)
                {
                    for (int j = 0; j < linesNumber; j++)
                    {
                        newPageRank[i] +=  (Jdistance[i][j] * PageRank[j]);
                    }
                }
                for (int k = 0; k < linesNumber; k++)
                    newEpsilon += (double)Math.Pow(newPageRank[k] - PageRank[k], 2);
                newEpsilon = Math.Sqrt(newEpsilon);
                divEpsilon = (double)Math.Abs(newEpsilon - Epsilon) / (double)Epsilon;
                Epsilon = newEpsilon;
                for (int i = 0; i < linesNumber; i++)
                {
                    PageRank[i] = newPageRank[i];
                }
                newPageRank = bubbleRank(newPageRank);
                UiInvoke(() => txtEditor.Text += print);
                for (int i = 0; i < 5; i++)
                {
                    UiInvoke(() => txtEditor.Text += "\n" + newPageRank[i].ToString());
                }
                iteration++;


            }
            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - PageRank jaccard on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));

        }
        private void calc_pageRank_cosineSimilarity()
        {
            stopWatch.Reset();
            stopWatch.Start();
            timer.Start();
            double d = 0.85;
            double divEpsilon = 1.0, Epsilon = 1.0, newEpsilon = 0.0;
            double[] PageRank = new double[linesNumber];
            double[] newPageRank = new double[linesNumber];
            for (int i = 0; i < linesNumber; i++)
            {
                PageRank[i] = (float)1 / linesNumber;
            }
            UiInvoke(() => txtEditor.Clear());
            int iteration = 0;
            string print;
            while (divEpsilon > 0.01)
            {
                print = string.Format("\n\nIteration number {0}: \nThe 5 max pageRank values are:\n", iteration);
                newEpsilon = 0.0;
                newPageRank = ResetRank(newPageRank);
                for (int i = 0; i < linesNumber; i++)
                {
                    for (int j = 0; j < linesNumber; j++)
                    {
                        newPageRank[i] +=  CosineSimilarity[i][j] * PageRank[j];
                    }
                }
                for (int k = 0; k < linesNumber; k++)
                    newEpsilon += (double)Math.Pow(newPageRank[k] - PageRank[k], 2);
                newEpsilon = Math.Sqrt(newEpsilon);
                divEpsilon = (double)Math.Abs(newEpsilon - Epsilon) / (double)Epsilon;
                Epsilon = newEpsilon;
                for (int i = 0; i < linesNumber; i++)
                {
                    PageRank[i] = newPageRank[i];
                }
                newPageRank = bubbleRank(newPageRank);
                UiInvoke(() => txtEditor.Text += print);
                for (int i = 0; i < 5; i++)
                {
                    UiInvoke(() => txtEditor.Text += "\n" + newPageRank[i].ToString());
                }
                iteration++;


            }
            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - PageRank jaccard on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));

        }
        private double[] ResetRank(double[] temp)
        {
            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] = 0.0;
            }
            return temp;
        }
        private double[] bubbleRank(double[] temp)
        {
            double t;
            for (int i = linesNumber - 1; i > 0; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    if (temp[j] < temp[j + 1])
                    {
                        t = temp[j + 1];
                        temp[j + 1] = temp[j];
                        temp[j] = t;
                    }

                }
            }
            return temp;
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
            if (string.IsNullOrEmpty(FileBuff))
                MessageBox.Show("Please choose some DataSet by clicking on Browse button", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            else
            {
                Dictionary<string, int>[] arrayDictionary;
                Dictionary<string, float>[] clustCentroid;
                arrayDictionary = KmeansReadData(FileBuff);
                string question = "How many clusters do you want to create?";
                int kValue;
                kInputWindow kInput = new kInputWindow(question, linesNumber);
                kInput.ShowDialog();
                if (kInput.DialogResult.HasValue && kInput.DialogResult.Value)
                {
                    kValue = Convert.ToInt32(kInput.Answer);
                    List<int>[] linesClusters = new List<int>[kValue];
                    linesClusters = initRandom(kValue);
                    firstInit(linesClusters, kValue);
                    clustCentroid = calcCentroids(linesClusters, arrayDictionary, kValue);
                }
            }
        }
        private Dictionary<string, int>[] KmeansReadData(string FileBuff)
        {
            splitBySpacesAndLines(FileBuff);

            HashSet<string> hash = new HashSet<string>();
            Dictionary<string, int> init_dict = new Dictionary<string, int>();//define defulat dictioanry for all the dataset

            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; j < FileMatrix[i].Length; j++)
                    hash.Add(FileMatrix[i][j]);

            string text_show = null;
            text_show = "The DataSet include the values : ";
            foreach (string i in hash)
                text_show += string.Format("{0} ", i);

            text_show += string.Format("\nThe DataSet include {0} values\n", hash.Count.ToString());
            UiInvoke(() => txtEditor.Text = text_show);

            foreach (string key in hash)
                init_dict[key] = 0;

            arrayDictionaries = new Dictionary<string, int>[linesNumber];
            for (int i = 0; i < linesNumber; i++)
                arrayDictionaries[i] = new Dictionary<string, int>(init_dict);// init array of dictionaris by the file 

            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; j < FileMatrix[i].Length; j++)
                    if (arrayDictionaries[i].ContainsKey(FileMatrix[i][j]))
                        arrayDictionaries[i][FileMatrix[i][j]] += 1;// cehck if the key is exsit in the line and up the value of the key
            return arrayDictionaries;
        }
        private List<int>[] initRandom(int K)
        {
            string print = "\n>>>>>>>>>>Init iteration<<<<<<<<<<\n";
            Random random = new Random();
            List<int>[] linesClast = new List<int>[K];
            for (int i = 0; i < K; i++)
            {
                int rand = random.Next(0, linesNumber);
                linesClast[i] = new List<int>();
                linesClast[i].Add(rand);
                print += string.Format("Line number {0} assigned to cluster number {1}\n", rand, i);
            }
            txtEditor.Text += print;
            return linesClast;
        }
        private void firstInit(List<int>[] linesClust, int K)
        {
            double min;
            int minRow, rightCluster;
            int row;
            for (int i = 0; i < linesNumber; i++)
            {
                minRow = linesClust[0].First();
                min = CosineSimilarity[i][minRow];
                rightCluster = 0;
                for (int j = 1; j < K; j++)
                {
                    row = linesClust[j].First();
                    if (CosineSimilarity[i][row] < min)
                        rightCluster = j;
                }
                bool flag = true;
                for (int n = 0; n < K; n++)
                    if (i == linesClust[n].First())
                    {
                        flag = false;
                        break;
                    }
                if (flag)
                    linesClust[rightCluster].Add(i);
            }

            string print = "\n>>>>>>>>>>First iteration<<<<<<<<<<";
            int counter = 0;
            for (int i = 0; i < K; i++)
            {
                counter += linesClust[i].Count;
                print += string.Format("\nCluster {0} have {1} values.", i, linesClust[i].Count);
            }
            print += string.Format("\n\nCount of lines: {0}", counter);
            txtEditor.Text += print;
        }
        private Dictionary<string, float>[] calcCentroids(List<int>[] linesClust, Dictionary<string, int>[] arrDict, int K)
        {
            float sum=0;
            float avg=0;
            Dictionary<string, float>[] centroidClust = new Dictionary<string, float>[K];
            for (int i = 0; i < K; i++)
            {
                centroidClust[i] = new Dictionary<string, float>();
                foreach (string key in arrDict[0].Keys)
                {
                    sum = 0;
                    foreach (int item in linesClust[i])
                    {
                        sum += arrDict[item][key];
                    }
                    avg = sum / linesClust[i].Count;
                    centroidClust[i].Add(key, avg);
                }
            }
            string print = "\n\n>>>>>>>>>>The centroids<<<<<<<<<<";
            for (int i = 0; i < K; i++)
            {
                print += string.Format("\nCentroid of cluster {0}:\n", i);
                foreach (KeyValuePair<string, float> element in centroidClust[i])
                {
                    print += string.Format(" {0}->{1} ", element.Key, element.Value);
                }
            }
            txtEditor.Text += print;
            return centroidClust;
        }
    }
}
