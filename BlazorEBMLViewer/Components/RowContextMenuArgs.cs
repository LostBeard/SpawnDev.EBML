using Microsoft.AspNetCore.Components.Web;
using SpawnDev.EBML.Elements;

namespace BlazorEBMLViewer.Components
{
    public class RowContextMenuArgs
    {
        public MouseEventArgs MouseEventArgs { get; set; }
        public BaseElement Element { get; set; }
        public RowContextMenuArgs(MouseEventArgs mouseEventArgs, BaseElement element)
        {
            MouseEventArgs = mouseEventArgs;
            Element = element;
        }
    }
}
