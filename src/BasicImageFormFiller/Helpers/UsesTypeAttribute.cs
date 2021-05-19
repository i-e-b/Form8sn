using System;

namespace BasicImageFormFiller.Helpers
{
    public class UsesTypeAttribute : Attribute
    {
        public Type Type { get; set; }
        public UsesTypeAttribute(Type type) => Type = type;
    }
}