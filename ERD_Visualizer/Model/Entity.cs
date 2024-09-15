using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ERD_Visualizer.Model
{
    public class Entity
    {
        public string Name { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public override string ToString()
        {
            return $"{Name}";
        }
    }
    public class PropertyUiModel
    {
        public Rectangle RelationAnchor { get; set; }
        public TextBlock TextBlock { get; set; }
        public Point GetPosition() => new Point(Canvas.GetLeft(RelationAnchor)+(RelationAnchor.Width/2), Canvas.GetTop(RelationAnchor) + (RelationAnchor.Width / 2));
    }
    public class EntityUiModel : ITaggingObject
    {
        public static readonly SolidColorBrush SelectionColor = new SolidColorBrush(Color.FromRgb(224,205,26));
        public const double BoxSizeWidth = 150;
        public const double BoxSizeHeightStepperValue = 40;
        public const double NameTextblockWidth = 145;
        public const double NameTextblockHeight = 20;

        public bool IsSelected { get;private set; }
        public Entity Entity { get; private set; }
        public Point Position { get; private set; }
        public Rectangle Box { get; private set; }
        public TextBlock NameText { get; private set; }
        public ContextMenu ContextMenu { get; private set; }
        public IDictionary<string, PropertyUiModel> Properties { get; private set; }

        public Rect GetBounds()
        {
            return GeometryHelper.GetBounds(this.Box) ; 
        }

        private EntityUiModel(Entity entity,Rectangle box, TextBlock name, ContextMenu contextMenu = null) 
        { 
            Entity = entity;
            Box = box;  
            NameText = name;
            ContextMenu = contextMenu;

            Properties = new Dictionary<string, PropertyUiModel>();
        }
        public override string ToString()
        {
            return $"{Entity}";
        }

        public IEnumerable<FrameworkElement> GetAllUiElements()
        {
            yield return Box;
            yield return NameText;
            yield return ContextMenu;
            if (Properties is not null)
            {
                foreach (var item in Properties)
                {
                    yield return item.Value.TextBlock;
                }
                foreach (var item in Properties)
                {
                    yield return item.Value.RelationAnchor;
                }
            }
        }
        public void SelectionState(bool state)
        {
            IsSelected = state;
            Box.Stroke = IsSelected? SelectionColor : Brushes.Black;
        }
        public void UpdatePosition(Nullable<Point> position = null)
        {
            if(position is not null)
            {
                Position = position.Value;
            }

            Canvas.SetLeft(Box, Position.X);
            Canvas.SetTop(Box, Position.Y);

            Canvas.SetLeft(NameText, Position.X + (Box.Width - NameTextblockWidth) / 2);
            Canvas.SetTop(NameText, Position.Y + 5); 
            UpdatePropertyTextblockPosition();
        }
        public static (double Height,double Width) GetBoxSize(int propertyCount) => (((BoxSizeHeightStepperValue) * propertyCount)+5, BoxSizeWidth);
        public static EntityUiModel Create(Entity entity,Point position, ICollection<ContextMenuItem> contextMenuActions = null)
        {
            var rectSize = GetBoxSize(entity.Properties.Count); 
            var box = new Rectangle
            {
                Width = rectSize.Width,
                Height = rectSize.Height,
                Fill = Brushes.LightCoral,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Tag = entity 
            };

            var nameTextBlock = new TextBlock
            {
                Text = entity.Name,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Tag = entity,
                FontSize=14,
                
            };

            ContextMenu contextMenu =null;
            if(contextMenuActions is not null)
            {
                contextMenu = CreateEntityContextMenu(contextMenuActions);
                box.ContextMenu = contextMenu;
            }

            var entityUiModel = new EntityUiModel(entity, box, nameTextBlock, contextMenu);
            entityUiModel.AddPropertyTextblocks();

            entityUiModel.UpdatePosition(position);

            return entityUiModel;
        }

        public static void AddToCanvs(Canvas canvas,EntityUiModel entityUiModel)
        {
            var elements = entityUiModel.GetAllUiElements().Where(x=> x is not null).ToList();
            foreach (var child in elements)
            {
                if (child.Parent is Panel oldContainer)
                {
                    oldContainer.Children.Remove(child);
                }
                canvas.Children.Add(child);
            }
        }
        public static void RemoveFromCanvs(Canvas canvas, EntityUiModel entityUiModel)
        {
            var elements = entityUiModel.GetAllUiElements().Where(x => x is not null).ToList();
            foreach (var child in elements)
            {
                canvas.Children.Remove(child);
            }
        }

        private void AddPropertyTextblocks()
        {
            foreach (var property in Entity.Properties)
            {
                var propertyText = $"{property.Key}: {property.Value}";
                var propertyTextBlock = new TextBlock
                {
                    Text = propertyText,
                    Foreground = Brushes.Black,
                    Tag = Entity,
                    Margin = new Thickness(5, 0, 0, 0),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,  
                };
                var propertyRelationshipAnchor = new Rectangle
                {
                    Height = 15,
                    Width = 15,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Stroke = new SolidColorBrush(Colors.Black),
                    Margin = new Thickness(0, 0, 5, 0),
                    StrokeThickness = 0.5,
                    Tag=Entity,
                };
                Properties.Add(property.Key, new PropertyUiModel 
                { 
                    RelationAnchor = propertyRelationshipAnchor, 
                    TextBlock = propertyTextBlock 
                });
            }
            UpdatePropertyTextblockPosition();
        }

        private void UpdatePropertyTextblockPosition()
        {
            var yOffset = Position.Y + 25; // Starten Sie etwas weiter unten für die Eigenschaften, unterhalb des Namens und des Rechtecks
            foreach (var property in Properties)
            {
                var anchor = property.Value.RelationAnchor;
                var textblock = property.Value.TextBlock;

                Canvas.SetLeft(anchor, Position.X + 5);
                Canvas.SetTop(anchor, yOffset+3);
                Canvas.SetLeft(textblock, Position.X + anchor.Width + 5);
                Canvas.SetTop(textblock, yOffset+1);

                yOffset += 20;
            }
        }

        public class ContextMenuItem
        {
            public string ItemName { get; set; }
            public ContextMenuActionDelegate Action { get; set; }
            public object[] Params { get; set; }

            // Constructor to initialize the fields
            public ContextMenuItem(string itemName, ContextMenuActionDelegate action, object[] parameters)
            {
                ItemName = itemName;
                Action = action;
                Params = parameters;
            }
        }
        public delegate object ContextMenuActionDelegate(object sender, RoutedEventArgs e, object[] parameters);
        public static ContextMenu CreateEntityContextMenu(ICollection<ContextMenuItem> contextMenuActions)
        {
            var contextMenu = new ContextMenu();
            foreach (var action in contextMenuActions)
            {
                var menuItem = new MenuItem 
                { 
                    Header = action.ItemName 
                };
                menuItem.Click += (sender, e) => action.Action(sender,e, action.Params);
                contextMenu.Items.Add(menuItem);
            }
            return contextMenu;
        }
        public static MenuItem AddContextMenuItem(ContextMenu contextMenu, (string ItemName, ContextMenuActionDelegate Action, object[] Params) contextMenuAction)
        {

            var menuItem = new MenuItem
            {
                Header = contextMenuAction.ItemName
            };
            menuItem.Click += (sender, e) => contextMenuAction.Action(sender, e, contextMenuAction.Params);
            contextMenu.Items.Add(menuItem);
            return menuItem;
        }
    }
}
