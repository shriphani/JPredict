#define DEBUG

// Lokan Samasthan Sukhino Bhavantu

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace JPredictTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Members

        private bool strokeInProgress = false;
        private Point lastStrokePoint;
        private LinkedList<Point> curStrokePoints = new LinkedList<Point>();

        #endregion

        #region Properties

        /// <summary>
        /// Holds the current stroke
        /// </summary>
        public Stroke CurStroke
        {
            get { return curStroke; }
            set { curStroke = value; }
        }
        private Stroke curStroke = new Stroke();

        /// <summary>
        /// Set of strokes for this character
        /// </summary>
        public LinkedList<Stroke> CharStrokes
        {
            get { return charStrokes; }
            set { charStrokes = value; }
        }
        private LinkedList<Stroke> charStrokes = new LinkedList<Stroke>();

        #endregion
        String trainSymbols = "あいうえおかきくけこさしすせそらりるれろまみめも";

        public Dictionary<String, Stroke[]> GlobalDataSet = new Dictionary<string, Stroke[]>();

        public MainWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            // Load files
            LoadData();
        }

        /// <summary>
        /// Init Stroke
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void canvas1_MouseDown(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            downTextBlock.Text = e.GetPosition(null).ToString();
#endif
            strokeInProgress = true;
            lastStrokePoint = e.GetPosition(null);

            // Set up new stroke
            CurStroke = new Stroke();
            CurStroke.strokeInit = lastStrokePoint;
            curStrokePoints.AddLast(lastStrokePoint);
        }


        /// <summary>
        /// Stoke in progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void canvas1_MouseMove(object sender, MouseEventArgs e)
        {
#if DEBUG
            moveTextBlock.Text = e.GetPosition(null).ToString();
#endif
            if (strokeInProgress)
            {
                Line strokeLine = new Line();
                strokeLine.StrokeThickness = 10;
                strokeLine.Stroke = Brushes.White;

                strokeLine.X1 = lastStrokePoint.X;
                strokeLine.Y1 = lastStrokePoint.Y;
                strokeLine.X2 = e.GetPosition(null).X;
                strokeLine.Y2 = e.GetPosition(null).Y;

                this.canvas1.Children.Add(strokeLine);
                lastStrokePoint = e.GetPosition(null);
                curStrokePoints.AddLast(lastStrokePoint);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void canvas1_MouseUp(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            upTextBlock.Text = e.GetPosition(null).ToString();
#endif
            strokeInProgress = false;
            curStrokePoints.AddLast(e.GetPosition(null));
            curStroke.strokeFinit = e.GetPosition(null);

            // compute centroid and finish recording data for this stroke.
            Point centr = ptScale(1.0 / curStrokePoints.Count(), curStrokePoints.Aggregate<Point>((sum, val) => ptSum(sum, val)));
            curStroke.centroid = centr;

            curStroke.strokeInit.X -= curStroke.centroid.X;
            curStroke.strokeInit.Y -= curStroke.centroid.Y;

            curStroke.strokeFinit.X -= curStroke.centroid.X;
            curStroke.strokeFinit.Y -= curStroke.centroid.Y;

            curStroke.centroid.X -= curStroke.centroid.X;
            curStroke.centroid.Y -= curStroke.centroid.Y;

            // normalize the stroke.
            curStroke.normalize();
            
            CharStrokes.AddLast(curStroke);

            // perform prediction
            string[] top3 = SymbolRank();
            if (top3.Length >= 1)
            {
                result1Button.Content = top3[0];
            }
            if (top3.Length >= 2)
            {
                result3Button.Content = top3[1];
            }
            if (top3.Length >= 3)
            {
                result2Button.Content = top3[2];
            }

            curStrokePoints.Clear();
        }

        /// <summary>
        /// Using the accumulated stroke sequence,
        /// Perform a lookup. return top-3 symbols
        /// </summary>
        /// <param name="curStroke"></param>
        /// <returns></returns>
        public string[] SymbolRank()
        {
            Dictionary<String, Double> symbolDist = new Dictionary<string, double>();
            foreach (string symbol in GlobalDataSet.Keys)
            {
                if (GlobalDataSet[symbol].Length >= CharStrokes.Count())
                {
                    symbolDist.Add(symbol, (CharStrokes.Zip(GlobalDataSet[symbol], (s1, s2) => strokeDist(s1, s2)).ToArray<double>()).Sum());
                }
            }
            var items = from k in symbolDist.Keys
                        orderby symbolDist[k] ascending
                        select k;

            return items.Take<String>(3).ToArray<String>();
        }


        /// <summary>
        /// Clear canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            canvas1.Children.Clear();
            CharStrokes.Clear();
            curStrokePoints.Clear();
        }


        #region Helpers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        private Point ptSum(Point pt1, Point pt2)
        {
            return new Point(pt1.X + pt2.X, pt1.Y + pt2.Y);
        }

        private Point ptScale(double n, Point pt)
        {
            return new Point(pt.X * n, pt.Y * n);
        }

        /// <summary>
        /// Given train symbols to look for, pick up the values and
        /// build the trained dataset.
        /// </summary>
        private void LoadData()
        {
            foreach (char symbol in this.trainSymbols)
            {
                StreamReader sr = new StreamReader(symbol + ".txt");
                LinkedList<Stroke> strokeList = new LinkedList<Stroke>();
                while (!sr.EndOfStream)
                {
                    Stroke stroke = new Stroke();
                    Point[] coords = new Point[3];
                    int i = 0;
                    string dataLine = sr.ReadLine();
                    foreach (string ptDataStr in dataLine.Split(':'))
                    {
                        if (!string.IsNullOrWhiteSpace(ptDataStr))
                        {
                            coords[i] = parsePoint(ptDataStr);
                            i++;
                        }
                    }
                    stroke.strokeInit = coords[0];
                    stroke.strokeFinit = coords[2];
                    stroke.centroid = coords[1];
                    strokeList.AddLast(stroke);
                }

                GlobalDataSet.Add(""+symbol, strokeList.ToArray<Stroke>());
            }
        }

        private double euclideanDist(Point p1, Point p2)
        {
            return Math.Sqrt(((p1.X - p2.X) * (p1.X - p2.X)) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        private double strokeDist(Stroke stroke1, Stroke stroke2)
        {
            return euclideanDist(stroke1.strokeInit, stroke2.strokeInit) +
                   euclideanDist(stroke1.strokeFinit, stroke2.strokeFinit);
        }

        private Point parsePoint(String ptStr)
        {
            String[] ptCoordStrs = ptStr.Split(',');
            return new Point(double.Parse(ptCoordStrs[0]), double.Parse(ptCoordStrs[1]));
        }

        #endregion
    }

    #region Helpers

    public class Stroke
    {
        public Point strokeInit;
        public Point centroid;
        public Point strokeFinit;

        public void normalize()
        {
            strokeInit.X -= centroid.X;
            strokeInit.Y -= centroid.Y;

            strokeInit.X /= strokeInit.X * strokeInit.X + strokeInit.Y * strokeInit.Y;
            strokeInit.Y /= strokeInit.X * strokeInit.X + strokeInit.Y * strokeInit.Y;

            strokeFinit.X -= centroid.X;
            strokeFinit.Y -= centroid.Y;

            strokeFinit.X /= strokeFinit.X * strokeFinit.X + strokeFinit.Y * strokeFinit.Y;
            strokeFinit.Y /= strokeFinit.X * strokeFinit.X + strokeFinit.Y * strokeFinit.Y;

            centroid.X = centroid.Y = 0;
        }
    }

    #endregion
}
