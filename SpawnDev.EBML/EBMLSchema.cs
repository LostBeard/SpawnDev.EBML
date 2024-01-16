using SpawnDev.EBML.Matroska;
using SpawnDev.EBML.WebM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnDev.EBML
{
    public abstract class EBMLSchema
    {
        public virtual Type ElementIdEnumType { get; } = typeof(ulong);
        public abstract string DocType { get; }
        public abstract bool ValidChildCheck(Enum[] elementId, Enum childElementId);
        public abstract Type? GetElementType(Enum elementId);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TElementId">The Enum type or ulong type that will be used to represent ElementIds</typeparam>
    public abstract class EBMLSchema<TElementId> : EBMLSchema where TElementId : struct
    {
        public override Type ElementIdEnumType { get; } = typeof(TElementId);
        /// <summary>
        /// Used when trying to determine if an element is a child of an element of unknown size
        /// </summary>
        /// <param name="parentIdChain"></param>
        /// <param name="childElementId"></param>
        /// <returns></returns>
        public abstract bool ValidChildCheck(TElementId[] parentIdChain, TElementId childElementId);
        /// <summary>
        /// Returns the type to be created to represent this element instance
        /// </summary>
        /// <param name="elementId"></param>
        /// <returns></returns>
        public abstract Type? GetElementType(TElementId elementId);

        public override Type? GetElementType(Enum elementId) => GetElementType((TElementId)(object)elementId);

        public override bool ValidChildCheck(Enum[] parentIdChain, Enum childElementId)
        {
            return ValidChildCheck(parentIdChain.Select(o => (TElementId)(object)o).ToArray(), (TElementId)(object)childElementId);
        }
    }
}
