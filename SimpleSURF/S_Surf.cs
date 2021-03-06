﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSURF
{
    public class S_Surf
    {
        private class MatchInfo
        {
            public int ind_1;
            public int ind_2;
            public double euclideanDist;

            public MatchInfo()
            {
                this.ind_1 = -1;
                this.ind_2 = -1;
                this.euclideanDist = -1.0;
            }
        }

        private double threshold;
        private int octaveStart;
        private int octaveEnd;
        private OctaveMap octMap;

        public S_Surf(int octaveStart, int octaveEnd, double threshold)
        {
            this.threshold = threshold;
            this.octaveStart = octaveStart;
            this.octaveEnd = octaveEnd;
        }

        private double getEuclideanDistance(double[] desc_1, double[] desc_2, int length)
        {
            double sum = 0;
            for (int i = 0; i < length; i++)
                sum += (desc_1[i] - desc_2[i]) * (desc_1[i] - desc_2[i]);

            return Math.Sqrt(sum);
        }

        public List<FeaturePoint> extractPoints(IntegImage img, int imgWidth, int imgHeight)
        {
            List<FeaturePoint> resPoints = new List<FeaturePoint>();

            this.octMap = new OctaveMap(
                octaveStart, octaveEnd, img, imgWidth, imgHeight);
            this.octMap.computeMap();

            for (int oct = octaveStart; oct <= octaveEnd; oct++)
            {
                for (int k = 0; k < OctaveMap.INTERVALS - 2; k++)
                {
                    OctaveLayer bot = octMap.map[oct - 1, k];
                    OctaveLayer mid = octMap.map[oct - 1, k + 1];
                    OctaveLayer top = octMap.map[oct - 1, k + 2];

                    for (int i = 0; i < mid.height; i++)
                        for (int j = 0; j < mid.width; j++)
                            if (octMap.pointIsExtremum(i, j, bot, mid, top, this.threshold))
                            {
                                octMap.pointIsExtremum(i, j, bot, mid, top, this.threshold);
                                resPoints.Add(
                                    new FeaturePoint(j, i, mid.scale, mid.radius, mid.signs[i, j]));
                            }
                }
            }
            return resPoints;
        }

        public void setDescriptor(FeaturePoint p, IntegImage img, int imgWidth, int imgHeight)
        {
            //Affects to the descriptor area
            const int haarScale = 20;
            //Side of the Haar wavelet
            int haarFilterSize = 2 * p.scale;
            //Result
            double[] desc = new double[FeaturePoint.DESC_SIZE];

            //Length of the side of the descriptor area
            int descSide = haarScale * p.scale;
            //Side of the quadrant in 4x4 grid
            int quadStep = descSide / 4;
            //Side of the sub-quadrant in 5x5 regular grid of quadrant
            int subQuadStep = quadStep / 5;

            int leftTop_row = p.y - (descSide / 2);
            int leftTop_col = p.x - (descSide / 2);

            int count = 0;

            for (int r = leftTop_row; r < leftTop_row + descSide; r += quadStep)
                for (int c = leftTop_col; c < leftTop_col + descSide; c += quadStep)
                {
                    double dx = 0;
                    double dy = 0;
                    double abs_dx = 0;
                    double abs_dy = 0;

                    for (int sub_r = r; sub_r < r + quadStep; sub_r += subQuadStep)
                        for (int sub_c = c; sub_c < c + quadStep; sub_c += subQuadStep)
                        {
                            //Approximate center of sub quadrant
                            int cntr_r = sub_r + subQuadStep / 2;
                            int cntr_c = sub_c + subQuadStep / 2;

                            //Left top point for Haar wavelet computation
                            int cur_r = cntr_r - haarFilterSize / 2;
                            int cur_c = cntr_c - haarFilterSize / 2;

                            //Gradients
                            double cur_dx = img.HaarWavelet_X(cur_r, cur_c, haarFilterSize);
                            double cur_dy = img.HaarWavelet_Y(cur_r, cur_c, haarFilterSize);

                            dx += cur_dx;
                            dy += cur_dy;
                            abs_dx += Math.Abs(cur_dx);
                            abs_dy += Math.Abs(cur_dy);
                        }

                    desc[count++] = dx;
                    desc[count++] = dy;
                    desc[count++] = abs_dx;
                    desc[count++] = abs_dy;
                }

            p.descriptor = desc;
        }

        private void normalizeDist(MatchInfo[] ar, int length)
        {
            double max = 0;

            //Find max value
            for (int i = 0; i < length; i++)
            {
                if (ar[i].euclideanDist >= 0)
                {
                    if (i == 0)
                    {
                        max = ar[i].euclideanDist;
                    }
                    else
                    {
                        if (ar[i].euclideanDist > max)
                            max = ar[i].euclideanDist;
                    }
                }
            }

            for (int i = 0; i < length; i++)
                if (max != 0 && ar[i].euclideanDist >= 0)
                    ar[i].euclideanDist = ar[i].euclideanDist / max;
        }

        public FeaturePoint[,] matchPoints(FeaturePoint[] points_1, int len_1,
            FeaturePoint[] points_2, int len_2, double threshold)
        {
            const double ratioThreshold = 0.8;

            int minLength = (len_1 < len_2) ? len_1 : len_2;

            bool isSwap = false;
            FeaturePoint[] p_1 = null;
            FeaturePoint[] p_2 = null;

            if (minLength == len_2)
            {
                p_1 = points_2;
                p_2 = points_1;

                int tmp = 0;
                tmp = len_1;
                len_1 = len_2;
                len_2 = tmp;
                isSwap = true;
            }
            else
            {
                p_1 = points_1;
                p_2 = points_2;
                isSwap = false;
            }

            //Matched points
            FeaturePoint[,] result = new FeaturePoint[minLength, 2];

            /* 
             * Stores matched points and their
             * normalized euclidean distances  ( [0..1] )
             * 1st point | 2nd point | distance
             */
            MatchInfo[] distMap = new MatchInfo[minLength];
            for (int i = 0; i < minLength; i++)
                distMap[i] = new MatchInfo();

            /*
             * Flag that points are matched
             */
            bool[] alreadyMatched = new bool[len_2];
            for (int i = 0; i < len_2; i++)
                alreadyMatched[i] = false;

            int countMatched = 0;

            for (int i = 0; i < len_1; i++)
            {
                double bestDist = -1;
                int bestIndex = -1;

                //Find the nearest point
                for (int j = 0; j < len_2; j++)
                    if (!alreadyMatched[j])
                        if (p_1[i].sign == p_2[j].sign)
                        {
                            double curDist = getEuclideanDistance(p_1[i].descriptor, p_2[j].descriptor,
                                FeaturePoint.DESC_SIZE);

                            if (bestDist == -1)
                            {
                                bestDist = curDist;
                                bestIndex = j;
                            }
                            else
                                if (curDist < bestDist)
                                {
                                    bestDist = curDist;
                                    bestIndex = j;
                                }
                        }
                /*
                 * Findes the 2nd nearest point
                 */
                double bestDist_2 = -1;

                for (int j = 0; j < len_2; j++)
                    if (!alreadyMatched[j])
                        if (p_1[i].sign == p_2[j].sign)
                        {
                            double curDist = getEuclideanDistance(p_1[i].descriptor, p_2[j].descriptor,
                                FeaturePoint.DESC_SIZE);

                            if (bestDist_2 < 0)
                                bestDist_2 = curDist;
                            else
                                if (curDist > bestDist && curDist < bestDist_2)
                                    bestDist_2 = curDist;
                        }
                /*
                 * FALSE DETECTION PRUNING
                 * If ratio (1st / 2nd) > 0.8 - this is the false detection
                 */
                if (bestDist_2 > 0 && bestDist >= 0)
                    if (bestDist / bestDist_2 < ratioThreshold)
                    {
                        distMap[countMatched].ind_1 = i;
                        distMap[countMatched].ind_2 = bestIndex;
                        distMap[countMatched].euclideanDist = bestDist;
                        countMatched++;
                        alreadyMatched[bestIndex] = true;
                    }
            }

            normalizeDist(distMap, minLength);

            //Pruning based on the custom threshold (0..1)
            int c = 0;
            for (int i = 0; i < minLength; i++)
                if (distMap[i].euclideanDist >= 0
                    && distMap[i].euclideanDist <= threshold)
                    if (!isSwap)
                    {
                        result[c, 0] = p_1[distMap[i].ind_1];
                        result[c, 1] = p_2[distMap[i].ind_2];
                        c++;
                    }
                    else
                    {
                        result[c, 1] = p_1[distMap[i].ind_1];
                        result[c, 0] = p_2[distMap[i].ind_2];
                        c++;
                    }

            return result;
        }
    }
}
