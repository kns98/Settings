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
            // PropertyGridのCategoryを設定する
            var categoryTable = new Dictionary<string, Attribute>
            {
                { nameof(BoolSetting), new CategoryAttribute("組み込みのデータ型") },
                { nameof(StringSetting), new CategoryAttribute("組み込みのデータ型") },
                { nameof(StringCollectionSetting), new CategoryAttribute("複合データ型") },
                { nameof(DateTimeSetting), new CategoryAttribute("複合データ型") },
                { nameof(IntSetting), new CategoryAttribute("組み込みのデータ型") }
            };
            addAttribute(categoryTable);

            // PropertyGridのHelpテキストを設定する
            var descriptionTable = new Dictionary<string, Attribute>
            {
                { nameof(BoolSetting), new DescriptionAttribute("bool型の設定") },
                { nameof(StringSetting), new DescriptionAttribute("string型の設定") },
                { nameof(StringCollectionSetting), new DescriptionAttribute("複数のstring型の設定") },
                { nameof(DateTimeSetting), new DescriptionAttribute("DateTime型の設定") },
                { nameof(IntSetting), new DescriptionAttribute("int型の設定") }
            };
            addAttribute(descriptionTable);
        }

        /// <summary>
        ///     プロパティに属性を追加する
        /// </summary>
        /// <param name="attributeTable">Key:プロパティ名、Value:追加する属性</param>
        private void addAttribute(Dictionary<string, Attribute> attributeTable)
        {
            if (attributeTable == null) return;

            var properties = TypeDescriptor.GetProperties(this);
            foreach (PropertyDescriptor p in properties)
            {
                Attribute attribute;
                if (attributeTable.TryGetValue(p.Name, out attribute))
                {
                    // 属性を追加する。
                    // 本当はMemberDescriptor.Attributes.Addのようにしたいのだが、Attributes属性はgetだけ定義されている。
                    // そのためリフレクションを使って属性を追加する
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