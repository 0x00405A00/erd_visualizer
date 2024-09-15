
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ERD_Visualizer
{
    public class GeometryHelper
    {
        public static (object affectedSegment,List<(Point, BOX_POSITION)> intersections) GetIntersectionPoints(PathGeometry pathGeometry, Rectangle rectangle)
        {
            List<(Point, BOX_POSITION)> intersectionPoints = new List<(Point, BOX_POSITION)>();
            object affectedObj = null;
            Rect rectBounds = new Rect(
                Canvas.GetLeft(rectangle),
                Canvas.GetTop(rectangle),
                rectangle.Width,
                rectangle.Height);

            foreach (PathFigure figure in pathGeometry.Figures)
            {
                Point startPoint = figure.StartPoint;

                foreach (PathSegment segment in figure.Segments)
                {
                    if (segment is LineSegment lineSegment)
                    {
                        Point endPoint = lineSegment.Point;
                        GetLineRectangleIntersections(startPoint, endPoint, rectBounds, intersectionPoints);
                        startPoint = endPoint;
                        affectedObj = segment;
                    }
                    else if (segment is BezierSegment bezierSegment)
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            return (affectedObj, intersectionPoints);
        }
        [Flags]
        public enum BOX_POSITION : int
        {
            TOP = 1,
            BOTTOM = 2,
            LEFT = 4,
            RIGHT = 8,
        }
        public static Rect GetBounds(Rectangle rectangle)
        {
            return new Rect(
                Canvas.GetLeft(rectangle),
                Canvas.GetTop(rectangle),
                rectangle.Width,
                rectangle.Height);
        }
        public static double GetDistance(Point point1, Point point2)
        {
            double x = 0;
            double y = 0;
            if (point1.X > point2.X)
            {
                x = point1.X - point2.X;
            }
            else
            {
                x = point2.X - point1.X;
            }
            if(point1.Y>point2.Y)
            {
                y= point1.Y - point2.Y;
            }
            else
            {
                y = point2.Y - point1.Y;
            }
            return (x+y);
        }
        public static Point GetMidPoint(Point point1, Point point2)=> new Point((point1.X+ point2.X)/2, (point1.Y + point2.Y) / 2) ;
        public static bool IsRectWithinBounds(Rectangle bounds, Rectangle rect)
        {
            
            return IsRectWithinBounds(bounds.ToRect(), rect.ToRect());
        }
        public static bool IsRectWithinBounds(Rect bounds,Rect rect) => bounds.IntersectsWith(rect);

        private static void GetLineRectangleIntersections(Point p1, Point p2, Rect rect, List<(Point, BOX_POSITION)> intersections)
        {
            Point? intersection;

            // Check each side of the rectangle
            if ((intersection = GetIntersection(p1, p2, rect.TopLeft, rect.TopRight)).HasValue)
                intersections.Add((intersection.Value, BOX_POSITION.TOP));

            if ((intersection = GetIntersection(p1, p2, rect.TopRight, rect.BottomRight)).HasValue)
                intersections.Add((intersection.Value, BOX_POSITION.RIGHT));

            if ((intersection = GetIntersection(p1, p2, rect.BottomRight, rect.BottomLeft)).HasValue)
                intersections.Add((intersection.Value, BOX_POSITION.BOTTOM));

            if ((intersection = GetIntersection(p1, p2, rect.BottomLeft, rect.TopLeft)).HasValue)
                intersections.Add((intersection.Value, BOX_POSITION.LEFT));
        }

        private static Point? GetIntersection(Point p1, Point p2, Point p3, Point p4)
        {
            double a1 = p2.Y - p1.Y;
            double b1 = p1.X - p2.X;
            double c1 = a1 * p1.X + b1 * p1.Y;

            double a2 = p4.Y - p3.Y;
            double b2 = p3.X - p4.X;
            double c2 = a2 * p3.X + b2 * p3.Y;

            double delta = a1 * b2 - a2 * b1;

            if (delta == 0)
            {
                return null; // Parallel lines
            }

            double x = (b2 * c1 - b1 * c2) / delta;
            double y = (a1 * c2 - a2 * c1) / delta;

            if (IsPointOnLineSegment(p1, p2, new Point(x, y)) && IsPointOnLineSegment(p3, p4, new Point(x, y)))
            {
                return new Point(x, y);
            }

            return null;
        }

        private static bool IsPointOnLineSegment(Point p1, Point p2, Point p)
        {
            return Math.Min(p1.X, p2.X) <= p.X && p.X <= Math.Max(p1.X, p2.X) &&
                   Math.Min(p1.Y, p2.Y) <= p.Y && p.Y <= Math.Max(p1.Y, p2.Y);
        }

        public static Point? GetAbsolutePositionOfChild(Canvas canvas, UIElement childElement)
        {

            if (!canvas.Children.Contains(childElement))
            {
                return null;
            }

            Point absolutePosition = new Point();
            Point relativePoint = childElement.TranslatePoint(new Point(0, 0), canvas);
            absolutePosition = new Point(
                relativePoint.X + Canvas.GetLeft(childElement),
                relativePoint.Y + Canvas.GetTop(childElement)
            );

            return absolutePosition;
        }
        public static bool IsRectangleNearBounds(Canvas canvas, Rectangle rectangle, double proximityOffset = 300)
        {
            var absolutePositionOfRectangle = GetAbsolutePositionOfChild(canvas, rectangle);
            if (absolutePositionOfRectangle == null)
                return false;

            var maxXFromRectangle = (absolutePositionOfRectangle.Value.X + rectangle.Width);
            var maxYFromRectangle = (absolutePositionOfRectangle.Value.Y + rectangle.Height);

            var upperLeftCorner = absolutePositionOfRectangle.Value;
            var upperRightCorner = new Point(maxXFromRectangle, absolutePositionOfRectangle.Value.Y);
            var lowerLeftCorner = new Point(absolutePositionOfRectangle.Value.X, maxYFromRectangle);
            var lowerRightCorner = new Point(maxXFromRectangle, maxYFromRectangle);

            var nearBoundsTopLeft = !IsPointWithinCanvasBounds(canvas, upperLeftCorner, proximityOffset);
            var nearBoundsTopRight = !IsPointWithinCanvasBounds(canvas, upperRightCorner, proximityOffset);
            var nearBoundsBottomLeft = !IsPointWithinCanvasBounds(canvas, lowerLeftCorner, proximityOffset);
            var nearBoundsBottomRight = !IsPointWithinCanvasBounds(canvas, lowerRightCorner, proximityOffset);

            Debug.WriteLine($"Top Left Corner is near bounds: {nearBoundsTopLeft}");
            Debug.WriteLine($"Top Right Corner is near bounds: {nearBoundsTopRight}");
            Debug.WriteLine($"Bottom Left Corner is near bounds: {nearBoundsBottomLeft}");
            Debug.WriteLine($"Bottom Right Corner is near bounds: {nearBoundsBottomRight}");

            return (nearBoundsTopLeft || nearBoundsTopRight || nearBoundsBottomLeft || nearBoundsBottomRight);
        }

        public static bool IsPointWithinCanvasBounds(Canvas canvas, Point point, double offset)
        {
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;

            double minX = 0 + offset;
            double maxX = canvasWidth - offset;
            double minY = 0 + offset;
            double maxY = canvasHeight - offset;

            string conditionString = $"({point.X} >= {minX} && {point.X} <= {maxX} && {point.Y} >= {minY} && {point.Y} <= {maxY})";
            Debug.WriteLine(conditionString);

            return (point.X >= minX && point.X <= maxX &&
                point.Y >= minY && point.Y <= maxY);
        }
        public static double Radians(Point a,Point b)
        {

            double deltaX = a.X > b.X ? a.X - b.X : b.X - a.X;
            double deltaY = a.Y > b.Y ? a.Y - b.Y : b.Y - a.Y;

            double angleInRadians = Math.Atan2(deltaY, deltaX);

            return angleInRadians;
        }
        public static double Degrees(Point a,Point b)
        {
            double angleInRadians = Radians(a,b);

            double angleInDegrees = angleInRadians * (180 / Math.PI);
            return angleInDegrees;
        }
        public static (Point upperLeft, Point upperRight, Point lowerLeft, Point lowerRight) GetRectangleCornerCoords(Canvas canvas, Rectangle rectangle)
        {

            var absolutePositionOfRectangle = new Point(Canvas.GetLeft(rectangle), Canvas.GetTop(rectangle));
            var minXFromRectangle = (absolutePositionOfRectangle.X);
            var minYFromRectangle = (absolutePositionOfRectangle.Y);
            var maxXFromRectangle = (absolutePositionOfRectangle.X + rectangle.Width);
            var maxYFromRectangle = (absolutePositionOfRectangle.Y + rectangle.Height);

            return (
                new Point(minXFromRectangle, minYFromRectangle), 
                new Point(maxXFromRectangle, minYFromRectangle), 
                new Point(minXFromRectangle, maxYFromRectangle), 
                new Point(maxXFromRectangle, maxYFromRectangle)
                );
        }
        public static bool IsRectangleOutOfBounds(Canvas canvas, Rectangle rectangle)
        {
            var absolutePositionOfRectangle = GetAbsolutePositionOfChild(canvas, rectangle);
            if (absolutePositionOfRectangle == null)
                return false;

            var maxXFromRectangle = (absolutePositionOfRectangle.Value.X + rectangle.Width);
            var maxYFromRectangle = (absolutePositionOfRectangle.Value.Y + rectangle.Height);

            var upperRightCorner = new Point(maxXFromRectangle, absolutePositionOfRectangle.Value.Y);
            var lowerLeftCorner = new Point(absolutePositionOfRectangle.Value.X, maxYFromRectangle);

            var outsideBoundsLeft = IsPointWithinCanvasBounds(canvas, upperRightCorner, 20);
            var oudsideBoundsRight = IsPointWithinCanvasBounds(canvas, lowerLeftCorner, 20);
            Debug.WriteLine($"IsPointWithinCanvasBounds: {outsideBoundsLeft}");
            Debug.WriteLine($"IsPointWithinCanvasBounds: {oudsideBoundsRight}");
            return (!outsideBoundsLeft || !oudsideBoundsRight);
        }
        public static Rect CalculateBoundingBox(Canvas canvas, Point point, (double X, double Y) offsets)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X + offsets.X);
            maxY = Math.Max(maxY, point.Y + offsets.Y);

            var canvasTransform = canvas.LayoutTransform as TransformGroup;
            if (canvasTransform != null)
            {
                var translateTransform = canvasTransform.Children.OfType<TranslateTransform>().FirstOrDefault();
                if (translateTransform != null)
                {
                    minX += -translateTransform.X;
                    minY += -translateTransform.Y;
                    maxX += -translateTransform.X;
                    maxY += -translateTransform.Y;
                }
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
        public static Point GetPointAlongPath(Point startPoint, IEnumerable<Point> segments, double distance)
        {
            double accumulatedDistance = 0;
            foreach (var segment in segments)
            {
                var segmentLength = CalculateSegmentLength(startPoint, segment);
                if (accumulatedDistance + segmentLength >= distance)
                {
                    var t = (distance - accumulatedDistance) / segmentLength;
                    return new Point(
                        startPoint.X + t * (segment.X - startPoint.X),
                        startPoint.Y + t * (segment.Y - startPoint.Y)
                    );
                }
                accumulatedDistance += segmentLength;
                startPoint = segment;
            }

            return startPoint;
        }

        public static double CalculateSegmentLength(Point startPoint, Point endPoint)
        {
            return Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));
        }

        public static double CalculateBezierLength(Point startPoint, Point controlPoint1, Point controlPoint2, Point endPoint)
        {
            var n = 100;
            var length = 0.0;
            var previousPoint = startPoint;

            for (int i = 1; i <= n; i++)
            {
                var t = (double)i / n;
                var currentPoint = BezierInterpolation(startPoint, controlPoint1, controlPoint2, endPoint, t);
                length += CalculateSegmentLength(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }

            return length;
        }

        public static Point BezierInterpolation(Point start, Point control1, Point control2, Point end, double t)
        {
            var x = Math.Pow(1 - t, 3) * start.X + 3 * t * Math.Pow(1 - t, 2) * control1.X + 3 * Math.Pow(t, 2) * (1 - t) * control2.X + Math.Pow(t, 3) * end.X;
            var y = Math.Pow(1 - t, 3) * start.Y + 3 * t * Math.Pow(1 - t, 2) * control1.Y + 3 * Math.Pow(t, 2) * (1 - t) * control2.Y + Math.Pow(t, 3) * end.Y;
            return new Point(x, y);
        }
        public static double CalculatePathLength(Point startPoint, IEnumerable<Point> segments)
        {
            double pathLength = 0;
            Point previousPoint = startPoint;

            foreach (var segment in segments)
            {
                pathLength += CalculateSegmentLength(previousPoint, segment);
                previousPoint = segment;
            }

            return pathLength;
        }
        public static List<Point> GetPathPoints(Path path)
        {
            var pathGeometry = path.Data as PathGeometry;
            var pathFigure = pathGeometry.Figures.FirstOrDefault();
            if (pathFigure == null)
                return new List<Point>();

            var points = new List<Point> { pathFigure.StartPoint };
            foreach (var segment in pathFigure.Segments)
            {
                if (segment is LineSegment lineSegment)
                {
                    points.Add(lineSegment.Point);
                }
                else if (segment is BezierSegment bezierSegment)
                {
                    points.Add(bezierSegment.Point1);
                    points.Add(bezierSegment.Point2);
                    points.Add(bezierSegment.Point3);
                }
            }

            return points;
        }
        public static Point GetNearestIntersectionPoint(Rect fromRect, Rect toRect)
        {
            var fromCenter = new Point(fromRect.Left + fromRect.Width / 2, fromRect.Top + fromRect.Height / 2);
            var toCenter = new Point(toRect.Left + toRect.Width / 2, toRect.Top + toRect.Height / 2);

            var angle = Math.Atan2(toCenter.Y - fromCenter.Y, toCenter.X - fromCenter.X);

            var offsetX = fromRect.Width / 2 * Math.Cos(angle);
            var offsetY = fromRect.Height / 2 * Math.Sin(angle);

            var intersection = new Point(fromCenter.X + offsetX, fromCenter.Y + offsetY);

            if (intersection.X < fromRect.Left)
                intersection.X = fromRect.Left;
            else if (intersection.X > fromRect.Right)
                intersection.X = fromRect.Right;

            if (intersection.Y < fromRect.Top)
                intersection.Y = fromRect.Top;
            else if (intersection.Y > fromRect.Bottom)
                intersection.Y = fromRect.Bottom;

            return intersection;
        }

        public static (bool isLeft, bool isAbove) GetDirection(Point pos1, Point pos2) => ((pos2.X < pos1.X), ((pos2.Y < pos1.Y)));
        public static PathGeometry RectangleToPathGeometry(Rectangle rect)
        {
            PathGeometry pathGeometry = new PathGeometry();
            PathFigure figure = new PathFigure();
            figure.StartPoint = new Point(rect.Margin.Left, rect.Margin.Top);

            LineSegment topLine = new LineSegment(new Point(rect.Margin.Left + rect.Width, rect.Margin.Top), true);
            LineSegment rightLine = new LineSegment(new Point(rect.Margin.Left + rect.Width, rect.Margin.Top + rect.Height), true);
            LineSegment bottomLine = new LineSegment(new Point(rect.Margin.Left, rect.Margin.Top + rect.Height), true);
            LineSegment leftLine = new LineSegment(new Point(rect.Margin.Left, rect.Margin.Top), true);

            figure.Segments.Add(topLine);
            figure.Segments.Add(rightLine);
            figure.Segments.Add(bottomLine);
            figure.Segments.Add(leftLine);

            pathGeometry.Figures.Add(figure);

            return pathGeometry;
        }

        static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
    }
}
