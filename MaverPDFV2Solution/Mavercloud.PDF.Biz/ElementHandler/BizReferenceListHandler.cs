using iText.Layout;
using iText.Layout.Element;
using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.General.Entities;
using Mavercloud.PDF.General.Helpers;
using Mavercloud.PDF.Biz.Pages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Biz.ElementHandler
{
    public class BizReferenceListHandler : ListHandler
    {
        private Dictionary<string, ParagraphStyleInfo> referenceStyles;

        public BizReferenceListHandler(object sourceData, 
            ElementItem item, 
            string styleName, 
            Document document, Dictionary<string, ParagraphStyleInfo> referenceStyles) 
            : base(sourceData, item, styleName, document)
        {
            this.referenceStyles = referenceStyles;
        }

        public override ListItem CreateItem(object valueObj, ElementItem listItemStyleItem, string symbol)
        {

            if (valueObj is Reference && referenceStyles != null)
            {
                var refId = ((Reference)valueObj).OriginalId;

                ParagraphStyleInfo itemStyleInfo = null;
                if (referenceStyles.ContainsKey(refId))
                {
                    itemStyleInfo = referenceStyles[refId];
                }
                if (itemStyleInfo != null)
                {
                    var clonedItem = StyleHelper.GetElementItemByParagraphStyle(listItemStyleItem, itemStyleInfo);
                    return base.CreateItem(valueObj, clonedItem, symbol);
                }
                else
                {
                    return base.CreateItem(valueObj, listItemStyleItem, symbol);
                }
            }
            else
            {
                return base.CreateItem(valueObj, listItemStyleItem, symbol);
            }
        }
        public override void AddItemToList(int index)
        {
            var currentItem = listItems[index];
            if (index < listItems.Count - 1)
            {
                currentItem.SetNextRenderer(new ReferenceListItemRenderer(index, currentItem, listItems[index + 1], document));
                
            }
            currentItem.SetKeepTogether(true);
            base.AddItemToList(index);
        }
    }
}
