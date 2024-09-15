using System.Windows.Shapes;

namespace ERD_Visualizer.Model
{
    public class Relationship : ITaggingObject
    {
        public EntityUiModel Source { get; set; }
        public EntityUiModel Target { get; set; }
        public string StartText { get; set; }
        public string MidText { get; set; }
        public string EndText { get; set; }
        public Line Line { get; set; }
        public string SourceProperty { get; set; }
        public string TargetProperty { get; set; }
        public int AmountOfControlPoints { get; set; } = 20;
        public override string ToString()
        {
            return $"{Source}.{SourceProperty}<->{Target}.{TargetProperty}";
        }
    }
}
