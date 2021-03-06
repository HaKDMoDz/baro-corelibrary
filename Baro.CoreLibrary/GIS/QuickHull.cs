﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Baro.CoreLibrary.GIS
{
    public static class QuickHull
    {
        public static List<Point> CH2(List<Point> points, bool removeFirst)
        {
            List<Point> vertices = new List<Point>();

            if (points.Count == 0)
            {
                return null;
            }
            else if (points.Count == 1)
            {
                // If it's a single point, return it
                vertices.Add(points[0]);
                return vertices;
            }


            Point leftMost = CH2Init(points);
            vertices.Add(leftMost);

            Point prev = leftMost;
            Point? next;
            double rot = 0;
            do
            {
                next = CH2Step(prev, points, ref rot);

                // If it's not the first vertex (leftmost) or we want spiral (instead of CH2)
                // remove it
                if (prev != leftMost || removeFirst)
                    points.Remove(prev);

                // If this isn't the last vertex, save it
                if (next.HasValue)
                {
                    vertices.Add(next.Value);
                    prev = next.Value;
                }

            } while (points.Count > 0 && next.HasValue && next.Value != leftMost);
            points.Remove(leftMost);

            return vertices;

        }

        private static Point CH2Init(List<Point> points)
        {
            // Initialization - Find the leftmost point
            Point leftMost = points[0];
            int leftX = leftMost.X;

            foreach (Point p in points)
            {
                if (p.X < leftX)
                {
                    leftMost = p;
                    leftX = p.X;
                }
            }

            return leftMost;
        }

        private static Point? CH2Step(Point currentPoint, List<Point> points, ref double rot)
        {
            double angle, angleRel, smallestAngle = 2 * Math.PI, smallestAngleRel = 4 * Math.PI;
            Point? chosen = null;
            double xDiff, yDiff;

            foreach (Point candidate in points)
            {
                if (candidate == currentPoint)
                    continue;

                xDiff = candidate.X - currentPoint.X;
                yDiff = -(candidate.Y - currentPoint.Y); //Y-axis starts on top
                angle = Math.Atan2(yDiff, xDiff) + Math.PI;

                // angleRel is the angle between the line and the rotated y-axis
                // y-axis has the direction of the last computed supporting line
                // given by variable rot.
                angleRel = 2 * Math.PI - (rot - angle);

                if (angleRel >= 2 * Math.PI)
                    angleRel -= 2 * Math.PI;

                if (angleRel < smallestAngleRel)
                {
                    smallestAngleRel = angleRel;
                    smallestAngle = angle;
                    chosen = candidate;
                }
            }

            // Save the smallest angle as the rotation of the y-axis for the
            // computation of the next supporting line.
            rot = smallestAngle;

            return chosen;
        }
    }
}
