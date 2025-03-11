using Force.DeepCloner;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.General.Element;
using Mavercloud.PDF.General.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Biz.ElementHandler
{
    internal class BizFigureTableHandler : TableHandler
    {
        private Dictionary<string, ParagraphStyleInfo> paragraphStyles;
        public BizFigureTableHandler(object sourceData, 
            ElementItem item, 
            string styleName, 
            Document document, Dictionary<string, ParagraphStyleInfo> paragraphStyles) : base(sourceData, item, styleName, document)
        {
            this.paragraphStyles = paragraphStyles;
        }


        public override Cell CreateCellForListObject(object obj, ElementItem subItem)
        {
            if (subItem.Name == "FigureCaption")
            {
                var kvp = (KeyValuePair<string, string>)obj;
                var clonedItem = subItem;
                if (paragraphStyles != null && paragraphStyles.ContainsKey(kvp.Key))
                {
                    var paragraphStyle = paragraphStyles[kvp.Key];
                    clonedItem = subItem.DeepClone();
                    StyleHelper.SetElementItemParagraphStyle(clonedItem.Items[0], paragraphStyle);
                }
                return base.CreateCellForListObject(kvp.Value, clonedItem);
            }
            else
            {
                return base.CreateCellForListObject(obj, subItem);
            }
            
        }
    }
}
