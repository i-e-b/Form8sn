using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Form8snCore.FileFormats;

namespace Form8snCore.HelpersAndConverters
{
    public class EnumOption
    {
        public EnumOption(string name, string? description)
        {
            Name = name;
            Description = description ?? name;
        }
        
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class EnumOptions
    {
        public static IEnumerable<EnumOption> AllDisplayFormatTypes()
        {
            return FillWithEnum(typeof(DisplayFormatType));
        }

        private static IEnumerable<EnumOption> FillWithEnum(Type type)
        {
            var items = new List<EnumOption>();
            var enumValues = type.GetFields().Where(f=> !f.IsSpecialName);
            foreach (var info in enumValues)
            {
                var desc = info.CustomAttributes?.FirstOrDefault(a => a.AttributeType == typeof(DescriptionAttribute));
                var maybeDescription = desc?.ConstructorArguments.FirstOrDefault().Value.ToString();
                items.Add(new EnumOption(info.Name, maybeDescription));
            }
            return items;
        }
    }
}