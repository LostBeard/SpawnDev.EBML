using Microsoft.AspNetCore.Components.Web;
using SpawnDev.EBML.Elements;

namespace BlazorEBMLViewer.Components
{
    public class RowContextMenuArgs
    {
        public MouseEventArgs MouseEventArgs { get; set; }
        public ElementBase Element { get; set; }
        public RowContextMenuArgs(MouseEventArgs mouseEventArgs, ElementBase element)
        {
            MouseEventArgs = mouseEventArgs;
            Element = element;
        }
    }
}
