using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace SpawnDev.EBML.StreamElements
{
    public class BaseElement
    {
        private ElementHeader _Header;
        public ElementHeader Header
        {
            get
            {
                _Header.Size = (ulong)DataStream.Length;
                return _Header;
            }
        }
        public Stream DataStream { get; set; }
        /// <summary>
        /// Copies the entire element to the specified stream
        /// </summary>
        /// <param name="stream"></param>
        public virtual void CopyTo(Stream stream)
        {
            Header.CopyTo(stream);
            DataStream.Position = 0;
            DataStream.CopyTo(stream);
        }
        /// <summary>
        /// Copies the entire element to the specified stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
        {
            await Header.CopyToAsync(stream, cancellationToken);
            DataStream.Position = 0;
            await DataStream.CopyToAsync(stream, cancellationToken);
        }
        public BaseElement(Stream dataStream, ElementHeader elementHeader)
        {
            _Header = elementHeader;
            DataStream = dataStream;
        }
        public BaseElement(ulong id)
        {
            _Header = new ElementHeader(id, 0);
            DataStream = new MemoryStream();
        }
        public BaseElement(ElementHeader elementHeader)
        {
            _Header = elementHeader;
            DataStream = new MemoryStream();
        }
    }
    public class BaseElement<T> : BaseElement
    {
        public BaseElement(Stream dataStream, ElementHeader elementHeader) : base(elementHeader, dataStream)
        {

        }
        public BaseElement(ulong id, T value) : base(new ElementHeader(id,), dataStream)
        {

        }
        protected abstract Stream DataToStream
    }
    public class IntegerElement : BaseElement
    {
        /// <summary>
        /// Constructor used when read from a stream
        /// </summary>
        /// <param name="elementHeader"></param>
        /// <param name="dataStream"></param>
        public IntegerElement(ElementHeader elementHeader, Stream dataStream) : base(elementHeader, dataStream)
        {

        }
        public IntegerElement(ulong id, long value) : base(new ElementHeader(id,), dataStream)
        {

        }
    }
}
