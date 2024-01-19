using SpawnDev.EBML;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace EBMLViewer.Controls
{
    public partial class BaseElementView : UserControl, IElementControl
    {
        public BaseElement Element { get; private set; }
        public BaseElementView()
        {
            InitializeComponent();
        }

        public void LoadElement(BaseElement element)
        {
            Element = element;
            var count = Math.Min(Element.Length, 4 * 1024);
            var trimmed = count != Element.Length;
            var hex = BytesToHexView(Element.Stream, count);
            if (trimmed)
            {
                hex += "..";
            }
            textBox1.Text = hex;
        }
        string BytesToHexView(Stream? stream, long maxLength = 1024, long startPos = 0, int rowSize = 16)
        {
            var sb = new StringBuilder();
            if (stream != null)
            {
                for (var i = 0; i < maxLength; i += rowSize)
                {
                    var bytes = stream.ReadBytes(startPos + i, rowSize);
                    if (bytes.Length > 0)
                    {
                        var hexi = Convert.ToHexString(BitConverter.GetBytes((uint)i));
                        sb.Append($"{hexi} ");
                        for (var n = 0; n < bytes.Length; n++)
                        {
                            var b = bytes[n];
                            var hex = Convert.ToHexString(new[] { b });
                            sb.Append($"{hex} ");
                        }
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }
    }
}
