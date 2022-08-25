using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace SettingsDialog.Properties
{
    internal sealed partial class Settings
    {
        public Settings()
        {
            //Set the Category of the PropertyGrid
            var categoryTable = new Dictionary<string, Attribute>
            {
                { nameof(BoolSetting), new CategoryAttribute("Built-in data types") },
                { nameof(StringSetting), new CategoryAttribute("Built-in data types") },
                { nameof(StringCollectionSetting), new CategoryAttribute("Complex data type") },
                { nameof(DateTimeSetting), new CategoryAttribute("Complex data type") },
                { nameof(IntSetting), new CategoryAttribute("Built-in data types") }
            };
            addAttribute(categoryTable);

            //Set the Help text in the PropertyGrid
            var descriptionTable = new Dictionary<string, Attribute>
            {
                { nameof(BoolSetting), new DescriptionAttribute("Setting bool") },
                { nameof(StringSetting), new DescriptionAttribute("Setting string type") },
                { nameof(StringCollectionSetting), new DescriptionAttribute("Setting multiple string types") },
                { nameof(DateTimeSetting), new DescriptionAttribute("Set DateTime type") },
                { nameof(IntSetting), new DescriptionAttribute("Set int type") }
            };
            addAttribute(descriptionTable);
        }

        /// <summary>
        //Add an attribute to a property
        /// </summary>
        //<param name="attributeTable">Key: property name, Value: attribute to add</param>
        private void addAttribute(Dictionary<string, Attribute> attributeTable)
        {
            if (attributeTable == null) return;

            var properties = TypeDescriptor.GetProperties(this);
            foreach (PropertyDescriptor p in properties)
            {
                Attribute attribute;
                if (attributeTable.TryGetValue(p.Name, out attribute))
                {
                    //Add attributes.
                    //I really want it to be like MemberDescriptor.Attributes.Add, but the Attributes attribute is defined only by get.
                    //So use reflection to add attributes
                    var fi = p.Attributes.GetType()
                        .GetField("_attributes", BindingFlags.NonPublic | BindingFlags.Instance);
                    var attrs = fi.GetValue(p.Attributes) as Attribute[];
                    var listAttr = new List<Attribute>();
                    if (attrs != null) listAttr.AddRange(attrs);
                    listAttr.Add(attribute);
                    fi.SetValue(p.Attributes, listAttr.ToArray());
                }
            }
        }
    }
}