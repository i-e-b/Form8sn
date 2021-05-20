using System;

namespace Form8snCore
{
    public class UsesTypeAttribute : Attribute
    {
        public Type Type { get; set; }
        public UsesTypeAttribute(Type type) => Type = type;
    }
}