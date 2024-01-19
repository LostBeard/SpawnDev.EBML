using SpawnDev.EBML;

namespace EBMLViewer.Controls
{

    public static class EBMLFormsControls
    {
        public static Dictionary<Type, Type> ElementToControlTypeMap { get; } = new Dictionary<Type, Type> {
            { typeof(BaseElement), typeof(BaseElementView) },
            //{ typeof(MasterElement), typeof(MasterElementView) },
            //{ typeof(FloatElement), typeof(FloatElementView) },
            //{ typeof(UintElement), typeof(UintElementView) },
            //{ typeof(IntElement), typeof(IntElementView) },
            //{ typeof(BinaryElement), typeof(BinaryElementView) },
            //{ typeof(DateElement), typeof(DateElementView) },
            //{ typeof(UnknownElement), typeof(UnknownElementView) },
            //{ typeof(EBMLElement), typeof(EBMLElementView) },
            //{ typeof(SimpleBlockElement), typeof(SimpleBlockElementView) },
            //{ typeof(TrackEntryElement), typeof(TrackEntryElementView) },
        };
    }
    public interface IElementControl
    {
        void LoadElement(BaseElement element);
    }
}
