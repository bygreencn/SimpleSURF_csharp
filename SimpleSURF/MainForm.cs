﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimpleSURF
{
    public partial class MainForm : Form
    {
        private Bitmap m_btmp_1;
        private Bitmap m_btmp_2;

        private Bitmap m_resultBtmp_1;
        private Bitmap m_resultBtmp_2;

        private IntegImage m_integImg_1;
        private IntegImage m_integImg_2;

        private Color m_positiveClr;
        private Color m_negativeClr;

        private string m_stats;

        S_Surf m_surf;

        List<FeaturePoint> m_points_1;
        List<FeaturePoint> m_points_2;

        List<FeaturePoint> m_matchedPoints_1;
        List<FeaturePoint> m_matchedPoints_2;

        //List<FeaturePoint> m_points_1;

        public MainForm()
        {
            InitializeComponent();

            m_positiveClr = Color.Yellow;
            m_negativeClr = Color.Blue;
        }

        private void convertToLuminosity(Bitmap b)
        {
            const double forRed = 0.21;
            const double forGreen = 0.72;
            const double forBlue = 0.07;

            for (int i = 0; i < b.Height; i++)
                for (int j = 0; j < b.Width; j++)
                {
                    Color c = b.GetPixel(j, i);
                    int lum = (int)(forRed * c.R + forGreen * c.G + forBlue * c.B);
                    Color newColor = Color.FromArgb(lum, lum, lum);

                    b.SetPixel(j, i, newColor);
                }
            
            /*
            for (int i = 0; i < b.Height; i++)
                for (int j = 0; j < b.Width; j++)
                {
                    Color c = b.GetPixel(j, i);
                    int value = (c.B + c.R + c.B) / 3;

                    b.SetPixel(j, i, Color.FromArgb(value, value, value));
                }
             * */
        }

        private void drawPoints(Bitmap btmp, List<FeaturePoint> points)
        {
            Graphics g = Graphics.FromImage(btmp);

            foreach (FeaturePoint fp in points)
            {
                Pen pen = new Pen((fp.sign > 0) ? m_positiveClr : m_negativeClr);
                g.DrawEllipse(pen, fp.x - fp.radius, fp.y - fp.radius,
                    fp.radius * 2, fp.radius * 2);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (m_btmp_1 == null && m_btmp_2 == null)
            {
                MessageBox.Show("Open images first");
                return;
            }



            m_resultBtmp_1 = (Bitmap)m_btmp_1.Clone();
            m_integImg_1 = new IntegImage(IntegImage.arrayFromBitmap(m_btmp_1));
            m_resultBtmp_2 = (Bitmap)m_btmp_2.Clone();
            m_integImg_2 = new IntegImage(IntegImage.arrayFromBitmap(m_btmp_2));

            double threshold = Double.Parse(this.txtBoxThreshold.Text);
            int botOctave = Int32.Parse(this.txtBoxBottomOct.Text);
            int topOctave = Int32.Parse(this.txtBoxTopOct.Text);

            m_surf = new S_Surf(botOctave, topOctave, threshold);

            m_points_1 = m_surf.extractPoints(
                m_integImg_1, m_integImg_1.width, m_integImg_1.height);
            m_points_2 = m_surf.extractPoints(
                m_integImg_2, m_integImg_2.width, m_integImg_2.height);



            /*
            double [,] ar = new double[,] {{1, 3, 4, 2, 5}, {7, 1, 2, 3, 4}, {2, 3, 5, 1, 2}};
            double[,] res = i.m_matrix;
            double ans = i.getRectSum(0, 0, 3, 3);
            ans = i.getRectSum(0, 0, 1, 1);
            ans = i.getRectSum(0, 0, 3, 1);
            ans = i.getRectSum(0, 0, 5, 5);
            ans = i.g
             */

            this.Text = m_points_1.Count.ToString() + " + " + m_points_2.Count.ToString()
                + " total feature points";

            /*
            Dictionary<int, int> statistic = new Dictionary<int, int>();

            for (int i = 0; i < m_points_1.Count; i++)
            {
                int specRad = m_points_1[i].radius;
                bool isMemorized = false;
                foreach (KeyValuePair<int, int> pair in statistic)
                    if (pair.Key == (specRad * 2 + 1))
                    {
                        isMemorized = true;
                        break;
                    }

                if (!isMemorized)
                {
                    int count = 0;
                    for (int j = 0; j < m_points_1.Count; j++)
                    {
                        if (m_points_1[j].radius == specRad)
                            count++;
                    }
                    statistic.Add(specRad * 2 + 1, count);
                }
            }

            m_stats = "";
            foreach(KeyValuePair<int, int> pair in statistic)
                m_stats += "Size : " + pair.Key.ToString() + " | " 
                    + pair.Value.ToString() + " points" + "\n";

            MessageBox.Show(m_stats);
            */

            foreach (FeaturePoint fp in m_points_1)
                m_surf.setDescriptor(fp, m_integImg_1, m_integImg_1.width, m_integImg_1.height);
            foreach (FeaturePoint fp in m_points_2)
                m_surf.setDescriptor(fp, m_integImg_2, m_integImg_2.width, m_integImg_2.height);

            m_resultBtmp_1 = (Bitmap)m_btmp_1.Clone();
            this.drawPoints(m_resultBtmp_1, m_points_1);
            this.picBox_1.Image = m_resultBtmp_1;
            this.picBox_1.Invalidate();

            m_resultBtmp_2 = (Bitmap)m_btmp_2.Clone();
            this.drawPoints(m_resultBtmp_2, m_points_2);
            this.picBox_2.Image = m_resultBtmp_2;
            this.picBox_2.Invalidate();
        }

        private void btnOpenImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            string pathToFile = openFileDialog.FileName;

            if (pathToFile.Equals(""))
                return;

            m_btmp_1 = new Bitmap(pathToFile);
            convertToLuminosity(m_btmp_1);
            this.picBox_1.Image = m_btmp_1;
        }

        private void btnStats_Click(object sender, EventArgs e)
        {
            MessageBox.Show(m_stats);
        }

        private void btnPosColor_Click(object sender, EventArgs e)
        {
            this.clrDial.ShowDialog();
            m_positiveClr = clrDial.Color;
            if (m_btmp_1 != null)
            {
                m_resultBtmp_1 = (Bitmap)m_btmp_1.Clone();
                this.drawPoints(m_resultBtmp_1, m_points_1);
                this.picBox_1.Image = m_resultBtmp_1;
                this.picBox_1.Invalidate();

                m_resultBtmp_2 = (Bitmap)m_btmp_2.Clone();
                this.drawPoints(m_resultBtmp_2, m_points_2);
                this.picBox_2.Image = m_resultBtmp_2;
                this.picBox_2.Invalidate();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.clrDial.ShowDialog();
            m_negativeClr = clrDial.Color;
            if (m_btmp_1 != null)
            {
                m_resultBtmp_1 = (Bitmap)m_btmp_1.Clone();
                this.drawPoints(m_resultBtmp_1, m_points_1);
                this.picBox_1.Image = m_resultBtmp_1;
                this.picBox_1.Invalidate();

                m_resultBtmp_2 = (Bitmap)m_btmp_2.Clone();
                this.drawPoints(m_resultBtmp_2, m_points_2);
                this.picBox_2.Image = m_resultBtmp_2;
                this.picBox_2.Invalidate();
            }
        }

        private void btnOpenImage_2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            string pathToFile = openFileDialog.FileName;

            if (pathToFile.Equals(""))
                return;

            m_btmp_2 = new Bitmap(pathToFile);
            convertToLuminosity(m_btmp_2);
            this.picBox_2.Image = m_btmp_2;
        }

        private void btnMatch_Click(object sender, EventArgs e)
        {
            if (m_points_1 != null && m_points_2 != null)
            {
                double matchTreshold = Double.Parse(this.txtBoxMatch.Text.ToString());

                FeaturePoint[,] matches = m_surf.matchPoints(m_points_1.ToArray(), m_points_1.Count,
                    m_points_2.ToArray(), m_points_2.Count, matchTreshold);

                m_matchedPoints_1 = new List<FeaturePoint>();
                m_matchedPoints_2 = new List<FeaturePoint>();

                for (int i = 0; i < matches.Length / 2; i++)
                {
                    if (matches[i, 0] == null)
                        break;

                    m_matchedPoints_1.Add(matches[i, 0]);
                    m_matchedPoints_2.Add(matches[i, 1]);
                }

                m_resultBtmp_1 = (Bitmap)m_btmp_1.Clone();
                this.drawPoints(m_resultBtmp_1, m_matchedPoints_1);
                this.picBox_1.Image = m_resultBtmp_1;
                this.picBox_1.Invalidate();

                m_resultBtmp_2 = (Bitmap)m_btmp_2.Clone();
                this.drawPoints(m_resultBtmp_2, m_matchedPoints_2);
                this.picBox_2.Image = m_resultBtmp_2;
                this.picBox_2.Invalidate();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Bitmap prevBtmp = new Bitmap(m_btmp_1.Width + m_btmp_2.Width,
                m_btmp_1.Height);

            Graphics g = Graphics.FromImage(prevBtmp);
            g.DrawImage(m_btmp_1, 0, 0, m_btmp_1.Width, m_btmp_1.Height);
            g.DrawImage(m_btmp_2, m_btmp_1.Width, 0, m_btmp_2.Width, m_btmp_2.Height);

            Pen p = new Pen(new SolidBrush(Color.Yellow), 2);

            for (int i = 0; i < m_matchedPoints_1.Count; i++)
            {
                g.DrawLine(p, 
                    m_matchedPoints_1[i].x, m_matchedPoints_1[i].y,
                    m_matchedPoints_2[i].x + m_btmp_1.Width,
                    m_matchedPoints_2[i].y);

                g.DrawEllipse(p, m_matchedPoints_1[i].x, m_matchedPoints_1[i].y,
                    2, 2);
                g.DrawEllipse(p,
                    m_matchedPoints_2[i].x + m_btmp_1.Width,
                    m_matchedPoints_2[i].y,
                    2, 2);
            }

            PreviewForm pf = new PreviewForm();
            pf.Image = prevBtmp;

            pf.Show();
        }
    }
}
