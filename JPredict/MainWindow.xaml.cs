// Lokan Samasthan Sukhino Bhavantu

#define Debug

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JPredict
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
            get { return curStroke;  }
            set { curStroke = value; }
        }
        private Stroke curStroke=new Stroke();

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

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();   
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

            // compute centroid and finish recording data for this stroke.
            Point centr = ptScale(1.0/curStrokePoints.Count(), curStrokePoints.Aggregate<Point>((sum, val) => ptSum(sum, val)));
            curStroke.centroid = centr;

            curStroke.strokeFinit = e.GetPosition(null);
            CharStrokes.AddLast(curStroke);

            curStrokePoints.Clear();
        }




        /// <summary>
        /// Clear canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            canvas1.Children.Clear();
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

        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //strokeFinished_button_Click(null,null);
        }

        private void strokeFinished_button_Click(object sender, RoutedEventArgs e)
        {
            String trainChar = trainLabel.Text;
            StreamWriter sw = new StreamWriter(trainChar + ".txt");

            foreach (Stroke stroke in CharStrokes)
            {
                stroke.normalize();
                //sw.Write("(");
                sw.Write(stroke.strokeInit);
                sw.Write(":");
                sw.Write(stroke.centroid);
                sw.Write(":");
                sw.Write(stroke.strokeFinit);
                sw.WriteLine();
                sw.Flush();
            }
            sw.Close();
            canvas1.Children.Clear();
            CharStrokes.Clear();
        }
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
