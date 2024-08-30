using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML
{
    /// <summary>
    /// Cache item
    /// </summary>
    internal class MasterCacheItem
    {
        /// <summary>
        /// Element instance path
        /// </summary>
        public string InstancePath { get; set; }
        /// <summary>
        /// When marked true the entire master element has been iterated and Children contains all of it's children<br/>
        /// This allows the already iterated result to be returned or searched instead of reiterating over the file
        /// </summary>
        public bool Complete { get; set; }
        /// <summary>
        /// Children
        /// </summary>
        public Dictionary<string, ElementStreamInfo> Children = new Dictionary<string, ElementStreamInfo>();
    }
}
