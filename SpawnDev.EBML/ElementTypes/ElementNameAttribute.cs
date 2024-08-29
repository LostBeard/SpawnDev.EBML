using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnDev.EBML
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple =true)]
    internal class ElementNameAttribute : Attribute
    {
        public string DocType { get; set; }
        public string Name { get; set; }
        public ElementNameAttribute(string docType, string name)
        {
            DocType = docType;
            Name = name;
        }
    }
}
