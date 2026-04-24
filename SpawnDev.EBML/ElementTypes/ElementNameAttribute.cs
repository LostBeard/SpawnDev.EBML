using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class ElementNameAttribute : Attribute
    {
        public string? Name { get; set; }
        public string[] DocTypes { get; set; }
        public ElementNameAttribute(string name, params string[] docTypes)
        {
            Name = name;
            DocTypes = docTypes;
        }
    }
    //[AttributeUsage(AttributeTargets.Class)]
    //internal class DocTypeAttribute : Attribute
    //{
    //    public string[] DocTypes { get; set; }
    //    public DocTypeAttribute(params string[] docTypes)
    //    {
    //        DocTypes = docTypes;
    //    }
    //}
    //[AttributeUsage(AttributeTargets.Class)]
    //internal class ElementNameAttribute : Attribute
    //{
    //    public ulong Id { get; set; }
    //    public string[] DocTypes { get; set; }
    //    public ElementNameAttribute(string hexId, params string[] docTypes)
    //    {
    //        Id = EBMLConverter.ElementIdFromHexId(hexId);
    //        DocTypes = docTypes;
    //    }
    //    public ElementNameAttribute(ulong id, params string[] docTypes)
    //    {
    //        Id = id;
    //        DocTypes = docTypes;
    //    }
    //}
}
