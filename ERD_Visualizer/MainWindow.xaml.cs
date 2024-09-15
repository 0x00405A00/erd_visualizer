//#define RELATIONSHIP_LINE_CORRECTION_DEBUG
//#define RELATIONSHIP_LINE_HORIZONTAL_VERTICAL_CORRECTION_DEBUG
//#define RELATIONSHIP_LINE_HORIZONTAL_HORIZONTAL_CORRECTION_DEBUG
#define USEV2RELATIONSHIPLINE
using ERD_Visualizer.Model;
using System.Diagnostics;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ERD_Visualizer
{
    public partial class MainWindow : Window
    {
        private bool _enableGrid = false;
        private bool _moveLines = false;
        const int DiagramCanvasCellSize = 20;
        static SolidColorBrush RelationshipLineIntersectionCorrectionSpaceHelperPointColor = new SolidColorBrush(Color.FromRgb(235, 210, 70));
        static SolidColorBrush RelationshipLineIntersectionCorrectionSpaceHelperPoint2Color = new SolidColorBrush(Color.FromRgb(235, 54, 70));
        static Point InvalidPoint = new Point(0, 0);
        const double RelationshipLineIntersectionCorrectionSpaceValue = 20.0;
        const double CanvasZoomFactor = 1.1;
        private int DiagramCanvasRows = 0;
        private int DiagramCanvasColumns = 0;
        private int DiagramCanvasRowsOffset = 0;
        private int DiagramCanvasColumnsOffset = 0;

        private bool _isSelectionPan;
        private Point _selectionPanPos;
        const int SelectionRectZIndex = 0;
        static SolidColorBrush SelectionRectColor = new SolidColorBrush(Color.FromArgb(93,116, 219, 237));
        private Rectangle _selectionRect;
        private TextBlock _selectionRectTextblock;

        private Line _alignHelperLinesVertical = null;
        private Line _alignHelperLinesHorizontal = null;
        private Ellipse _alignHelperEllipse = null;
        private Line _alignHelperEllipseDegreeLinePrev = null;
        private Line _alignHelperEllipseDegreeLineNext = null;

        private List<EntityUiModel> _entities = new List<EntityUiModel>();
        private List<Relationship> _relationships { get; set; } = new List<Relationship>();
        private EntityUiModel _selectedEntity;
        private bool _isDragging;
        private bool _isPanning;
        private Point _lastPosition;
        private Point LastPosition => _lastPosition;
        public bool IsMultiselectedEnabled { get; set; }    

        private TranslateTransform _translateTransform;
        private Point _lastPositionPaning;
        private EntityUiModel _tempStartEntity;
        private Path _tempRelationshipPath; 
        private Point _currentMousePosition;
        private List<Ellipse> _intersectionPoints = new List<Ellipse>();

        private Path _draggedPath;
        private Point _mouseDownPosition;
        private Point _originalPathStartPoint;
        private Point _originalPathEndPoint;

        private Point? _dragStartPoint = null;
        private Ellipse _selectedControlPoint = null;
        private Path _selectedPath = null;
        private int _selectedIndex = -1;
        private List<Point> _pathPoints = new List<Point>();

        private List<Ellipse> _controlPoints = new List<Ellipse>();

        public MainWindow()
        {
            InitializeComponent();
            _translateTransform = new TranslateTransform();

            DiagramCanvas.Loaded += DiagramCanvas_Loaded;
            DiagramCanvas.PreviewMouseWheel += DiagramCanvas_PreviewMouseWheel;
            DiagramCanvas.MouseMove += DiagramCanvas_MouseMove;
            DiagramCanvas.MouseLeftButtonDown += DiagramCanvas_MouseLeftButtonDown;
            DiagramCanvas.MouseLeftButtonUp += DiagramCanvas_MouseLeftButtonUp;
            DiagramCanvas.MouseRightButtonDown += DiagramCanvas_MouseRightButtonDown;
            DiagramCanvas.MouseRightButtonUp += DiagramCanvas_MouseRightButtonUp;
            DiagramCanvas.RenderTransform = _translateTransform;//for paning

            InitializeTestData();
            EntitiesListBox.ItemsSource = _entities;
            this.WindowState = WindowState.Maximized;
        }

        private void InitializeTestData()
        {
            var entity1 = new Entity
            {
                Name = "Entity1",
                Properties = new Dictionary<string, string>
                {
                    { "PropertyA", "TypeA" },
                    { "PropertyB", "TypeB" }
                }
            };

            var entity2 = new Entity
            {
                Name = "Entity2",
                Properties = new Dictionary<string, string>
                {
                    { "PropertyC", "TypeC" },
                    { "PropertyD", "TypeD" }
                }
            };

            var entity3 = new Entity
            {
                Name = "Entity3",
                Properties = new Dictionary<string, string>
                {
                    { "PropertyE", "TypeE" },
                    { "PropertyF", "TypeF" }
                }
            };

            /*StartAddingRelationship(entity)

            var actions = new List<EntityUiModel.ContextMenuItem>() { new EntityUiModel.ContextMenuItem("Relation add",) };*/

            var entityUiModel1 = EntityUiModel.Create(entity1, new Point(100, 160));
            var entityUiModel2 = EntityUiModel.Create(entity2, new Point(1800, 170));
            var entityUiModel3 = EntityUiModel.Create(entity3, new Point(420, 420));

            _relationships.Add(new Relationship
            {
                Source = entityUiModel1,
                Target = entityUiModel2,
                SourceProperty = "PropertyA",
                TargetProperty = "PropertyC",
                StartText = "",
                MidText = "1:N",
                EndText = ""
            });

            _relationships.Add(new Relationship
            {
                Source = entityUiModel2,
                Target = entityUiModel3,
                SourceProperty = "PropertyD",
                TargetProperty = "PropertyF",
                StartText = "",
                MidText = "1:1",
                EndText = ""
            });

            _relationships.Add(new Relationship
            {
                Source = entityUiModel3,
                Target = entityUiModel1,
                SourceProperty = "PropertyE",
                TargetProperty = "PropertyB",
                StartText = "",
                MidText = "N:1",
                EndText = ""
            });

            _entities.Add(entityUiModel1);
            _entities.Add(entityUiModel2);
            _entities.Add(entityUiModel3);
        }

        private void MenuItem_ShowAllEntities_Click(object sender, RoutedEventArgs e)
        {
            Rect bounds = CalculateEntitiesBoundingBox(DiagramCanvas);

            double canvasWidth = DiagramCanvas.ActualWidth;
            double canvasHeight = DiagramCanvas.ActualHeight;

            double scaleX = bounds.Width/ bounds.Left;
            double scaleY = bounds.Height/ bounds.Top;

            double zoom = Math.Min(scaleX, scaleY);

            Point pos = new Point(bounds.Left, bounds.Top);
            ZoomToPosition(DiagramCanvas,DiagramCanvasScrollViewer, pos, zoom,true);
        }

        private void RenderGrid()
        {
            var gridRows = DiagramCanvas.Children.OfType<FrameworkElement>().Where(x => x.Tag == GridTagObjectRow).ToList();
            var gridCols = DiagramCanvas.Children.OfType<FrameworkElement>().Where(x => x.Tag == GridTagObjectColumn).ToList();

            var oldRows = DiagramCanvasRows;
            var oldColumns = DiagramCanvasColumns;
            var newRows = (int)(DiagramCanvas.ActualHeight / DiagramCanvasCellSize);
            var newColumns = (int)(DiagramCanvas.ActualWidth / DiagramCanvasCellSize);
            bool expand = false;
            if (DiagramCanvasRows == 0)
            {
                DiagramCanvasRows = newRows;
                DiagramCanvasColumns = newColumns;
                expand = true;
            }
            if (!expand)
            {
                if (newRows > DiagramCanvasRows)
                {
                    DiagramCanvasRowsOffset = DiagramCanvasRows;
                    DiagramCanvasRows = newRows;
                    expand = true;
                }
                if (newColumns > DiagramCanvasColumns)
                {
                    DiagramCanvasColumnsOffset = DiagramCanvasColumns;
                    DiagramCanvasColumns = newColumns;
                    expand = true;
                }
            }
            if (expand)
            {
                DrawGridRows(DiagramCanvasCellSize, DiagramCanvasRows, DiagramCanvasRowsOffset);
                DrawGridColumns(DiagramCanvasCellSize, DiagramCanvasColumns, DiagramCanvasColumnsOffset);
            }
            Debug.WriteLine($"DiagramCanvasRows: {DiagramCanvasRows}[{DiagramCanvasRowsOffset}], DiagramCanvasColumns: {DiagramCanvasColumns} [{DiagramCanvasColumnsOffset}]");
            Debug.WriteLine($"Old DiagramCanvasRows: {oldRows}, Old DiagramCanvasColumns: {oldColumns}, tagged Canvas Child Count: {gridRows.Count}/{gridCols.Count}");

            foreach (var row in gridRows)
            {
                row.Visibility = _enableGrid ? Visibility.Visible : Visibility.Hidden;
            }
            foreach (var col in gridCols)
            {
                col.Visibility = _enableGrid ? Visibility.Visible : Visibility.Hidden;
            }
        }
        private void EntitiesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EntitiesListBox.SelectedItem != null)
            {
                EntityUiModel selectedEntity = (EntityUiModel)EntitiesListBox.SelectedItem;

                ZoomAndFocusOnEntity(selectedEntity);
            }
        }

        private void ZoomAndFocusOnEntity(EntityUiModel entity)
        {
            ZoomToPosition(DiagramCanvas,DiagramCanvasScrollViewer,entity.Position,1,true);
        }
        public Rect CalculateEntitiesBoundingBox(Canvas canvas)
        {
            var minXEntity = _entities.OrderBy(x => x.Position.X).First();
            var maxXEntity = _entities.OrderByDescending(x => x.Position.X).First();
            var minYEntity = _entities.OrderBy(x => x.Position.Y).First();
            var maxYEntity = _entities.OrderByDescending(x => x.Position.Y).First();
            var width = ((maxXEntity.Position.X + maxXEntity.Box.Width) - minXEntity.Position.X);
            var height = ((maxYEntity.Position.Y + maxYEntity.Box.Height) - minYEntity.Position.Y);
            return new Rect(minXEntity.Position.X,minYEntity.Position.Y, width, height);
        }

        private void DiagramCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            RenderGrid();

            CreateEntitiesOnCanvas();
            DrawRelationships();
        }
        private void DiagramCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {

                double zoomFactor = e.Delta > 0 ? CanvasZoomFactor : 1.0 / CanvasZoomFactor;

                Point mousePosition = e.GetPosition(DiagramCanvas);
                ZoomToPosition(DiagramCanvas,DiagramCanvasScrollViewer,mousePosition,zoomFactor,false);

                e.Handled = true;
            }
        }

        private void ZoomToPosition(Canvas canvas, ScrollViewer scrollViewer,Point positionToZoom, double zoomFactor, bool setFixedZoom)
        {

            var transformGroup = canvas.LayoutTransform as TransformGroup;
            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(1, 1));
                transformGroup.Children.Add(new TranslateTransform(0, 0));
                canvas.LayoutTransform = transformGroup;
            }

            var scaleTransform = transformGroup.Children[0] as ScaleTransform;
            var translateTransform = transformGroup.Children[1] as TranslateTransform;

            double newScaleX = setFixedZoom ? zoomFactor : scaleTransform.ScaleX * zoomFactor;
            double newScaleY = setFixedZoom ? zoomFactor : scaleTransform.ScaleY * zoomFactor;
            Debug.WriteLine($"{nameof(ZoomToPosition)}: {zoomFactor}, direction of zoom: {(zoomFactor<1?"out":"in")}");

            double deltaX = positionToZoom.X * (setFixedZoom ? zoomFactor : (1 - zoomFactor));
            double deltaY = positionToZoom.Y * (setFixedZoom ? zoomFactor : (1 - zoomFactor));

            scaleTransform.ScaleX = newScaleX;
            scaleTransform.ScaleY = newScaleY;
            Debug.WriteLine($"{nameof(ZoomToPosition)}: scaleTransform.ScaleX={scaleTransform.ScaleX}, newScaleX:{newScaleX}");
            Debug.WriteLine($"{nameof(ZoomToPosition)}: scaleTransform.ScaleY={scaleTransform.ScaleY}, newScaleY:{newScaleY}");

            translateTransform.X -= deltaX;
            translateTransform.Y -= deltaY;
            Debug.WriteLine($"{nameof(ZoomToPosition)}: translateTransform.X={translateTransform.X}, deltaX:{deltaX}");
            Debug.WriteLine($"{nameof(ZoomToPosition)}: translateTransform.Y={translateTransform.Y}, deltaY:{deltaY}");
            scrollViewer.ScrollToHorizontalOffset(setFixedZoom ? deltaX : (scrollViewer.HorizontalOffset - deltaX < 0 ? 0 : scrollViewer.HorizontalOffset - deltaX));
            scrollViewer.ScrollToVerticalOffset(setFixedZoom ? deltaY: (scrollViewer.VerticalOffset - deltaY < 0 ? 0 : scrollViewer.VerticalOffset - deltaY));
        }

        private void DiagramCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(DiagramCanvas);
            _selectedEntity = _entities.FirstOrDefault(entity =>
                mousePos.X >= entity.Position.X && mousePos.X <= entity.Position.X + EntityUiModel.BoxSizeWidth &&
                mousePos.Y >= entity.Position.Y && mousePos.Y <= entity.Position.Y + (EntityUiModel.GetBoxSize(entity.Properties.Count).Height));

            var foundPaths = DiagramCanvas.Children.OfType<Path>().ToList();    
            var foundEllipses = DiagramCanvas.Children.OfType<Ellipse>()
                .Where(ellipse => ellipse.Tag is TaggingObject tagging && tagging.TagObject is Relationship)
                .Select(x=> (x, GeometryHelper.GetDistance(new Point(Canvas.GetLeft(x), Canvas.GetTop(x)), e.GetPosition(DiagramCanvas)))).OrderBy(x=>x.Item2).Where(x=>x.Item2 < x.x.Width);
            var ellipse = foundEllipses.FirstOrDefault().x;
            if (ellipse != null &&ellipse.Tag is TaggingObject tagging && tagging.TagObject is Relationship)
            {
                _controlPointStartDrag = true;
                _controlPointStartDragPosition = e.GetPosition(DiagramCanvas);
                ellipse.Fill = Brushes.Green;
                _selectedControlPointForDrag = ellipse;
            }
            if(ellipse == null)
            {
                RemoveAlignHelperEllipse();
            }

            if (_selectedEntity != null)
            {
                _isDragging = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _lastPositionPaning = mousePos;
                _isPanning = true;
                DiagramCanvas.CaptureMouse();
            }
            else if (MultiselectedEnabledToggle.IsChecked??false)
            {
                if(DiagramCanvas.Children.Contains(_selectionRect))
                {
                    DiagramCanvas.Children.Remove(_selectionRect);
                    DiagramCanvas.Children.Remove(_selectionRectTextblock);
                }
                _isSelectionPan = true;
                _selectionRect = new Rectangle();
                _selectionRect.StrokeDashCap = PenLineCap.Round;
                _selectionRect.StrokeDashArray = new DoubleCollection(new double[] { 0,4 });
                _selectionRect.StrokeThickness = 1;
                _selectionRect.Stroke = Brushes.Blue;
                _selectionRect.Width = 0;
                _selectionRect.Height = 0;
                _selectionRect.Fill = SelectionRectColor;
                DiagramCanvas.Children.Add(_selectionRect);
                Canvas.SetLeft(_selectionRect,mousePos.X);
                Canvas.SetTop(_selectionRect, mousePos.Y);
                Canvas.SetZIndex(_selectionRect, SelectionRectZIndex);

                _selectionRectTextblock = new TextBlock();
                DiagramCanvas.Children.Add(_selectionRectTextblock);
                _selectionRectTextblock.Foreground = new SolidColorBrush(Color.FromRgb(0,0,0));
                Canvas.SetLeft(_selectionRectTextblock, mousePos.X);
                Canvas.SetTop(_selectionRectTextblock, mousePos.Y+10);
                Canvas.SetZIndex(_selectionRectTextblock, SelectionRectZIndex);

                _selectionPanPos =mousePos;
            }
            _lastPosition = mousePos;
        }
        private void UpdateUiLastPos()
        {
            MouseXPos.Text =$"X: {Math.Round(_lastPosition.X, 2)}";
            MouseYPos.Text = $"Y: {Math.Round(_lastPosition.Y, 2)}";
        }
        private static Line MouseMoveLine = null;

        private object AlignHelperLinesTag = new object();
        private async Task CreateAlignHelperLines(EntityUiModel entityUiModel, double offset)
        {
            Dispatcher.Invoke(() => {

                var nearestEntity = _entities.Where(x=>x!= entityUiModel).OrderBy(x=>GeometryHelper.GetDistance(x.Position,entityUiModel.Position)).ToList().FirstOrDefault();
                if (nearestEntity != null)
                {
                    var x = nearestEntity;
                    var movedEntityBoxCorners = GeometryHelper.GetRectangleCornerCoords(DiagramCanvas, entityUiModel.Box);
                    var corners = GeometryHelper.GetRectangleCornerCoords(DiagramCanvas, x.Box);
                    if ((corners.upperLeft.X - offset) < movedEntityBoxCorners.upperLeft.X && (corners.upperLeft.X + offset) > movedEntityBoxCorners.upperLeft.X)//vertical line with same x
                    {
                        var isEntityMoreLeft = movedEntityBoxCorners.upperLeft.X > corners.upperLeft.X;
                        var diffX = isEntityMoreLeft ? movedEntityBoxCorners.upperLeft.X - corners.upperLeft.X : corners.upperLeft.X - movedEntityBoxCorners.upperLeft.X;
                        var to = corners.upperLeft;
                        var distUpperLeft = (GeometryHelper.GetDistance(movedEntityBoxCorners.upperLeft, corners.lowerLeft));
                        var distLowerLeft = (GeometryHelper.GetDistance(movedEntityBoxCorners.lowerLeft, corners.upperLeft));

                        var xx = isEntityMoreLeft ? movedEntityBoxCorners.upperLeft.X - diffX : movedEntityBoxCorners.upperLeft.X + diffX;
                        var y = movedEntityBoxCorners.upperLeft.Y < to.Y ?
                        (movedEntityBoxCorners.lowerLeft.Y + distLowerLeft) : (movedEntityBoxCorners.upperLeft.Y - distUpperLeft);
                        var line = _alignHelperLinesVertical==null?new Line(): _alignHelperLinesVertical;
                        line.Stroke = SelectionRectColor;
                        line.StrokeThickness = 1;
                        line.X1 = xx;
                        line.Y1 = distUpperLeft < distLowerLeft ? movedEntityBoxCorners.upperLeft.Y : movedEntityBoxCorners.lowerLeft.Y;
                        line.X2 = xx;
                        line.Y2 = y;
                        if (_alignHelperLinesVertical == null)
                        {
                            _alignHelperLinesVertical = line;
                            DiagramCanvas.Children.Add(_alignHelperLinesVertical);
                        }

                        entityUiModel.UpdatePosition(new Point(xx,entityUiModel.Position.Y));

                    }
                    else
                    {
                        RemoveAlignHelperLineVertical();
                    }

                    if ((corners.upperRight.Y - offset) < movedEntityBoxCorners.upperRight.Y && (corners.upperRight.Y + offset) > movedEntityBoxCorners.upperRight.Y)//horizontal line with same y
                    {
                        var isEntityUpper = movedEntityBoxCorners.upperRight.Y > corners.upperRight.Y;
                        var diffY = isEntityUpper ? movedEntityBoxCorners.upperRight.Y - corners.upperRight.Y : corners.upperRight.Y - movedEntityBoxCorners.upperRight.Y;
                        var to = corners.upperLeft;
                        var distUpperLeft = (GeometryHelper.GetDistance(movedEntityBoxCorners.upperRight, corners.lowerRight));
                        var distLowerLeft = (GeometryHelper.GetDistance(movedEntityBoxCorners.lowerRight, corners.upperRight));

                        var yy = isEntityUpper ? movedEntityBoxCorners.upperRight.Y - diffY : movedEntityBoxCorners.upperRight.Y + diffY;
                        var xxx = movedEntityBoxCorners.upperRight.X < to.X ?
                        (movedEntityBoxCorners.lowerRight.X + distLowerLeft) : (movedEntityBoxCorners.upperRight.X - distUpperLeft);
                        var line = _alignHelperLinesHorizontal == null ? new Line() : _alignHelperLinesHorizontal;
                        line.Stroke = SelectionRectColor;
                        line.StrokeThickness = 1;
                        line.X1 = distUpperLeft < distLowerLeft ? movedEntityBoxCorners.upperRight.X : movedEntityBoxCorners.lowerRight.X;
                        line.Y1 = yy;
                        line.X2 = xxx;
                        line.Y2 = yy;
                        if (_alignHelperLinesHorizontal == null)
                        {
                            _alignHelperLinesHorizontal = line;
                            DiagramCanvas.Children.Add(_alignHelperLinesHorizontal);
                        }

                        entityUiModel.UpdatePosition(new Point(entityUiModel.Position.X, yy));

                    }
                    else
                    {
                        RemoveAlignHelperLineHorizontal();
                    }
                }

                /*Thread.Sleep(300);
                foreach (var helperLine in DiagramCanvas.Children.OfType<Line>().Where(x => x.Tag == AlignHelperLinesTag).ToList())
                {
                    DiagramCanvas.Children.Remove(helperLine);
                }*/
            });
        }
        private async void DiagramCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = e.GetPosition(DiagramCanvas);
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed && _selectedEntity != null)
            {
                var rectFromSelectedEntity = _selectedEntity.Box;
                var offsetX = mousePos.X - _lastPosition.X;
                var offsetY = mousePos.Y - _lastPosition.Y;
                (double, double) mouseDirection = ((_lastPosition.X - mousePos.X), (_lastPosition.Y - mousePos.Y));
                var absolutePos = GeometryHelper.GetAbsolutePositionOfChild(DiagramCanvas, rectFromSelectedEntity);
                var newPos = new Point(_selectedEntity.Position.X + offsetX, _selectedEntity.Position.Y + offsetY);
                Debug.WriteLine($"offsetx: {offsetX} ,offsety: {offsetY}, lastpos: {_lastPosition.ToString()}, mousepos: {mousePos.ToString()}, absolute-pos-rectangle: {absolutePos?.ToString()}");
                if (absolutePos?.X < 0)
                {
                    newPos.X = 0;
                    //Canvas.SetLeft(rectFromSelectedEntity, 0);
                    Debug.WriteLine("abort move, because of outbounds left");
                }
                if (absolutePos?.Y < 0)
                {
                    newPos.Y = 0;
                    //Canvas.SetTop(rectFromSelectedEntity, 0);
                    Debug.WriteLine("abort move, because of outbounds up");
                }

                if (GeometryHelper.IsRectangleNearBounds(DiagramCanvas, rectFromSelectedEntity, (DiagramCanvasCellSize * 3)))
                {
                    //ExpandCanvasIfNecessary(DiagramCanvas, mousePos,10);
                    ExpandCanvas(DiagramCanvas, rectFromSelectedEntity, (DiagramCanvasCellSize * 3));
                }

                RenderGrid();
                //CreateEntitiesOnCanvas();
                var relations = _relationships.Where(relation => relation.Source == _selectedEntity || relation.Target == _selectedEntity);
                foreach(var relation in relations)
                {
                    DrawRelationShip(relation.Source == _selectedEntity?relation.Target:relation.Source);
                }

                _selectedEntity.UpdatePosition(newPos);
                await CreateAlignHelperLines(_selectedEntity,10);
            }
            else if (_controlPointStartDrag && _selectedControlPointForDrag != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var ellipse = _selectedControlPointForDrag;
                var pointTag = ellipse.Tag as TaggingObject;
                ellipse.Fill = Brushes.Yellow;
                var path = DiagramCanvas.Children.OfType<Path>().Where(x => x.Tag is TaggingObject tagging && tagging.TagObject == pointTag.TagObject).FirstOrDefault();
                if (path != null)
                {
                    var pathGeom = path.Data as PathGeometry;
                    if (pathGeom != null)
                    {
                        var figure = pathGeom.Figures.FirstOrDefault();
                        var matchedPoint = figure.Segments.Where(x => pointTag.args != null && x.GetHashCode() == (int)pointTag.args[0]).FirstOrDefault();
                        if (matchedPoint is LineSegment lineSegment)
                        {
                            int index = figure.Segments.IndexOf(lineSegment);
                            var prevLinesegment = (index - 1 != 0 ? figure.Segments[index - 1] : null) as LineSegment;
                            var nextLinesegment = (index + 1 != 0 ? figure.Segments[index + 1] : null) as LineSegment;
                            lineSegment.Point = new Point(mousePos.X, mousePos.Y);
                            ellipse.Fill = Brushes.Red;
                            double degreesPrev = double.NaN;
                            double degreesNext = double.NaN;
                            string tooltipContent = null;
                            if (nextLinesegment != null)
                            {
                                degreesNext = GeometryHelper.Degrees(lineSegment.Point, nextLinesegment.Point);
                                var radians = GeometryHelper.Radians(lineSegment.Point, nextLinesegment.Point);
                                tooltipContent += $"Next (blue): (Degr.:{degreesNext}°, Radians: {radians})\n";
                            }
                            if (prevLinesegment != null)
                            {
                                degreesPrev= GeometryHelper.Degrees(lineSegment.Point, prevLinesegment.Point);
                                var radians = GeometryHelper.Radians(lineSegment.Point, prevLinesegment.Point);
                                tooltipContent += $"Prev (green): (Degr.:{degreesPrev}°, Radians: {radians})";
                            }
                            var tooltip = (ToolTip)ellipse.ToolTip;

                            tooltip.Content = tooltipContent;
                            Canvas.SetLeft(ellipse, mousePos.X-(ellipse.Width));
                            Canvas.SetTop(ellipse, mousePos.Y - (ellipse.Height));

                            Ellipse degreeEllipse = _alignHelperEllipse == null ? new Ellipse() : _alignHelperEllipse;
                            Line linePrev = null;
                            Line lineNext = null;

                            if (prevLinesegment != null)
                            {
                                linePrev = _alignHelperEllipseDegreeLinePrev == null ? new Line() : _alignHelperEllipseDegreeLinePrev;
                                linePrev.Stroke = Brushes.Green;
                                linePrev.StrokeThickness = 1;
                                linePrev.X1 = lineSegment.Point.X;
                                linePrev.Y1 = lineSegment.Point.Y;
                                linePrev.X2 = prevLinesegment.Point.X;
                                linePrev.Y2 = prevLinesegment.Point.Y;
                            }
                            if(nextLinesegment != null)
                            {
                                lineNext = _alignHelperEllipseDegreeLineNext == null ? new Line() : _alignHelperEllipseDegreeLineNext;
                                lineNext.Stroke = Brushes.Blue;
                                lineNext.StrokeThickness = 1;
                                lineNext.X1 = lineSegment.Point.X;
                                lineNext.Y1 = lineSegment.Point.Y;
                                lineNext.X2 = nextLinesegment.Point.X;
                                lineNext.Y2 = nextLinesegment.Point.Y;
                            }

                            degreeEllipse.Fill = SelectionRectColor;
                            degreeEllipse.Width = 40;
                            degreeEllipse.StrokeDashCap = PenLineCap.Round;
                            degreeEllipse.Height = degreeEllipse.Width;
                            degreeEllipse.Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                            degreeEllipse.StrokeDashArray=new DoubleCollection(new double[] {0,4 });
                            degreeEllipse.StrokeThickness = 1;
                            degreeEllipse.ToolTip = tooltip;

                            if (_alignHelperEllipse == null)
                            {
                                _alignHelperEllipse = degreeEllipse;
                                DiagramCanvas.Children.Add(_alignHelperEllipse);
                            }
                            if(_alignHelperEllipseDegreeLineNext == null)
                            {
                                _alignHelperEllipseDegreeLineNext = lineNext;
                                DiagramCanvas.Children.Add(_alignHelperEllipseDegreeLineNext);
                            }
                            if (_alignHelperEllipseDegreeLinePrev == null)
                            {
                                _alignHelperEllipseDegreeLinePrev = linePrev;
                                DiagramCanvas.Children.Add(_alignHelperEllipseDegreeLinePrev);
                            }
                            Canvas.SetLeft(_alignHelperEllipse, lineSegment.Point.X - (degreeEllipse.Width / 2));
                            Canvas.SetTop(_alignHelperEllipse, lineSegment.Point.Y - (degreeEllipse.Height / 2));

                        }
                    }
                }
            }

            if (_isSelectionPan)
            {
                RenderSelectionArea(_selectionPanPos,mousePos);
            }
            _lastPosition = mousePos;
            UpdateUiLastPos();
        }
        private void RenderSelectionArea(Point first,Point second)
        {
            var firstXIsHighter = first.X > second.X;
            var firstYIsHighter = first.Y > second.Y;
            var w = firstXIsHighter ? first.X - second.X: second.X - first.X;
            var h = firstYIsHighter ? first.Y - second.Y : second.Y - first.Y;

            if (firstXIsHighter)
            {
                Canvas.SetLeft(_selectionRect, first.X - w);
                Canvas.SetLeft(_selectionRectTextblock, first.X - w);
            }
            if (firstYIsHighter)
            {
                Canvas.SetTop(_selectionRect, first.Y - h);
                Canvas.SetTop(_selectionRectTextblock, first.Y - h);
            }
            _selectionRect.Width= w;
            _selectionRect.Height= h;

            _entities.Where(x => x.IsSelected).ToList().ForEach(x => { x.SelectionState(false); });
            var entitesWihtinBounds = _entities.Where(x => GeometryHelper.IsRectWithinBounds(_selectionRect, x.Box)).ToList();
            entitesWihtinBounds.ForEach(x => { x.SelectionState(true); });
            if(entitesWihtinBounds.Any())
            {
                _selectionRectTextblock.Text = $"Selected: {string.Join(',', entitesWihtinBounds.Select(x => x.Entity.Name))}";
            }

        }
        private async Task CreateMouseMoveLine(Point from,Point to)
        {
            Dispatcher.Invoke(() => {

                MouseMoveLine = new Line();
                MouseMoveLine.Stroke = Brushes.Black;
                MouseMoveLine.StrokeThickness = 5;
                MouseMoveLine.X1 = from.X;
                MouseMoveLine.Y1 = from.Y;
                MouseMoveLine.X2 = to.X;
                MouseMoveLine.Y2 = to.Y;
                DiagramCanvas.Children.Add(MouseMoveLine);
            });
            Thread.Sleep(300);
            RemoveMouseMoveLine() ;
        }
        private async Task RemoveMouseMoveLine()
        {
            Dispatcher.Invoke(() =>
            {
                DiagramCanvas.Children.Remove(MouseMoveLine);
            });
        }
        private void RemoveAlignHelperLineVertical()
        {

            if (_alignHelperLinesVertical != null)
            {
                DiagramCanvas.Children.Remove(_alignHelperLinesVertical);
                _alignHelperLinesVertical = null;
            }
        }
        private void RemoveAlignHelperLineHorizontal()
        {

            if (_alignHelperLinesHorizontal != null)
            {
                DiagramCanvas.Children.Remove(_alignHelperLinesHorizontal);
                _alignHelperLinesHorizontal = null;
            }
        }
        private void RemoveAlignHelperEllipse()
        {

            if (_alignHelperEllipse != null)
            {
                if (_alignHelperEllipseDegreeLinePrev != null)
                {
                    DiagramCanvas.Children.Remove(_alignHelperEllipseDegreeLinePrev);
                    _alignHelperEllipseDegreeLinePrev = null;
                }
                if (_alignHelperEllipseDegreeLineNext != null)
                {
                    DiagramCanvas.Children.Remove(_alignHelperEllipseDegreeLineNext);
                    _alignHelperEllipseDegreeLineNext = null;
                }
                DiagramCanvas.Children.Remove(_alignHelperEllipse);
                _alignHelperEllipse = null;
            }
        }
        private void DiagramCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(_isDragging)
            {
                _selectedEntity = null;
                RemoveAlignHelperLineHorizontal();
                RemoveAlignHelperLineVertical();

            }
            if (_isPanning)
            {
                Point currentMousePosition = e.GetPosition(this);
                double offsetX = _lastPositionPaning.X - currentMousePosition.X;
                double offsetY = _lastPositionPaning.Y - currentMousePosition.Y;

                _translateTransform.X += offsetX;
                _translateTransform.Y += offsetY;

                if (MouseMoveLine != null)
                {
                    RemoveMouseMoveLine();
                }
                Task.Run(() => CreateMouseMoveLine(currentMousePosition, _lastPositionPaning));
                _isPanning = false;
                DiagramCanvas.ReleaseMouseCapture();

                RenderGrid();
                CreateEntitiesOnCanvas();
            }
            if ( _isSelectionPan)
            {
                DiagramCanvas.Children.Remove(_selectionRect);
                DiagramCanvas.Children.Remove(_selectionRectTextblock);
                _isSelectionPan = false;
            }
            if(_controlPointStartDrag)
            {

                _controlPointStartDrag = false;
                //RemoveAlignHelperEllipse();
            }


            System.Diagnostics.Debug.WriteLine("DiagramCanvas_MouseLeftButtonUp");
        }

        private void DiagramCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(DiagramCanvas);
            _selectedEntity = _entities.FirstOrDefault(entity =>
                mousePos.X >= entity.Position.X && mousePos.X <= entity.Position.X + EntityUiModel.BoxSizeWidth &&
                mousePos.Y >= entity.Position.Y && mousePos.Y <= entity.Position.Y + (EntityUiModel.GetBoxSize(entity.Properties.Count).Height));

            if (_selectedEntity != null)
            {
                e.Handled = true; 
            }
        }
        private void DiagramCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(DiagramCanvas);
            var endEntity = _entities.FirstOrDefault(entity =>
                mousePos.X >= entity.Position.X && mousePos.X <= entity.Position.X + EntityUiModel.BoxSizeWidth &&
                mousePos.Y >= entity.Position.Y && mousePos.Y <= entity.Position.Y + (EntityUiModel.GetBoxSize(entity.Properties.Count).Height));

            if (_tempStartEntity != null && endEntity != null && _tempStartEntity != endEntity)
            {
                var startProperty = _tempStartEntity.Entity.Properties.Keys.First();
                var endProperty = endEntity.Entity.Properties.Keys.First();

                var newRelationship = new Relationship
                {
                    Source = _tempStartEntity,
                    Target = endEntity,
                    SourceProperty = startProperty,
                    TargetProperty = endProperty,
                    StartText = $"{_tempStartEntity.Entity.Name}.{startProperty}",
                    MidText = "1:N",
                    EndText = $"{_tempStartEntity.Entity.Name}.{endProperty}"
                };

                _relationships.Add(newRelationship);
                DrawRelationships();
            }

            if (_tempRelationshipPath != null)
            {
                DiagramCanvas.Children.Remove(_tempRelationshipPath);
                _tempRelationshipPath = null;
            }
            _tempStartEntity = null;
        }
        private void Path_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _selectedPath = sender as Path;
            if(_selectedPath.Tag is TaggingObject tagging && tagging.TagObject is Relationship relationship)
            {
                System.Diagnostics.Debug.WriteLine($"Details der Relationship: {relationship.StartText} -> {relationship.MidText} -> {relationship.EndText}");
            }
        }

        private void SetCanvasSize(double height,double width)
        {
            DiagramCanvas.Height = height;
            DiagramCanvas.Width =width;
        }
        
        private void ExpandCanvas(Canvas canvas, Rectangle rectangle, double offset)
        {
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;

            var absolutePositionOfRectangle = GeometryHelper.GetAbsolutePositionOfChild(canvas, rectangle);

            if (absolutePositionOfRectangle == null)
                return;

            bool expanded = false;

            // Links erweitern
            /*if (absolutePositionOfRectangle.Value.X <= offset)
            {
                canvasWidth += offset;
                foreach (UIElement child in canvas.Children)
                {
                    Canvas.SetLeft(child, Canvas.GetLeft(child) + offset);
                }
                expanded = true;
            }*/

            // Rechts erweitern
            if ((absolutePositionOfRectangle.Value.X + rectangle.Width) >= (canvasWidth - offset))
            {
                canvasWidth += offset;
                expanded = true;
            }

            // Oben erweitern
            /*if (absolutePositionOfRectangle.Value.Y <= offset)
            {
                canvasHeight += offset;
                foreach (UIElement child in canvas.Children)
                {
                    Canvas.SetTop(child, Canvas.GetTop(child) + offset);
                }
                expanded = true;
            }*/

            // Unten erweitern
            if ((absolutePositionOfRectangle.Value.Y + rectangle.Height) >= (canvasHeight - offset))
            {
                canvasHeight += offset;
                expanded = true;
            }

            if (expanded)
            {
                canvas.Width = canvasWidth;
                canvas.Height = canvasHeight;
            }
        }

        private Path FindPathByControlPoint(Ellipse controlPoint)
        {
            foreach (var child in DiagramCanvas.Children)
            {
                if (child is Path path)
                {
                    var pathGeometry = path.Data as PathGeometry;
                    if (pathGeometry != null)
                    {
                        foreach (var figure in pathGeometry.Figures)
                        {
                            foreach (var segment in figure.Segments)
                            {
                                if (segment is BezierSegment bezierSegment)
                                {
                                    var controlPoints = new List<Point> { figure.StartPoint, bezierSegment.Point1, bezierSegment.Point2, bezierSegment.Point3 };

                                    foreach (var point in controlPoints)
                                    {
                                        if (controlPoint.Tag is not null&&point == (Point)controlPoint.Tag)
                                            return path;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private IEnumerable<FrameworkElement> GetUiElementsFromEntity(EntityUiModel entity)
        {
            if (entity == null) return null;
            return entity.GetAllUiElements();
        }
        private void DeleteUiElementsFromEntity(EntityUiModel entity)
        {
            if (entity == null) return;
            var elements = GetUiElementsFromEntity(entity);
            foreach (var element in elements)
            {
                DiagramCanvas.Children.Remove(element);
            }
        }
        private void CreateEntitiesOnCanvas()
        {
            foreach (var entity in _entities)
            {
                EntityUiModel.AddToCanvs(DiagramCanvas, entity);
            }
        }
        private void UpdateEntitiesOnCanvas()
        {
            foreach (var entity in _entities)
            {
                entity.UpdatePosition();
            }
        }

        private void AddMarkingIntersectionPoint(Point point)
        {
            var markingIntersectionPoint = new Rectangle
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.Red,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Margin = new Thickness(point.X - 3, point.Y - 3, 0, 0),

            };
            Canvas.SetLeft(markingIntersectionPoint, point.X);
            Canvas.SetTop(markingIntersectionPoint, point.Y);
            DiagramCanvas.Children.Add(markingIntersectionPoint);
        }
        private void DrawRelationships([CallerMemberName] string caller = "")
        {
            foreach (var entity in _entities)
            {
                DrawRelationShip(entity);
            }
        }
        private void DrawRelationShip(EntityUiModel entity)
        {
            foreach (var relationship in _relationships)
            {
                if (relationship.Source == entity || relationship.Target == entity)
                {
                    var allRelationUiObject = DiagramCanvas.Children.OfType<FrameworkElement>().Where(x => x.Tag is TaggingObject tag && tag.TagObject == relationship).ToList();
                    foreach (var relationshipUiObject in allRelationUiObject)
                    {
                        DiagramCanvas.Children.Remove(relationshipUiObject);
                    }
#if USEV2RELATIONSHIPLINE
                    var pathGeometry = CreateRelationshipLinesegmentPathV2(relationship, relationship.SourceProperty, relationship.TargetProperty);
#else
                    var pathGeometry = CreateRelationshipLinesegmentPathV1(relationship, relationship.SourceProperty, relationship.TargetProperty);
#endif 
                    var contextMenu = new ContextMenu();
                    MenuItem itemSetAmountOfControlPoints = new MenuItem { Header = "Set amount of controls points" };
                    itemSetAmountOfControlPoints.Click += (s, e) =>
                    {
                        var dialog = CustomInputDialog.Show($"Amount of control points",$"Please enter the amout of control points (actual value: {relationship.AmountOfControlPoints})");
                        if(dialog.ShowDialog().Value)
                        {
                            if(int.TryParse(dialog.InputValue, out int inputInt))
                            {
                                var path = GetPathFromRelation(relationship);
                                if(path != null)
                                {
                                    relationship.AmountOfControlPoints = inputInt;
                                    RenderPathControlPoints(path.Data as PathGeometry, relationship);
                                }
                            }
                        }
                    };
                    MenuItem itemInsertControlPointBetween = new MenuItem { Header = "Insert one point between" };
                    itemInsertControlPointBetween.Click += (s, e) =>
                    {

                        var path = GetPathFromRelation(relationship);
                        if (path != null)
                        {
                            var controlPoints = GetPathControlPoints(relationship).Select(x=>(x, GeometryHelper.GetDistance(_lastPosition, new Point(Canvas.GetLeft(x), Canvas.GetTop(x))))).OrderBy(x=>x.Item2).ToList();
                            if(controlPoints.Any())
                            {
                                PathGeometry newPathGeom = new PathGeometry();
                                var currentPathFigure = (path.Data as PathGeometry).Figures.First();
                                var pathGeom = currentPathFigure.Segments.OfType<LineSegment>().ToList();

                                PathFigure newPathFigure = new PathFigure();
                                newPathFigure.StartPoint = currentPathFigure.StartPoint;
                                var firstPoint = controlPoints.First().x;
                                var firstPointTaggingObject = firstPoint.Tag is TaggingObject taggingObject ? taggingObject : null;
                                var secondPoint = controlPoints[1].x;
#if RELATIONSHIP_LINE_CORRECTION_DEBUG
                                firstPoint.Fill = new SolidColorBrush(Color.FromArgb(255, 45, 12, 78));
                                secondPoint.Fill = new SolidColorBrush(Color.FromArgb(255, 45, 12, 78));
#endif

                                var lineSegmentOfFirstPoint = pathGeom.Where(x => x.GetHashCode() == ((int)firstPointTaggingObject.args[0])).FirstOrDefault();

                                int indexOfFirstPoint = pathGeom.IndexOf(lineSegmentOfFirstPoint);

                                var firstPointLocation = new Point(Canvas.GetLeft(firstPoint), Canvas.GetTop(firstPoint));
                                var secondPointLocation = new Point(Canvas.GetLeft(secondPoint), Canvas.GetTop(secondPoint));
                                var newPointX = (firstPointLocation.X+ secondPointLocation.X)/2;
                                var newPointY = (firstPointLocation.Y + secondPointLocation.Y) / 2;

                                var newPoint = new Point(newPointX, newPointY);
                                var newLineSegment = new LineSegment(newPoint, true);
                                var degrees = GeometryHelper.Degrees(newPoint,firstPointLocation);
                                CreateControlPoint(relationship, newPoint, newLineSegment.GetHashCode(), $"{degrees}°");
                                pathGeom.Insert(indexOfFirstPoint, newLineSegment);
                                pathGeom.ForEach(x=> newPathFigure.Segments.Add(x));
                                newPathGeom.Figures.Add(newPathFigure);
                                path.Data = newPathGeom;
                            }
                        }
                    };
                    contextMenu.Items.Add(itemSetAmountOfControlPoints);
                    contextMenu.Items.Add(itemInsertControlPointBetween);
                    if (pathGeometry.Figures.Count > 0)
                    {
                        var firstFigure = pathGeometry.Figures[0];

                        var path = new Path
                        {
                            Stroke = Brushes.Black,
                            StrokeThickness = 2,
                            Data = pathGeometry,
                            ContextMenu = contextMenu,
                            Tag = new TaggingObject(relationship) 
                        };

                        path.MouseRightButtonDown += (sender,args)=> {
                            
                        };    


                        path.MouseLeftButtonDown += Path_MouseLeftButtonDown;

                        DiagramCanvas.Children.Add(path);

                        var startTextOffset = CalculateTextOffset(firstFigure.StartPoint, entity.Position, relationship.StartText);
                        var bezierSegment = (LineSegment)firstFigure.Segments[0]; // Annahme: Erste Segment ist ein BezierSegment
                        /*var midPoint = bezierSegment.Point3;
                        var midTextOffset = CalculateTextOffset(midPoint, entity.Position, relationship.MidText);

                        var endPoint = bezierSegment.Point2;
                        var endTextOffset = CalculateTextOffset(endPoint, entity.Position, relationship.EndText);

                        AddTextToCanvas(relationship,relationship.StartText, startTextOffset);
                        AddTextToCanvas(relationship, relationship.MidText, midTextOffset);
                        AddTextToCanvas(relationship, relationship.EndText, endTextOffset);*/
                        //RenderPathControlPoints(relationship,path);


                    }
                }
            }
        }
        private object GridTagObjectRow = new object();
        private object GridTagObjectColumn = new object();

        private void DrawGridRows(int cellSize, int rows, int rowOffset = 0)
        {
            var previousLines = DiagramCanvas.Children.OfType<Line>().Where(x => x.Tag == GridTagObjectRow).ToList();
            foreach (var line in previousLines)
            {
                line.X2 = DiagramCanvas.ActualWidth;
            }
            for (int i = rowOffset; i <= rows; i++)
            {
                Line line = new Line();
                line.Stroke = Brushes.Black;
                line.StrokeThickness = 1;

                line.X1 = 0;
                line.Y1 = i * cellSize;
                line.X2 = DiagramCanvas.ActualWidth;
                line.Y2 = i * cellSize;
                line.Tag = GridTagObjectRow;
                line.Visibility = _enableGrid ? Visibility.Visible : Visibility.Hidden;
                DiagramCanvas.Children.Add(line);

                ToolTip toolTip = new ToolTip();
                toolTip.Content = $"{i * cellSize}";
                TextBlock textBlock = new TextBlock();
                textBlock.Tag = GridTagObjectRow;
                textBlock.Text = $"{i * cellSize}";
                textBlock.ToolTip = toolTip;
                textBlock.Visibility = _enableGrid ? Visibility.Visible : Visibility.Hidden;
                DiagramCanvas.Children.Add(textBlock);
                Canvas.SetLeft(textBlock, 0);
                Canvas.SetTop(textBlock, i * cellSize);
            }
        }
        private void DrawGridColumns(int cellSize, int columns, int columnOffset = 0)
        {
            var previousLines = DiagramCanvas.Children.OfType<Line>().Where(x => x.Tag == GridTagObjectColumn).ToList();
            foreach (var line in previousLines)
            {
                line.Y2 = DiagramCanvas.ActualHeight;
            }
            for (int j = columnOffset; j <= columns; j++)
            {
                Line line = new Line();
                line.Stroke = Brushes.Black;
                line.StrokeThickness = 1;

                line.X1 = j * cellSize;
                line.Y1 = 0;
                line.X2 = j * cellSize;
                line.Y2 = DiagramCanvas.ActualHeight;
                line.Tag = GridTagObjectColumn;
                line.Visibility = _enableGrid ? Visibility.Visible : Visibility.Hidden;
                DiagramCanvas.Children.Add(line);

                ToolTip toolTip = new ToolTip();
                toolTip.Content = $"{j * cellSize}";
                TextBlock textBlock = new TextBlock();
                textBlock.Tag = GridTagObjectColumn;
                textBlock.Text = $"{j * cellSize}";
                textBlock.ToolTip = toolTip;
                textBlock.Visibility = _enableGrid ? Visibility.Visible : Visibility.Hidden;
                DiagramCanvas.Children.Add(textBlock);
                Canvas.SetLeft(textBlock, j * cellSize);
                Canvas.SetTop(textBlock, 0);
            }
        }
        private PathGeometry CreateRelationshipLinesegmentPathV2(Relationship relationship, string sourceProperty, string targetProperty)
        {
            var (start, end, sourceIdx, targetIdx) = GetLineEndpoints(relationship.Source, relationship.Target, sourceProperty, targetProperty);
            if (start == null || end == null)
            {
                return null;
            }
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();
            pathFigure.StartPoint = start.Value;

            Func<Point, Point, double, double> CalcY = new Func<Point, Point, double, double>((pos1, pos2, appendvalue) =>
            {
                var relatedDirectionsOfEntites = GeometryHelper.GetDirection(pos1, pos2);
                return (relatedDirectionsOfEntites.isAbove ? (pos1.Y - appendvalue) : (pos1.Y + appendvalue));
            });
            Func<Point, Point, double, double> CalcX = new Func<Point, Point, double, double>((pos1, pos2, appendvalue) =>
            {
                var relatedDirectionsOfEntites = GeometryHelper.GetDirection(pos1, pos2);
                return (relatedDirectionsOfEntites.isLeft ? (pos1.X - appendvalue) : (pos1.X + appendvalue));
            });
            var sourceHeight = relationship.Source.Box.Height;
            var targetHeight = relationship.Target.Box.Height;

            var sourceIdxX = 20 + (sourceIdx * 10);
            var targetIdxX = 20 + (targetIdx * 10);
            var sourceEntityBounds = relationship.Source.GetBounds();
            var targetEntityBounds = relationship.Target.GetBounds();

            var directionStartAndEnd = GeometryHelper.GetDirection(start.Value, end.Value);
            var startSegmentYDistance = directionStartAndEnd.isAbove ? (start.Value.Y - end.Value.Y) : (end.Value.Y - start.Value.Y);
            var startSegmentEndpos = new Point(start.Value.X - sourceIdxX, CalcY(start.Value, end.Value, startSegmentYDistance));
            var startSegmentEndpos2 = new Point(startSegmentEndpos.X, startSegmentEndpos.Y + (directionStartAndEnd.isAbove ? -(startSegmentEndpos.Y - sourceEntityBounds.Top + sourceIdxX) : (sourceEntityBounds.Bottom - startSegmentEndpos.Y + sourceIdxX)));
            
            (Point start, Point mid, Point end) startSegmentPositions = (new Point(start.Value.X, start.Value.Y), new Point(start.Value.X - sourceIdxX, start.Value.Y), startSegmentEndpos);

            var endSegmentYDistance = directionStartAndEnd.isAbove ? (start.Value.Y - end.Value.Y) : (end.Value.Y - start.Value.Y);
            var endSegmentEndpos = new Point(end.Value.X - targetIdxX, CalcY(end.Value, start.Value, 0));
            var endSegmentEndpos2 = new Point(endSegmentEndpos.X, endSegmentEndpos.Y + (!directionStartAndEnd.isAbove ? -(endSegmentEndpos.Y - targetEntityBounds.Top) : (targetEntityBounds.Bottom - endSegmentEndpos.Y)));
            (Point start, Point mid, Point end) endSegmentPositions = (new Point(end.Value.X, end.Value.Y), new Point(end.Value.X - targetIdxX, end.Value.Y), endSegmentEndpos);
            var startSegmentHorizontalPos = new Point(!directionStartAndEnd.isLeft ? sourceEntityBounds.Right : (endSegmentEndpos2.X), startSegmentEndpos2.Y);
            var startMidSegment = new Point(startSegmentEndpos.X, startSegmentEndpos.Y);

            const string sourceStartSegmentName = "SourceStartSegment";
            const string midSegmentName = "MidSegment";
            const string targetEndSegmentName = "TargetEndSegment";

            Dictionary<string, List<Point>> lineSegmentDefinitionPoints = new()
            {
                {sourceStartSegmentName,new List<Point>{ startSegmentPositions.start, startSegmentPositions.mid, startSegmentEndpos2, startSegmentHorizontalPos } },
                {targetEndSegmentName,new List<Point>{ endSegmentEndpos2, endSegmentPositions.mid, endSegmentPositions.start } },
            };

            foreach (var point in lineSegmentDefinitionPoints.Values.SelectMany(x => x))
            {
                var linesegment = new LineSegment(point, true);
                pathFigure.Segments.Add(linesegment);
            }
            pathGeometry.Figures.Add(pathFigure);
            var intersectionPoints = new List<(object affectedSegment,EntityUiModel affectedEntity, List<(Point, GeometryHelper.BOX_POSITION)> intersections)>();
            foreach (var entity in _entities)
            {
                var intersection = GeometryHelper.GetIntersectionPoints(pathGeometry, entity.Box);
                intersectionPoints.Add((intersection.affectedSegment,entity,intersection.intersections));
            }
            foreach (var item in intersectionPoints)
            {
                if (item.intersections.Count == 0)
                    continue;

                bool hasEntryAndExitPoint = item.intersections.Count >= 2;
                SolidColorBrush intersectionPointColor = hasEntryAndExitPoint ? new SolidColorBrush(Color.FromRgb(55, 252, 55)) : new SolidColorBrush(Color.FromRgb(123, 123, 123));
                Debug.WriteLine($"line intersections, hasEntryAndExitPoint:{hasEntryAndExitPoint}, affected segment: {((LineSegment)(item.affectedSegment)).Point.ToString()}, relationship: {relationship}");
                List<(Point, GeometryHelper.BOX_POSITION)> lineRouteEditPoints = new List<(Point, GeometryHelper.BOX_POSITION)>();
                int i = 1;
                if (directionStartAndEnd.isLeft && item.intersections.Count >= 2)
                {

                    var intersectionPointNearestToRelatedEntity = item.intersections.OrderBy(x => GeometryHelper.GetDistance(x.Item1, endSegmentEndpos2)).First();
                    Debug.WriteLine($"{intersectionPointNearestToRelatedEntity.Item1.ToString()}----{intersectionPointNearestToRelatedEntity.Item2}");
                }

                foreach (var intersection in item.intersections)
                {
                    var point = intersection.Item1;
#if RELATIONSHIP_LINE_CORRECTION_DEBUG
                    var tooltip = new ToolTip();
                    tooltip.Content = $"INTERSECTIONPOINT #{i}/{item.intersections.Count}: {item.affectedEntity.Entity.Name}, {intersection.Item2}, hasEntryAndExitPoint: {hasEntryAndExitPoint}";
                    Ellipse ellipse = new Ellipse();
                    ellipse.Width = 10;
                    ellipse.Height = 10;
                    ellipse.Fill = intersectionPointColor;
                    ellipse.ToolTip = tooltip;
                    ellipse.Tag = new TaggingObject(relationship);
                    _intersectionPoints.Add(ellipse);
                    DiagramCanvas.Children.Add(ellipse);
                    Canvas.SetLeft(ellipse, point.X - (ellipse.Width / 2));
                    Canvas.SetTop(ellipse, point.Y - (ellipse.Height / 2));
                    Canvas.SetZIndex(ellipse, 10);
#endif

                    (Point, GeometryHelper.BOX_POSITION) pNew = (default, default);
                    switch (intersection.Item2)
                    {
                        case GeometryHelper.BOX_POSITION.TOP:
                            pNew = (new Point(point.X, point.Y - RelationshipLineIntersectionCorrectionSpaceValue), intersection.Item2);
                            break;
                        case GeometryHelper.BOX_POSITION.BOTTOM:
                            pNew = (new Point(point.X, point.Y + RelationshipLineIntersectionCorrectionSpaceValue), intersection.Item2);
                            break;
                        case GeometryHelper.BOX_POSITION.LEFT:
                            pNew = (new Point(point.X - RelationshipLineIntersectionCorrectionSpaceValue, point.Y), intersection.Item2);
                            break;
                        case GeometryHelper.BOX_POSITION.RIGHT:
                            pNew = (new Point(point.X + RelationshipLineIntersectionCorrectionSpaceValue, point.Y), intersection.Item2);
                            break;
                    }
                    lineRouteEditPoints.Add(pNew);
                    i++;
                }

                i = 1;
                List<PathSegment> pathSegments = new List<PathSegment>();
                var getHorizontalMovingPoint = lineRouteEditPoints.Where(x => x.Item2 == GeometryHelper.BOX_POSITION.LEFT || x.Item2 == GeometryHelper.BOX_POSITION.RIGHT)?.FirstOrDefault();
                var getVerticalMovingPoint = lineRouteEditPoints.Where(x => x.Item2 == GeometryHelper.BOX_POSITION.TOP || x.Item2 == GeometryHelper.BOX_POSITION.BOTTOM)?.FirstOrDefault();
#if RELATIONSHIP_LINE_CORRECTION_DEBUG
                foreach (var pNew in lineRouteEditPoints)
                {
                    var point = pNew.Item1;
                    var tooltip = new ToolTip();
                    tooltip.Content = $"BUFFERPOINT #{i}/{lineRouteEditPoints.Count}: {item.affectedEntity.Entity.Name} ,direction relative to box:{pNew.Item2.ToString()}";
                    Ellipse newPoint = new Ellipse();
                    newPoint.Width = 15;
                    newPoint.Height = 15;
                    newPoint.Fill = RelationshipLineIntersectionCorrectionSpaceHelperPointColor;
                    newPoint.Tag = new TaggingObject(relationship);
                    newPoint.ToolTip = tooltip;
                    DiagramCanvas.Children.Add(newPoint);
                    Canvas.SetLeft(newPoint, point.X - (newPoint.Width / 2));
                    Canvas.SetTop(newPoint, point.Y - (newPoint.Height / 2));
                    Canvas.SetZIndex(newPoint, 10);
                    i++;
                }
#endif

                if (pathSegments.Any())
                {
                    pathFigure.Segments.Clear();
                    pathSegments.ForEach(_ => pathFigure.Segments.Add(_));
                }

            }
            RenderPathControlPoints(pathGeometry,relationship);
            return pathGeometry;
        }
        private PathGeometry CreateRelationshipLinesegmentPathV1(Relationship relationship, string sourceProperty, string targetProperty)
        {
            var (start, end, sourceIdx, targetIdx) = GetLineEndpoints(relationship.Source, relationship.Target, sourceProperty, targetProperty);
            if (start == null || end == null)
            {
                return null;
            }
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();
            pathFigure.StartPoint = start.Value;

            Func<Point, Point, double, double> CalcY = new Func<Point, Point, double, double>((pos1, pos2, appendvalue) =>
            {
                var relatedDirectionsOfEntites = GeometryHelper.GetDirection(pos1, pos2);
                return (relatedDirectionsOfEntites.isAbove ? (pos1.Y - appendvalue) : (pos1.Y + appendvalue));
            });
            Func<Point, Point, double, double> CalcX = new Func<Point, Point, double, double>((pos1, pos2, appendvalue) =>
            {
                var relatedDirectionsOfEntites = GeometryHelper.GetDirection(pos1, pos2);
                return (relatedDirectionsOfEntites.isLeft ? (pos1.X - appendvalue) : (pos1.X + appendvalue));
            });
            var sourceHeight = relationship.Source.Box.Height;
            var targetHeight = relationship.Target.Box.Height;

            var sourceIdxX = 20 + (sourceIdx * 10);
            var targetIdxX = 20 + (targetIdx * 10);


            var directionStartAndEnd = GeometryHelper.GetDirection(start.Value, end.Value);
            var startSegmentYDistance = directionStartAndEnd.isAbove ? (start.Value.Y - end.Value.Y) : (end.Value.Y - start.Value.Y);
            var startSegmentEndpos = new Point(start.Value.X - sourceIdxX, CalcY(start.Value, end.Value, startSegmentYDistance));
            (Point start, Point mid, Point end) startSegmentPositions = (new Point(start.Value.X, start.Value.Y), new Point(start.Value.X - sourceIdxX, start.Value.Y), startSegmentEndpos);

            var endSegmentYDistance = directionStartAndEnd.isAbove ? (start.Value.Y - end.Value.Y) : (end.Value.Y - start.Value.Y);
            var endSegmentEndpos = new Point(end.Value.X - targetIdxX, CalcY(end.Value, start.Value, 0));
            (Point start, Point mid, Point end) endSegmentPositions = (new Point(end.Value.X, end.Value.Y), new Point(end.Value.X - targetIdxX, end.Value.Y), endSegmentEndpos);

            var midSegmentYDistance = directionStartAndEnd.isAbove ? (startSegmentEndpos.Y - endSegmentEndpos.Y) : (endSegmentEndpos.Y - startSegmentEndpos.Y);
            var midSegmentXDistance = directionStartAndEnd.isLeft ? (endSegmentEndpos.X - startSegmentEndpos.X) : (startSegmentEndpos.X - endSegmentEndpos.X);
            var midSegmentEndpos = new Point(endSegmentEndpos.X, endSegmentEndpos.Y);
            var startMidSegment = new Point(startSegmentEndpos.X, startSegmentEndpos.Y);

            const string sourceStartSegmentName = "SourceStartSegment";
            const string midSegmentName = "MidSegment";
            const string targetEndSegmentName = "TargetEndSegment";
            (Point start, Point mid, Point end) midSegmentPositions = (startMidSegment, new Point((directionStartAndEnd.isLeft ? startSegmentEndpos.X + midSegmentXDistance : startSegmentEndpos.X - midSegmentXDistance), startSegmentEndpos.Y), midSegmentEndpos);
            Dictionary<string, List<Point>> lineSegmentDefinitionPoints = new()
            {
                {sourceStartSegmentName,new List<Point>{ startSegmentPositions.start, startSegmentPositions.mid, startSegmentPositions.end } },
                {midSegmentName,new List<Point>{ midSegmentPositions.start, midSegmentPositions.mid, midSegmentPositions.end } },
                {targetEndSegmentName,new List<Point>{ endSegmentPositions.end, endSegmentPositions.mid, endSegmentPositions.start } },
            };

            foreach (var point in lineSegmentDefinitionPoints.Values.SelectMany(x => x))
            {
                var linesegment = new LineSegment(point, true);
                pathFigure.Segments.Add(linesegment);
            }
            pathGeometry.Figures.Add(pathFigure);

            var intersectionSource = GeometryHelper.GetIntersectionPoints(pathGeometry, relationship.Source.Box);
            var intersectionTarget = GeometryHelper.GetIntersectionPoints(pathGeometry, relationship.Target.Box);
            var intersectionPoints = new List<(object affectedSegment, List<(Point, GeometryHelper.BOX_POSITION)> intersections)>()
            { intersectionSource,intersectionTarget };

            foreach (var item in intersectionPoints)
            {
                if (item.intersections.Count == 0)
                    continue;

                bool hasEntryAndExitPoint = item.intersections.Count == 2;
                SolidColorBrush intersectionPointColor = hasEntryAndExitPoint ? new SolidColorBrush(Color.FromRgb(55, 252, 55)) : new SolidColorBrush(Color.FromRgb(123, 123, 123));
                bool isBoxSource = item == intersectionSource;
                Debug.WriteLine($"line intersections, hasEntryAndExitPoint:{hasEntryAndExitPoint}, isBoxSource:{isBoxSource}, affected segment: {((LineSegment)(item.affectedSegment)).Point.ToString()}, relationship: {relationship}");
                List<(Point, GeometryHelper.BOX_POSITION)> lineRouteEditPoints = new List<(Point, GeometryHelper.BOX_POSITION)>();
                foreach (var intersection in item.intersections)
                {
                    var point = intersection.Item1;
#if RELATIONSHIP_LINE_CORRECTION_DEBUG
                    Ellipse ellipse = new Ellipse();
                    ellipse.Width = 10;
                    ellipse.Height = 10;
                    ellipse.Fill = intersectionPointColor;
                    ellipse.Tag = new TaggingObject(relationship);
                    _intersectionPoints.Add(ellipse);
                    DiagramCanvas.Children.Add(ellipse);
                    Canvas.SetLeft(ellipse, point.X - (ellipse.Width / 2));
                    Canvas.SetTop(ellipse, point.Y - (ellipse.Height / 2));
                    Canvas.SetZIndex(ellipse, 10);
#endif

                    (Point, GeometryHelper.BOX_POSITION) pNew = (default, default);
                    switch (intersection.Item2)
                    {
                        case GeometryHelper.BOX_POSITION.TOP:
                            pNew = (new Point(point.X, point.Y - RelationshipLineIntersectionCorrectionSpaceValue), intersection.Item2);
                            break;
                        case GeometryHelper.BOX_POSITION.BOTTOM:
                            pNew = (new Point(point.X, point.Y + RelationshipLineIntersectionCorrectionSpaceValue), intersection.Item2);
                            break;
                        case GeometryHelper.BOX_POSITION.LEFT:
                            pNew = (new Point(point.X - RelationshipLineIntersectionCorrectionSpaceValue, point.Y), intersection.Item2);
                            break;
                        case GeometryHelper.BOX_POSITION.RIGHT:
                            pNew = (new Point(point.X + RelationshipLineIntersectionCorrectionSpaceValue, point.Y), intersection.Item2);
                            break;
                    }
                    lineRouteEditPoints.Add(pNew);
                }
                List<PathSegment> pathSegments = new List<PathSegment>();

                var getHorizontalMovingPoint = lineRouteEditPoints.Where(x => x.Item2 == GeometryHelper.BOX_POSITION.LEFT || x.Item2 == GeometryHelper.BOX_POSITION.RIGHT)?.FirstOrDefault();
                var getVerticalMovingPoint = lineRouteEditPoints.Where(x => x.Item2 == GeometryHelper.BOX_POSITION.TOP || x.Item2 == GeometryHelper.BOX_POSITION.BOTTOM)?.FirstOrDefault();
#if RELATIONSHIP_LINE_CORRECTION_DEBUG
                foreach (var pNew in lineRouteEditPoints)
                {
                    var point = pNew.Item1;
                    var tooltip = new ToolTip();
                    tooltip.Content = pNew.Item2.ToString();
                    Ellipse newPoint = new Ellipse();
                    newPoint.Width = 15;
                    newPoint.Height = 15;
                    newPoint.Fill = RelationshipLineIntersectionCorrectionSpaceHelperPointColor;
                    newPoint.Tag = new TaggingObject(relationship);
                    newPoint.ToolTip = tooltip;
                    DiagramCanvas.Children.Add(newPoint);
                    Canvas.SetLeft(newPoint, point.X - (newPoint.Width / 2));
                    Canvas.SetTop(newPoint, point.Y - (newPoint.Height / 2));
                    Canvas.SetZIndex(newPoint, 10);
                }
#endif
                if (lineRouteEditPoints.Where(x => x.Item2 == GeometryHelper.BOX_POSITION.LEFT).Count() == 1 && lineRouteEditPoints.Where(x => x.Item2 == GeometryHelper.BOX_POSITION.RIGHT).Count() == 1)//durchzieht rectangle von links nach rechts oder umgekehrt (horizontaler schnitt)
                {
                    double y;
                    var sourceRectangleBounds = isBoxSource ? relationship.Source.GetBounds() : relationship.Target.GetBounds();
                    if (directionStartAndEnd.isAbove)//target is higher than source
                    {
                        y = sourceRectangleBounds.Bottom + 10;
                    }
                    else//target is lower than source
                    {
                        y = sourceRectangleBounds.Top - 10;
                    }
                    Dictionary<GeometryHelper.BOX_POSITION, Point> NewPoint = new Dictionary<GeometryHelper.BOX_POSITION, Point>();
#if RELATIONSHIP_LINE_HORIZONTAL_HORIZONTAL_CORRECTION_DEBUG
                    foreach (var p in lineRouteEditPoints)
                    {
                        var tooltip = new ToolTip();
                        tooltip.Content = $"DIRECTION POINT {(y < 0 ? "lower dir" : "upper dir")}, relation: {relationship.ToString()}";
                        Ellipse newPoint = new Ellipse();
                        newPoint.Width = 15;
                        newPoint.Height = 15;
                        newPoint.Fill = new SolidColorBrush(Color.FromRgb(155, 244, 231));
                        newPoint.Tag = new TaggingObject(relationship);
                        newPoint.ToolTip = tooltip;
                        DiagramCanvas.Children.Add(newPoint);
                        var pP = new Point(p.Item1.X - (newPoint.Width / 2), y + (newPoint.Height / 2));
                        Canvas.SetLeft(newPoint, pP.X);
                        Canvas.SetTop(newPoint, pP.Y);
                        Canvas.SetZIndex(newPoint, 10);
                        NewPoint.Add(p.Item2, pP);
                    }
                    var iP1 = NewPoint.Where(x => x.Key == GeometryHelper.BOX_POSITION.LEFT)?.First().Value;
                    var iP2 = NewPoint.Where(x => x.Key == GeometryHelper.BOX_POSITION.RIGHT)?.Last().Value;
                    var startPoint = isBoxSource ? start.Value : end.Value;
                    Line startSeg1 = new Line();
                    startSeg1.Stroke = new SolidColorBrush(Color.FromRgb(155, 244, 231));
                    startSeg1.StrokeThickness = 2;
                    startSeg1.X1 = startPoint.X;
                    startSeg1.Y1 = startPoint.Y;
                    startSeg1.X2 = iP1.Value.X;
                    startSeg1.Y2 = startPoint.Y;
                    startSeg1.Tag = new TaggingObject(relationship);

                    Line startSeg2 = new Line();
                    startSeg2.Stroke = new SolidColorBrush(Color.FromRgb(155, 244, 231));
                    startSeg2.StrokeThickness = 2;
                    startSeg2.X1 = iP1.Value.X;
                    startSeg2.Y1 = startPoint.Y;
                    startSeg2.X2 = iP1.Value.X;
                    startSeg2.Y2 = iP1.Value.Y;
                    startSeg2.Tag = new TaggingObject(relationship);

                    Line startSeg3 = new Line();
                    startSeg3.Stroke = new SolidColorBrush(Color.FromRgb(155, 244, 231));
                    startSeg3.StrokeThickness = 2;
                    startSeg3.X1 = iP1.Value.X;
                    startSeg3.Y1 = iP1.Value.Y;
                    startSeg3.X2 = iP2.Value.X;
                    startSeg3.Y2 = iP2.Value.Y;
                    startSeg3.Tag = new TaggingObject(relationship);

                    Line startSeg4 = new Line();
                    startSeg4.Stroke = new SolidColorBrush(Color.FromRgb(155, 244, 231));
                    startSeg4.StrokeThickness = 2;
                    startSeg4.X1 = iP2.Value.X;
                    startSeg4.Y1 = iP2.Value.Y;
                    startSeg4.X2 = iP2.Value.X;
                    startSeg4.Y2 = start.Value.Y;
                    startSeg4.Tag = new TaggingObject(relationship);


                    DiagramCanvas.Children.Add(startSeg1);
                    DiagramCanvas.Children.Add(startSeg2);
                    DiagramCanvas.Children.Add(startSeg3);
                    DiagramCanvas.Children.Add(startSeg4);
#endif
                }
                else if (getHorizontalMovingPoint?.Item1 != InvalidPoint && getVerticalMovingPoint?.Item1 != InvalidPoint)//rectangle wird vertical durchzogen
                {
                    var point = new Point(getHorizontalMovingPoint?.Item1.X ?? 0, getVerticalMovingPoint?.Item1.Y ?? 0);
                    if (point.X == 0 || point.Y == 0)
                    {

                    }
#if RELATIONSHIP_LINE_HORIZONTAL_VERTICAL_CORRECTION_DEBUG
                    var tooltip = new ToolTip();
                    tooltip.Content = $"STÜTZPUNKT (segmentAtoB and segmentBtoC)->relation: {relationship.ToString()}";
                    Ellipse stuetzPunkt = new Ellipse();
                    stuetzPunkt.Width = 15;
                    stuetzPunkt.Height = 15;
                    stuetzPunkt.Fill = RelationshipLineIntersectionCorrectionSpaceHelperPoint2Color;
                    stuetzPunkt.Tag = new TaggingObject(relationship);
                    stuetzPunkt.ToolTip = tooltip;
                    DiagramCanvas.Children.Add(stuetzPunkt);
                    Canvas.SetLeft(stuetzPunkt, point.X - (stuetzPunkt.Width / 2));
                    Canvas.SetTop(stuetzPunkt, point.Y - (stuetzPunkt.Height / 2));
                    Canvas.SetZIndex(stuetzPunkt, 10);
#endif

                    var segmentAtoB = ("segmentAtoB", new List<Point>(2) { (getVerticalMovingPoint?.Item1 ?? InvalidPoint), point });
                    var segmentBtoC = ("segmentBtoC", new List<Point>(2) { (getHorizontalMovingPoint?.Item1 ?? InvalidPoint), point });
#if RELATIONSHIP_LINE_HORIZONTAL_VERTICAL_CORRECTION_DEBUG
                    ICollection<(string, List<Point>)> segments = new List<(string, List<Point>)>() { segmentAtoB, segmentBtoC };
                    foreach (var segment in segments)
                    {
                        var tTip = new ToolTip();
                        tTip.Content = $"{segment.Item1}->relation: {relationship.ToString()}";
                        var p1 = segment.Item2[0];
                        var p2 = segment.Item2[1];
                        Debug.WriteLine($"{p1.ToString()}<--->{p2.ToString()}");
                        var linesegment = new Line();
                        linesegment.Stroke = RelationshipLineIntersectionCorrectionSpaceHelperPoint2Color;
                        linesegment.StrokeThickness = 2;
                        linesegment.StrokeDashArray = new DoubleCollection { 2, 2 };
                        linesegment.X1 = p1.X;
                        linesegment.Y1 = p1.Y;
                        linesegment.X2 = p2.X;
                        linesegment.Y2 = p2.Y;
                        linesegment.Tag = new TaggingObject(relationship);
                        linesegment.ToolTip = tTip;
                        DiagramCanvas.Children.Add(linesegment);
                    }
#endif
                    pathSegments.Add(new LineSegment(startSegmentPositions.start, true));
                    pathSegments.Add(new LineSegment(startSegmentPositions.mid, true));
                    LineSegment lineSegmentSegAtoB = new LineSegment(getVerticalMovingPoint?.Item1 ?? InvalidPoint, true);
                    LineSegment lineSegmentStuetzPunkt = new LineSegment(point, true);
                    LineSegment lineSegmentSegBtoC = new LineSegment(getHorizontalMovingPoint?.Item1 ?? InvalidPoint, true);
                    pathSegments.Add(lineSegmentSegAtoB);
                    pathSegments.Add(lineSegmentStuetzPunkt);
                    pathSegments.Add(lineSegmentSegBtoC);

                    foreach (var segmentPoint in lineSegmentDefinitionPoints.Where(x => x.Key != sourceStartSegmentName).SelectMany(x => x.Value))
                    {
                        LineSegment segmentPointLine = new LineSegment(segmentPoint, true);
                        pathSegments.Add(segmentPointLine);
                    }
                }
                if (pathSegments.Any())
                {
                    pathFigure.Segments.Clear();
                    pathSegments.ForEach(_ => pathFigure.Segments.Add(_));
                }
            }
            return pathGeometry;
        }

        private Point CalculateTextOffset(Point linePoint, Point entityPoint, string text)
        {
            const int padding = 10; 

            var angle = Math.Atan2(linePoint.Y - entityPoint.Y, linePoint.X - entityPoint.X);
            var xOffset = Math.Cos(angle) * padding;
            var yOffset = Math.Sin(angle) * padding;

            return new Point(linePoint.X - xOffset, linePoint.Y - yOffset);
        }

        private void AddTextToCanvas(Relationship relationship, string text, Point position)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = Brushes.Black,
                Background = Brushes.White,
                Tag = new TaggingObject(relationship),
            };

            Canvas.SetLeft(textBlock, position.X);
            Canvas.SetTop(textBlock, position.Y);

            DiagramCanvas.Children.Add(textBlock);
        }

        private (Point? start, Point? end, int sourcePropertyIndex, int targetPropertyIndex) GetLineEndpoints(EntityUiModel source, EntityUiModel target, string sourceProperty, string targetProperty)
        {
            var sourcePoint = source.Properties[sourceProperty].GetPosition();
            var targetPoint = target.Properties[targetProperty].GetPosition();


            return (sourcePoint, targetPoint, source.Properties.Keys.ToList().IndexOf(sourceProperty), target.Properties.Keys.ToList().IndexOf(targetProperty));
        }

        private void StartAddingRelationship(EntityUiModel startEntity)
        {
            _tempStartEntity = startEntity;

            var mousePos = Mouse.GetPosition(DiagramCanvas);
            var startProperty = startEntity.Entity.Properties.Keys.First(); 
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();
            var pos = GeometryHelper.GetAbsolutePositionOfChild(DiagramCanvas, startEntity.Properties[startProperty].RelationAnchor);
            if(pos == null)
            {
                return;
            }
            pathFigure.StartPoint = pos.Value; 
            var bezierSegment = new BezierSegment(
                pathFigure.StartPoint,      
                mousePos,                   
                mousePos,                   
                true                       
            );
            pathFigure.Segments.Add(bezierSegment);
            pathGeometry.Figures.Add(pathFigure);

            _tempRelationshipPath = new Path
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Data = pathGeometry
            };

            DiagramCanvas.Children.Add(_tempRelationshipPath);
        }
        private bool _controlPointStartDrag = false;
        private Point _controlPointStartDragPosition = default;
        private Ellipse _selectedControlPointForDrag;
        private Path GetPathFromRelation(Relationship relationship)
        {
            return DiagramCanvas.Children.OfType<Path>().Where(x => x.Tag is TaggingObject tagging && tagging.TagObject == relationship).FirstOrDefault(); 
        }
        private List<Ellipse> GetPathControlPoints(Relationship relationship)
        {
            return DiagramCanvas.Children.OfType<Ellipse>().Where(x => x.Tag is TaggingObject tagging && tagging.TagObject == relationship).ToList();
        }
        private async void RenderPathControlPoints(PathGeometry pathGeometry, Relationship relationship)
        {
            PathFigure pathFigure = null;
            Point startPoint = new Point();
            PathSegmentCollection segments = null;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var currentlyExistingControlPoints = GetPathControlPoints(relationship);
                if (currentlyExistingControlPoints.Any())
                {
                    foreach (var controlPoint in currentlyExistingControlPoints)
                    {
                        DiagramCanvas.Children.Remove(controlPoint);
                    }
                }

                pathFigure = pathGeometry.Figures.FirstOrDefault();
                if (pathFigure != null)
                {
                    startPoint = pathFigure.StartPoint;
                    segments = pathFigure.Segments.Clone();
                }
            });

            if (pathFigure == null)
                return;

            var pointsFromSegments = segments.OfType<LineSegment>().Select(x => x.Point).ToList();
            var result = await Task.Run<List<Point>>(() =>
            {
                var newSegments = new List<Point>();
                var pathLength = GeometryHelper.CalculatePathLength(startPoint, pointsFromSegments);

                var numberOfPoints = (int)Math.Ceiling(pathLength / relationship.AmountOfControlPoints);
                if (numberOfPoints < 2)
                    numberOfPoints = 2;

                for (int i = 0; i < numberOfPoints; i++)
                {
                    var distance = i * pathLength / (numberOfPoints - 1);
                    var pointOnPath = GeometryHelper.GetPointAlongPath(startPoint, pointsFromSegments, distance);
                    newSegments.Add(pointOnPath);
                }

                this.Dispatcher.Invoke(() =>
                {
                    pathFigure.Segments.Clear();
                    int i = 0;
                    foreach (var segment in newSegments)
                    {
                        if (!double.IsNaN(segment.X) && !double.IsNaN(segment.Y))
                        {
                            var lineSegment = new LineSegment(segment, true);
                            if (i > 1 && i < newSegments.Count() - 2)
                            {
                                CreateControlPoint(relationship, segment, lineSegment.GetHashCode());
                            }
                            pathFigure.Segments.Add(lineSegment);
                            Debug.WriteLine(lineSegment.Point);
                        }
                        i++;
                    }
                });
                return newSegments;
            });
            
        }
        private Ellipse CreateControlPoint(Relationship relationship, Point position, int lineSegmentHashCode, string toolTipText = null)
        {
            ToolTip toolTip = new ToolTip();
            toolTip.Content = $"{toolTipText}";
            var controlPoint = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.Red,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Tag = new TaggingObject(relationship, new object[1] { lineSegmentHashCode }),
                Visibility = _moveLines ? Visibility.Visible : Visibility.Hidden,
                ToolTip = toolTip
            };
            DiagramCanvas.Children.Add(controlPoint);
            Canvas.SetLeft(controlPoint, position.X - (controlPoint.Width / 2));
            Canvas.SetTop(controlPoint, position.Y - (controlPoint.Width / 2));
            Canvas.SetZIndex(controlPoint, 100);
            return controlPoint;
        }

        private void MultiselectedEnabledToggle_Click(object sender, RoutedEventArgs e)
        {

            _entities.Where(x => x.IsSelected).ToList().ForEach(x => { x.SelectionState(false); });
        }

        private void MoveLinesToggle_Click(object sender, RoutedEventArgs e)
        {
            _moveLines = !_moveLines;
            foreach(var relationship in _relationships)
            {
                var currentlyExistingControlPoints = GetPathControlPoints(relationship);
                foreach(var controlPoint in currentlyExistingControlPoints)
                {
                    controlPoint.Visibility = _moveLines?Visibility.Visible: Visibility.Hidden;
                }
            }
        }

        private void GridToggle_Click(object sender, RoutedEventArgs e)
        {
            _enableGrid = !_enableGrid;
            RenderGrid();
        }
    }
}