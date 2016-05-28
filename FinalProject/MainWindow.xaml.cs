using Microsoft.Win32;
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
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

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
            //TIMER
            timer.Tick += new EventHandler(dt_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
        }

        DispatcherTimer timer = new DispatcherTimer();
        Stopwatch stopWatch = new Stopwatch();
        string currentTime;

        float[][] JdistanceKmeans; //matrix of all jaccard distance of Kmeans values
        float[][] CosineDistanceKmeans; //matrix of all cosine distance of Kmeans values
        float[][] CosineDistancePR; //matrix of all cosine distance of PageRank values
        Dictionary<string, int>[] arrayDictionaries; //array of dictionaries --> to represent the vectors
        HashSet<string> hashSet = new HashSet<string>(); //HashSet of the file values
        int linesNumber; //size of rows
        string[][] FileMatrix; //matrix of the file readed
        string[] lines; //array of string - the lines of the file readed
        string FileBuff; //buffer of the file readed

        FileInfo fileLoaded = new FileInfo("file");
        Object thisLock = new Object(); //object lock critical section

        //INVOKE
        public static void UiInvoke(Action a)
        {
            Application.Current.Dispatcher.Invoke(a);
        }

        //TIMER
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

        //Radio button checked
        private void jaccard_Checked(object sender, RoutedEventArgs e)
        {
            Jaccard_radioButton.Foreground = Brushes.Blue;
            Jaccard_radioButton.Background = Brushes.Yellow;
            Cosine_radioButton.Foreground = Brushes.Black;
            Cosine_radioButton.Background = Brushes.White;
        }
        private void cosine_Checked(object sender, RoutedEventArgs e)
        {
            Cosine_radioButton.Foreground = Brushes.Blue;
            Cosine_radioButton.Background = Brushes.Yellow;
            Jaccard_radioButton.Foreground = Brushes.Black;
            Jaccard_radioButton.Background = Brushes.White;
        }

        //Open DataSet file
        private void Open_File_Click(object sender, RoutedEventArgs e)
        {
            Thread readFile = new Thread(() => dataSet_read_file());
            readFile.Start();
        }
        public async void dataSet_read_file()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    if (!fileLoaded.FullName.Equals(openFileDialog.FileName))
                    {
                        using (StreamReader sr = new StreamReader(openFileDialog.FileName))
                        {
                            fileLoaded = new FileInfo(openFileDialog.FileName);
                            UiInvoke(() => kmeans_button.IsEnabled = false);
                            UiInvoke(() => PageRank_button.IsEnabled = false);
                            FileBuff = await sr.ReadToEndAsync();
                            UiInvoke(() => txtEditor.Text = FileBuff);
                            UiInvoke(() => kmeans_button.IsEnabled = true);
                            UiInvoke(() => PageRank_button.IsEnabled = true);
                            //UiInvoke(() => txtEditor.TextChanged += (o, args) => kmeans_button.IsEnabled = true);
                            //UiInvoke(() => txtEditor.TextChanged += (o, args) => PageRank_button.IsEnabled = true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UiInvoke(() => txtEditor.Text = "Error: Could not read file.  Original error: " + ex.Message);
            }
        }

        //Normalization functions
        public void splitBySpacesAndLines(string text)
        {
            lines = text.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            linesNumber = lines.Length;
            FileMatrix = new string[linesNumber][];
            for (int i = 0; i < linesNumber; i++)
                FileMatrix[i] = lines[i].Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);
        }
        public void NormalizationData(string fileBuff)
        {
            Dictionary<string, int> init_dict = new Dictionary<string, int>(); //define defulat dictioanry for all the dataset

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
        private void ShowHsahSet()
        {
            string print = ">>>>>>>>>>  The DateSet values  <<<<<<<<<<\n";
            foreach (string i in hashSet)
                print += string.Format("{0} ", i);

            print += string.Format("\n\nThe DataSet include {0} values", hashSet.Count.ToString());
            UiInvoke(() => txtEditor.Text += print);
        }
        //#########################################################################  JACCARD DISTANCE  #########################################################################
        private void Jaccard_Kmeans()
        {
            stopWatch.Reset();
            stopWatch.Start();
            timer.Start();
            //Thread creation
            Thread normalizeThread = new Thread(() => NormalizationData(FileBuff));
            Thread jDistanceThread = new Thread(() => calc_JDistance());

            normalizeThread.Start();
            normalizeThread.Join();
            ShowHsahSet();
            jDistanceThread.Start();
        }
        public void calc_JDistance()
        {
            float intersect, union;
            JdistanceKmeans = new float[linesNumber][];
            for (int i = 0; i < linesNumber; i++)
            {
                JdistanceKmeans[i] = new float[linesNumber];
                for (int j = i; j < linesNumber; j++)
                {
                    intersect = 0;
                    union = 0;
                    foreach (KeyValuePair<string, int> item in arrayDictionaries[j])
                    {
                        if (arrayDictionaries[i][item.Key] > 0 && arrayDictionaries[j][item.Key] > 0)
                            intersect++;
                        if (arrayDictionaries[i][item.Key] > 0 || arrayDictionaries[j][item.Key] > 0)
                            union++;
                    }
                    JdistanceKmeans[i][j] = 1 - intersect / union;
                }
            }
            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; i > j; j++)
                    if (JdistanceKmeans[i][j] == 0)
                        JdistanceKmeans[i][j] = JdistanceKmeans[j][i];
            // After finish jaccard calculates
            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - Jaccard distance on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
            Thread KmeansByJaccard = new Thread(() => kmeans_by_Jaccard_algorithm());
            KmeansByJaccard.SetApartmentState(ApartmentState.STA);
            KmeansByJaccard.Start();
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

        //#########################################################################  COSINE DISTANCE  #########################################################################
        private void Cosine_Kmeans()
        {
            stopWatch.Reset();
            stopWatch.Start();
            timer.Start();
            //Thread creation
            Thread normalizeThread = new Thread(() => NormalizationData(FileBuff));
            Thread cosineDistanceThread = new Thread(() => calc_CosineDistance());

            normalizeThread.Start();
            normalizeThread.Join();
            ShowHsahSet();
            cosineDistanceThread.Start();
        }
        public void calc_CosineDistance()
        {
            double numerator;
            double denominatorA, denominatorB;
            CosineDistanceKmeans = new float[linesNumber][];

            for (int i = 0; i < linesNumber; i++)
            {
                CosineDistanceKmeans[i] = new float[linesNumber];
                for (int j = i; j < linesNumber; j++)
                {
                    numerator = 0;
                    denominatorA = 0;
                    denominatorB = 0;

                    foreach (KeyValuePair<string, int> item in arrayDictionaries[i])
                    {
                        numerator += arrayDictionaries[j][item.Key] * item.Value;
                        denominatorA += Math.Pow(item.Value, 2);
                        denominatorB += Math.Pow(arrayDictionaries[j][item.Key], 2);
                    }
                    if (denominatorA == 0 || denominatorB == 0) //checking Division by zero
                        CosineDistanceKmeans[i][j] = 0;
                    else
                        CosineDistanceKmeans[i][j] = 1 - ((float)(numerator / (Math.Sqrt(denominatorA) * Math.Sqrt(denominatorB))));
                }
            }
            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; i > j; j++)
                    if (CosineDistanceKmeans[i][j] == 0.0)
                        CosineDistanceKmeans[i][j] = CosineDistanceKmeans[j][i];
            // After finish cosine calculates
            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - Cosine distance on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
            Thread KmeansByCosine = new Thread(() => kmeans_by_Cosine_algorithm());
            KmeansByCosine.SetApartmentState(ApartmentState.STA);
            KmeansByCosine.Start();
        }
       
        //#########################################################################  PAGERANK  #########################################################################
        private void PageRank_button_Click(object sender, RoutedEventArgs e)
        {
            if (Jaccard_radioButton.IsChecked == false && Cosine_radioButton.IsChecked == false)
            {
                MessageBox.Show("Please choose before some distance algorithm", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (Jaccard_radioButton.IsChecked == true)
                {
                    kmeans_button.IsEnabled = false;
                    PageRank_button.IsEnabled = false;

                    txtEditor.Text = string.Format("Calculating Jaccard distances of '{0}' file ! Please wait . . .\n\n ", fileLoaded.Name);
                    //Jaccard_PageRank();
                }
                if (Cosine_radioButton.IsChecked == true)
                {
                    kmeans_button.IsEnabled = false;
                    PageRank_button.IsEnabled = false;

                    txtEditor.Text = string.Format("Calculating Cosine distances of '{0}' file ! Please wait . . .\n\n ", fileLoaded.Name);
                    Cosine_PageRank();
                }
            }
            //string question = "How many max ranked rows do you want to show?";
            //int kValue;
            //kInputWindow kInput = new kInputWindow(question, linesNumber);
            //kInput.ShowDialog();
            //if (kInput.DialogResult.HasValue && kInput.DialogResult.Value)
            //{
            //    kValue = Convert.ToInt32(kInput.Answer); // K input
            //    if (flag == 1)//Jaccard PageRank
            //    {
            //        Thread pagerank_jaccard_thread = new Thread(() => calc_pageRank_jdistance());
            //        pagerank_jaccard_thread.Start();
            //    }
            //    if (flag == 2) //Cosine PageRank
            //    {
            //        Thread pagerankCosineThread = new Thread(() => calc_PageRank_cosineDistance());
            //        pagerankCosineThread.Start();
            //    }
            }

        private void Cosine_PageRank()
        {
            //START TIMER
            stopWatch.Reset();
            stopWatch.Start();
            timer.Start();

            Thread normalizeThread = new Thread(() => NormalizationData(FileBuff));
            Thread cosinePRThread = new Thread(() => calc_PageRank_cosineDistance());

            normalizeThread.Start();
            normalizeThread.Join();
            ShowHsahSet();
            cosinePRThread.Start();   
        }
        //$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
        private void calc_PageRank_cosineDistance()
        {
            Dictionary<string, int> countValues = CreatCountItems();
            Dictionary<string, float> frequnceDict = initFreqDict(countValues);
            printInverseDict(frequnceDict);
            calc_CosineDistancePagerank(frequnceDict);
            //printMatrix(CosinePagerankMatrix);
            //PageRank_calculate(CosinePagerankMatrix, K);

            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - Cosine Distance on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
            Thread PageRankByCosine = new Thread(() => PageRank_by_Cosine_algorithm());
            PageRankByCosine.SetApartmentState(ApartmentState.STA);
            PageRankByCosine.Start();
        }
        private void PageRank_by_Cosine_algorithm()
        {
            string question = "How many max ranked rows do you want to show?";
            int kValue;
            kInputWindow kInput = new kInputWindow(question, linesNumber);
            kInput.ShowDialog();
            if (kInput.DialogResult.HasValue && kInput.DialogResult.Value)
            {
                //Start TIMER
                currentTime = string.Empty;
                stopWatch.Reset();
                stopWatch.Start();
                timer.Start();

                kValue = Convert.ToInt32(kInput.Answer); // K input
                Thread pagerankCosineThread = new Thread(() => PageRank_Cosine_Calculate(kValue));
                pagerankCosineThread.Start();
            }
        }
        private void PageRank_Cosine_Calculate(int K)
        {           
            double[] PageRank = new double[linesNumber];
            double[] newPageRank = new double[linesNumber];
            Dictionary<int, double> RankDictionary;
            double divEpsilon = 1.0, Epsilon = 1.0, newEpsilon = 0.0;
            double sumVector, sumPR;
            double d = 0.85;
            //************************  INIT  ************************
            double rank = (float)1 / linesNumber;
            for (int i = 0; i < linesNumber; i++)
            {
                sumPR = 0;
                for (int j = 0; j < linesNumber; j++)
                {
                    sumPR += CosineDistancePR[j][i];
                }
                PageRank[i] = rank * sumPR;
            }
            RankDictionary = initRankDictionary(PageRank);
            //************************************************
            int iteration = 1;
            string print = "########################################  PAGERANK BY COSINE DISTANCE:  ########################################\n";
            UiInvoke(() => txtEditor.Text = print);

            while (divEpsilon > 0.01)
            {
                print = string.Format("\n>>>>>>>>>>  Iteration number {0}  <<<<<<<<<<", iteration);
                UiInvoke(() => txtEditor.Text += print);

                //calc--> NewPageRank
                for (int i = 0; i < linesNumber; i++)
                    newPageRank[i] = (1 - d) + d * PageRank[i];
                sumVector = 0;
                for (int i = 0; i < linesNumber; i++)
                    sumVector += newPageRank[i];
                for (int i = 0; i < linesNumber; i++)
                    newPageRank[i] = newPageRank[i] / sumVector;

                //calc difference between the old pagerank and new pagerank
                newEpsilon = 0.0;
                for (int i = 0; i < linesNumber; i++)
                    newEpsilon += Math.Pow(newPageRank[i] - PageRank[i], 2);
                newEpsilon = Math.Sqrt(newEpsilon);
                divEpsilon = Math.Abs(newEpsilon - Epsilon);

                //copy - for the next iteration
                Epsilon = newEpsilon;
                for (int i = 0; i < linesNumber; i++)
                {
                    PageRank[i] = newPageRank[i];
                }

                //Update rankDictionary and print K max Ranked
                for (int i = 0; i < newPageRank.Length; i++)
                    RankDictionary[i] = newPageRank[i];
                printMaxRanks(RankDictionary, K);

                iteration++;
            }
            UiInvoke(() => txtEditor.Text += string.Format("\n\n>>>>>>>>>>  FINISH AFTER {0} ITERATIONS  <<<<<<<<<<", iteration));
            //Stop TIMER
            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - PageRank of Cosine on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
            UiInvoke(() => kmeans_button.IsEnabled = true);
            UiInvoke(() => PageRank_button.IsEnabled = true);
        }
        private void printMaxRanks(Dictionary<int, double> temp, int K)
        {
            var items = (from pair in temp
                         orderby pair.Value descending
                         select pair).ToDictionary(pair => pair.Key, pair => pair.Value).Take(K);

            // Display results.
            string print = string.Format("\nThe {0} max ranked lines are:\n", K);
            print += ("----------------------------------------------------------------------------------------------------------------------------------\n");
            foreach (KeyValuePair<int, double> pair in items)
            {
                print += string.Format("line {0}:   {1}\n", pair.Key, pair.Value);
                for (int i = 0; i < FileMatrix[pair.Key].Length; i++)
                    print += FileMatrix[pair.Key][i] + " ";
                print += ("\n----------------------------------------------------------------------------------------------------------------------------------\n");
            }
            UiInvoke(() => txtEditor.Text += print);
        }
        private Dictionary<int, double> initRankDictionary(double[] rank)
        {
            Dictionary<int, double> rankDict = new Dictionary<int, double>();
            for (int i = 0; i < rank.Length; i++)
                rankDict.Add(i, rank[i]);
            return rankDict;
        }
        private Dictionary<int, double> updateRankDictionary(double[] rank, Dictionary<int, double> rankDict)
        {
            for (int i = 0; i < rank.Length; i++)
                rankDict[i] = rank[i];
            return rankDict;
        }
        private void printInverseDict(Dictionary<string, float> frequnceDict)
        {
            string print = "\n\nThe Inverse frequency vector :\n";
            foreach (KeyValuePair<string, float> item in frequnceDict)
                print += string.Format("{0}-->{1}  |  ", item.Key, item.Value);
            UiInvoke(() => txtEditor.Text += print);
        }
        private void printMatrix(float[][] cosinePagerankMatrix)
        {
            string print = "\n\nCosine distance matrix:";
            for (int i = 0; i < linesNumber; i++)
            {
                print += "\n";
                for (int j = 0; j < linesNumber; j++)
                {
                    print += " | " + cosinePagerankMatrix[i][j];
                }
            }
            UiInvoke(() => txtEditor.Text += print);
        }
        //$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$

        private Dictionary<string, int> CreatCountItems()
        {
            Dictionary<string, int> CountItems = new Dictionary<string, int>();
            foreach (string key in hashSet)
            {
                CountItems.Add(key, 0);
            }
            for (int i = 0; i < linesNumber; i++)
            {
                foreach (string key in arrayDictionaries[i].Keys)
                {
                    if (arrayDictionaries[i][key] >= 1)
                        CountItems[key]++;
                }
            }
            return CountItems;
        }
        private Dictionary<string, float> initFreqDict(Dictionary<string, int> CountItems)
        {
            Dictionary<string, float> frequnceDict = new Dictionary<string, float>();
            foreach (string key in hashSet)
            {
                frequnceDict.Add(key, 0);
            }
            int maxValue = CountItems.Values.Max();
            foreach (KeyValuePair<string, int> item in CountItems)
            {
                frequnceDict[item.Key] = (float)maxValue / (float)CountItems[item.Key];
                //frequnceDict[item.Key] = (float)Math.Log(((double)maxValue / (double)CountItems[item.Key]) + 1);
            }
            return frequnceDict;
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
                //newPageRank = ResetRank(newPageRank);
                for (int i = 0; i < linesNumber; i++)
                {
                    for (int j = 0; j < linesNumber; j++)
                    {
                        newPageRank[i] += CosineDistanceKmeans[i][j] * PageRank[j];
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
                //newPageRank = bubbleRank(newPageRank);
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
        public void calc_CosineDistancePagerank(Dictionary<string, float> frequnceDict)
        {
            //Create an inverse frequency normaliztion vectors
            Dictionary<string, float>[] freqDictionaries;
            freqDictionaries = new Dictionary<string, float>[linesNumber];
            for (int i = 0; i < linesNumber; i++)
            {
                freqDictionaries[i] = new Dictionary<string, float>();
                foreach (KeyValuePair<string, float> item in frequnceDict)
                {
                    if (arrayDictionaries[i][item.Key] > 0)
                    {
                        freqDictionaries[i][item.Key] = frequnceDict[item.Key];
                    }
                    else
                        freqDictionaries[i][item.Key] = 0;
                }
            }

            double numerator;
            double denominatorA, denominatorB;
            CosineDistancePR = new float[linesNumber][];
            
            //000000000000000000000000000000000000000000000000000000000000000000
            //for (int i = 0; i < linesNumber; i++)
            //{
            //    jaccardPagerankMatrix[i] = new float[linesNumber];
            //    for (int j = i; j < linesNumber; j++)
            //{
            //    numerator = 0;
            //    denominator = 0;
            //    foreach (KeyValuePair<string, float> item in freqDictionaries[j])
            //    {
            //        numerator += freqDictionaries[i][item.Key] * freqDictionaries[j][item.Key];
            //        denominator += Math.Pow(freqDictionaries[i][item.Key] + freqDictionaries[j][item.Key], 2);
            //    }
            //    try
            //    {
            //        denominator = Math.Sqrt(denominator);
            //        jaccardPagerankMatrix[i][j] = 1 - (float)(numerator / denominator);
            //    }
            //    catch
            //    {
            //        MessageBox.Show("Error: division by zero", "Divide By Zero", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //000000000000000000000000000000000000000000000000000000000000000000

            for (int i = 0; i < linesNumber; i++)
            {
                CosineDistancePR[i] = new float[linesNumber];
                for (int j = i; j < linesNumber; j++)
                {
                    numerator = 0;
                    denominatorA = 0;
                    denominatorB = 0;

                    foreach (KeyValuePair<string, float> item in freqDictionaries[i])
                    {
                        numerator += freqDictionaries[j][item.Key] * item.Value;
                        denominatorA += Math.Pow(item.Value, 2);
                        denominatorB += Math.Pow(freqDictionaries[j][item.Key], 2);
                    }
                    if (denominatorA == 0 || denominatorB == 0) //checking Division by zero
                        CosineDistancePR[i][j] = 0;
                    else
                        CosineDistancePR[i][j] = 1 - ((float)(numerator / (Math.Sqrt(denominatorA) * Math.Sqrt(denominatorB))));
                }
            }

            for (int i = 0; i < linesNumber; i++)
                for (int j = 0; i > j; j++)
                    if (CosineDistancePR[i][j] == 0)
                        CosineDistancePR[i][j] = CosineDistancePR[j][i];
        }
        //#########################################################################  K-MEANS  #########################################################################
        private void kmeans_button_Click(object sender, RoutedEventArgs e)
        {
            if (Jaccard_radioButton.IsChecked == false && Cosine_radioButton.IsChecked == false)
            {
                MessageBox.Show("Please choose before some distance algorithm", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (Jaccard_radioButton.IsChecked == true)
                {
                    kmeans_button.IsEnabled = false;
                    PageRank_button.IsEnabled = false;

                    txtEditor.Text = string.Format("Calculating Jaccard distances of '{0}' file ! Please wait . . .\n\n ", fileLoaded.Name);
                    Jaccard_Kmeans();
                }
                if (Cosine_radioButton.IsChecked == true)
                {
                    kmeans_button.IsEnabled = false;
                    PageRank_button.IsEnabled = false;

                    txtEditor.Text = string.Format("Calculating Cosine distances of '{0}' file ! Please wait . . .\n\n ", fileLoaded.Name);
                    Cosine_Kmeans();
                }
            }
        }
    
        private void kmeans_by_Jaccard_algorithm()
        {
            string question = "How many clusters do you want to create?";
            int kValue;
            kInputWindow kInput = new kInputWindow(question, linesNumber);
            kInput.ShowDialog();
            if (kInput.DialogResult.HasValue && kInput.DialogResult.Value)
            {
                //Start TIMER
                currentTime = string.Empty;
                stopWatch.Reset();
                stopWatch.Start();
                timer.Start();

                kValue = Convert.ToInt32(kInput.Answer); // K input
                List<int>[] linesClusters; //array of list in size of K, each list have the lines of the cluster
                bool hasChange = true; //bool flag, if was a cluster change
                Dictionary<string, float>[] clustCentroid = new Dictionary<string, float>[kValue];               
                int iteration = 1;

                UiInvoke(() => txtEditor.Text = "######################################## K-MEANS BY JACCARD DISTANCE ########################################");
                // >>> INIT ITERATION<<<
                linesClusters = initRandom(kValue);
                Thread jaccardKmeansThread = new Thread(() => kmeans_jaccard(linesClusters, clustCentroid, kValue, iteration, hasChange));
                jaccardKmeansThread.Start();
            }
        }
        private void kmeans_by_Cosine_algorithm()
        {
            string question = "How many clusters do you want to create?";
            int kValue;
            kInputWindow kInput = new kInputWindow(question, linesNumber);
            kInput.ShowDialog();
            if (kInput.DialogResult.HasValue && kInput.DialogResult.Value)
            {
                //Start TIMER
                currentTime = string.Empty;
                stopWatch.Reset();
                stopWatch.Start();
                timer.Start();

                kValue = Convert.ToInt32(kInput.Answer); // K input
                List<int>[] linesClusters; //array of list in size of K, each list have the lines of the cluster
                bool hasChange = true; //bool flag, if was a cluster change
                Dictionary<string, float>[] clustCentroid = new Dictionary<string, float>[kValue];
                int iteration = 1;

                UiInvoke(() => txtEditor.Text = "######################################## K-MEANS BY COSINE DISTANCE ########################################");
                // >>> INIT ITERATION<<<
                linesClusters = initRandom(kValue);
                Thread cosineKmeansThread = new Thread(() => kmeans_cosine(linesClusters, clustCentroid, kValue, iteration, hasChange));
                cosineKmeansThread.Start();
            }
        }
        private void kmeans_jaccard(List<int>[] linesClusters, Dictionary<string, float>[] clustCentroid, int kValue, int iteration, bool hasChange)
        {
            firstInitJaccard(linesClusters, kValue);
            clustCentroid = initCentroids(linesClusters, kValue);
            while (hasChange && iteration < 100)
            {
                hasChange = UpdateClusteringJaccard(clustCentroid, linesClusters, kValue);
                clustCentroid = updateCentroids(clustCentroid, linesClusters, kValue);
                prints_rowClosest_centroidDifference(clustCentroid, linesClusters, kValue, iteration);
                iteration++;
            }
            UiInvoke(() => txtEditor.Text += string.Format("\n\n>>>>>>>>>>  FINISH AFTER {0} ITERATIONS  <<<<<<<<<<", iteration));
            //Stop TIMER
            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - K-Means of Jaccard on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
            UiInvoke(() => kmeans_button.IsEnabled = true);
            UiInvoke(() => PageRank_button.IsEnabled = true);
        }
        private void kmeans_cosine(List<int>[] linesClusters, Dictionary<string, float>[] clustCentroid, int kValue, int iteration, bool hasChange)
        {
            firstInitCosine(linesClusters, kValue);
            clustCentroid = initCentroids(linesClusters, kValue);
            while (hasChange && iteration < 100)
            {
                hasChange = UpdateClusteringCosine(clustCentroid, linesClusters, kValue);
                clustCentroid = updateCentroids(clustCentroid, linesClusters, kValue);
                prints_rowClosest_centroidDifference(clustCentroid, linesClusters, kValue, iteration);
                iteration++;
            }
            UiInvoke(() => txtEditor.Text += string.Format("\n\n>>>>>>>>>>  FINISH AFTER {0} ITERATIONS  <<<<<<<<<<", iteration));
            //Stop TIMER
            timer.Stop();
            stopWatch.Stop();
            UiInvoke(() => MessageBox.Show(String.Format("Finish - K-Means of Cosine on: {0}", ClockTextBlock.Text), "Thread", MessageBoxButton.OK, MessageBoxImage.Information));
            UiInvoke(() => kmeans_button.IsEnabled = true);
            UiInvoke(() => PageRank_button.IsEnabled = true);
        }
        private List<int>[] initRandom(int K)
        {
            bool randFlag; 
            string print = "\n>>>>>>>>>>  Init iteration  <<<<<<<<<<\n";
            Random random = new Random();
            List<int>[] linesClast = new List<int>[K];
            for (int i = 0; i < K; i++)
            {
                randFlag = true;
                int rand = random.Next(0, linesNumber);
                for (int j = 0; j < i; j++)
                {
                    if (linesClast[j].First() == rand)
                    {
                        i--;
                        randFlag = false;
                        break;      
                    }                        
                }
                if (randFlag)
                {
                    linesClast[i] = new List<int>();
                    linesClast[i].Add(rand);
                    print += string.Format("Line number {0} assigned to cluster number {1}\n", rand, i);
                }    
            }
            UiInvoke(() => txtEditor.Text += print);
            return linesClast;
        }
        private void firstInitCosine(List<int>[] linesClust, int K)
        {
            double min;
            int minRow, rightCluster;
            int row;
            for (int i = 0; i < linesNumber; i++)
            {
                minRow = linesClust[0].First();
                min = CosineDistanceKmeans[i][minRow];
                rightCluster = 0;
                for (int j = 1; j < K; j++)
                {
                    row = linesClust[j].First();
                    if (CosineDistanceKmeans[i][row] < min)
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

            string print = "\n\n>>>>>>>>>>  Initial placement  <<<<<<<<<<";
            int counter = 0;
            for (int i = 0; i < K; i++)
            {
                counter += linesClust[i].Count;
                print += string.Format("\nCluster {0} have {1} values.", i, linesClust[i].Count);
            }
            print += string.Format("\n\nTotal count of lines: {0}", counter);
            UiInvoke(() => txtEditor.Text += print);
        }
        private void firstInitJaccard(List<int>[] linesClust, int K)
        {
            double min;
            int minRow, rightCluster;
            int row;
            for (int i = 0; i < linesNumber; i++)
            {
                minRow = linesClust[0].First();
                min = JdistanceKmeans[i][minRow];
                rightCluster = 0;
                for (int j = 1; j < K; j++)
                {
                    row = linesClust[j].First();
                    if (JdistanceKmeans[i][row] < min)
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

            string print = "\n\n>>>>>>>>>>  Initial placement  <<<<<<<<<<";
            int counter = 0;
            for (int i = 0; i < K; i++)
            {
                counter += linesClust[i].Count;
                print += string.Format("\nCluster {0} have {1} values.", i, linesClust[i].Count);
            }
            print += string.Format("\n\nTotal count of lines: {0}", counter);
            UiInvoke(() => txtEditor.Text += print);
        }
        private Dictionary<string, float>[] initCentroids(List<int>[] linesClust, int K)
        {
            float sum = 0;
            float avg = 0;
            Dictionary<string, float>[] centroidClust = new Dictionary<string, float>[K];
            for (int i = 0; i < K; i++)
            {
                centroidClust[i] = new Dictionary<string, float>();
                foreach (string key in arrayDictionaries[0].Keys)
                {
                    sum = 0;
                    foreach (int item in linesClust[i])
                    {
                        sum += arrayDictionaries[item][key];
                    }
                    avg = sum / linesClust[i].Count;
                    centroidClust[i].Add(key, avg);
                }
            }
            return centroidClust;
        }
        private Dictionary<string, float>[] updateCentroids(Dictionary<string, float>[] centroidClust, List<int>[] linesClust, int K)
        {
            float sum = 0;
            float avg = 0;
            //Dictionary<string, float>[] oldCentroid = new Dictionary<string, float>[centroidClust.Length];
            //for (int i = 0; i < centroidClust.Length; i++)
            //{
            //    oldCentroid[i] = new Dictionary<string, float>();
            //    foreach (KeyValuePair<string, float> item in centroidClust[i])
            //        oldCentroid[i].Add(item.Key, item.Value);
            //}

            for (int i = 0; i < K; i++)
            {
                foreach (string key in arrayDictionaries[0].Keys)
                {
                    sum = 0;
                    foreach (int item in linesClust[i])
                    {
                        sum += arrayDictionaries[item][key];
                    }
                    if (linesClust[i].Count == 0)//checking devide by zero
                        avg = 0;
                    else
                        avg = sum / linesClust[i].Count;
                    centroidClust[i][key] = avg;
                }
            }
            //float distance;
            //string print = "\n";
            //for (int i = 0; i < K; i++)
            //{
            //    distance = calc_distance_centroids(oldCentroid[i], centroidClust[i]);
            //    print += string.Format("\nThe distance of centroid {0}: current centriod VS old centroid is {1}", i, distance);
            //}
            
            return centroidClust;
        }
        private bool UpdateClusteringCosine(Dictionary<string, float>[] centroids, List<int>[] lineClusters, int K)
        {
            float minimum, cosineValue;
            int rightCluster;
            Dictionary<int, int> removeValues = new Dictionary<int, int>();
            Dictionary<int, int> addValues = new Dictionary<int, int>();
            bool hasChange = false, changeCluster = false;

            for (int i = 0; i < K; i++)
            {
                foreach (int item in lineClusters[i])
                {
                    minimum = calcCosineDictionary(arrayDictionaries[item], centroids[i]);
                    rightCluster = i;
                    for (int j = 0; j < K; j++)
                    {
                        cosineValue = calcCosineDictionary(arrayDictionaries[item], centroids[j]);
                        if (cosineValue < minimum)
                        {
                            minimum = cosineValue;
                            rightCluster = j;
                            hasChange = true;
                            changeCluster = true;
                        }
                    }
                    if (changeCluster)
                    {
                        removeValues.Add(item, i);
                        addValues.Add(item, rightCluster);
                    }
                    changeCluster = false;
                }
            }
            foreach (KeyValuePair<int, int> element in addValues)
            {
                lineClusters[element.Value].Add(element.Key);
                lineClusters[removeValues[element.Key]].Remove(element.Key);
                // print += string.Format("\nRow number {0} passed from cluster {1} to cluster {2}.", element.Key, removeValues[element.Key], element.Value);
            }
            for (int i = 0; i < K; i++)
                if (lineClusters[i].Count == 0)
                    return false;

            return hasChange;
        }
        private bool UpdateClusteringJaccard(Dictionary<string, float>[] centroids, List<int>[] lineClusters, int K)
        {
            float minimum, jaccardValue;
            int rightCluster;
            Dictionary<int, int> removeValues = new Dictionary<int, int>();
            Dictionary<int, int> addValues = new Dictionary<int, int>();
            bool hasChange = false, changeCluster = false;

            for (int i = 0; i < K; i++)
            {
                foreach (int item in lineClusters[i])
                {
                    minimum = calcJaccardDictionary(arrayDictionaries[item], centroids[i]);
                    rightCluster = i;
                    for (int j = 0; j < K; j++)
                    {
                        jaccardValue = calcJaccardDictionary(arrayDictionaries[item], centroids[j]);
                        if (jaccardValue < minimum)
                        {
                            minimum = jaccardValue;
                            rightCluster = j;
                            hasChange = true;
                            changeCluster = true;
                        }
                    }
                    if (changeCluster)
                    {
                        removeValues.Add(item, i);
                        addValues.Add(item, rightCluster);
                    }
                    changeCluster = false;
                }
            }
            foreach (KeyValuePair<int, int> element in addValues)
            {
                lineClusters[element.Value].Add(element.Key);
                lineClusters[removeValues[element.Key]].Remove(element.Key);
                // print += string.Format("\nRow number {0} passed from cluster {1} to cluster {2}.", element.Key, removeValues[element.Key], element.Value);
            }
            for (int i = 0; i < K; i++)
                if (lineClusters[i].Count == 0)
                    return false;

            return hasChange;
        }
        private float calcJaccardDictionary(Dictionary<string, int> row, Dictionary<string, float> centroid)
        {
            double numerator = 0, denominator = 0;
            float Jdist = 0;
            foreach (KeyValuePair<string, int> element in row) //pass over each value of the row dictionary
            {
                numerator += row[element.Key] * centroid[element.Key];
                denominator += Math.Pow(row[element.Key] + centroid[element.Key], 2);
            }
            denominator = Math.Sqrt(denominator);
            try
            {
                return Jdist = 1 - (float)(numerator / denominator);
            }
            catch (DivideByZeroException)
            {
                MessageBox.Show("Error: division by zero", "Divide By Zero", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
        }
        private float calcCosineDictionary(Dictionary<string, int> row, Dictionary<string, float> centroid)
        {
            double numerator = 0, denominator = 0;
            double Ai = 0, Bi = 0;
            foreach (KeyValuePair<string, int> element in row)
            {
                numerator += row[element.Key] * centroid[element.Key];
                Ai += Math.Pow(row[element.Key], 2);
                Bi += Math.Pow(centroid[element.Key], 2);
            }
            denominator = Math.Sqrt(Ai) * Math.Sqrt(Bi);
            try
            {
                return 1 - (float)(numerator / denominator);
            }
            catch (DivideByZeroException)
            {
                MessageBox.Show("Error: division by zero", "Divide By Zero", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
        }
        private float calc_distance_centroids(Dictionary<string, float> oldCentroid, Dictionary<string, float> newCentroid)
        {
            float distance = 0;
            double numerator = 0, denominator = 0;
            double Ai = 0, Bi = 0;
            foreach (KeyValuePair<string, float> element in oldCentroid)
            {
                numerator += oldCentroid[element.Key] * newCentroid[element.Key];
                Ai += Math.Pow(oldCentroid[element.Key], 2);
                Bi += Math.Pow(newCentroid[element.Key], 2);
            }
            denominator = Math.Sqrt(Ai) * Math.Sqrt(Bi);
            try
            {
                distance = 1 - (float)(numerator / denominator);
            }
            catch (DivideByZeroException)
            {
                MessageBox.Show("Error: division by zero", "Divide By Zero", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
            return distance;
        }
        private void prints_rowClosest_centroidDifference(Dictionary<string, float>[] centroids, List<int>[] lineClusters, int K, int iteration)
        {
            string print = string.Format("\n\n>>>>>>>>>>  Iteration number {0}  <<<<<<<<<<", iteration);
            print += ("\n----------------------------------------------------------------------------------------------------------------------------------");
            int ClosestRow = 0;
            float minDistance = 0, distance;
            for (int i = 0; i < K; i++)
            {
                minDistance = calcCosineDictionary(arrayDictionaries[lineClusters[i][0]], centroids[i]);
                ClosestRow = lineClusters[i][0];
                foreach (int item in lineClusters[i])
                {
                    distance = calcCosineDictionary(arrayDictionaries[item], centroids[i]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        ClosestRow = item;
                    }
                }
                
                print += string.Format("\nCluster {0} have {1} values.", i, lineClusters[i].Count);
                print += string.Format("\nThe closest row to centriod {0} is row {1} and the distance is {2}",i, ClosestRow, minDistance);
                print += string.Format("\nRow {0} : ", ClosestRow);
                for (int j = 0; j < FileMatrix[ClosestRow].Length; j++)
                    print += string.Format("{0} ", FileMatrix[ClosestRow][j]);
                print += ("\n----------------------------------------------------------------------------------------------------------------------------------");
            }
            UiInvoke(() => txtEditor.Text += print);
        }

        
    }
}
