using System.Collections.Generic;

namespace Form8snCore.FileFormats
{
    public class MappingInfo
    {
        public MappingInfo() { MappingParameters = new Dictionary<string, string>(); }
        public MappingInfo(MappingType mappingType)
        {
            MappingType = mappingType;
            MappingParameters = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Path to the original data (like `root.parent.child`).
        /// This can also be the name of another mapping (no loops please)
        /// </summary>
        public string[]? DataPath { get; set; }

        /// <summary>
        /// The (pre-programmed) mapping to use
        /// </summary>
        public MappingType MappingType { get; set; }

        /// <summary>
        /// Params for the mapping (if any)
        /// </summary>
        public Dictionary<string,string> MappingParameters { get; set; }
    }
}