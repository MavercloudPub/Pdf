using iText.Layout;
using iText.Layout.Element;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.General.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mavercloud.PDF.General.ElementHandler
{
    public class JatsListHandler : ElementBaseHandler<List>
    {
        private JatsListInfo listEntity;

        protected List list;

        protected List<ListItem> listItems;

        protected ElementItem listItemStyleItem;
        public JatsListHandler(JatsListInfo sourceData, ElementItem item, string styleName, Document document) 
            : base(sourceData, item, styleName, document)
        {
            this.listEntity = sourceData;
        }

        public override List Handle()
        {
            list = new List();

            if (item.ListNumberingType.HasValue)
            {
                list.SetListSymbol(item.ListNumberingType.Value);
            }
            else
            {
                if (listEntity.ListType == "order")
                {
                    list.SetListSymbol(iText.Layout.Properties.ListNumberingType.DECIMAL);
                }
                else if (listEntity.ListType == "bullet" && string.IsNullOrEmpty(listEntity.Items[0].Symbol))
                {
                    list.SetListSymbol("\u2022");
                }
            }
            if (!string.IsNullOrEmpty(item.PostSymbolText))
            {
                list.SetPostSymbolText(item.PostSymbolText.Replace("[Space]", " "));
            }
            if (item.ListSymbolAlignment.HasValue)
            {
                list.SetListSymbolAlignment(item.ListSymbolAlignment.Value);
            }
            if (item.ListSymbolIndent.HasValue)
            {
                list.SetSymbolIndent(item.ListSymbolIndent.Value);
            }
            SetStyle(list);
            this.SetFont(list);
            this.SetFontSize(list);
            this.SetFontColor(list);

            listItemStyleItem = item.Items.FirstOrDefault(t => t.Type == ElementType.ListItem);

            if (listItemStyleItem != null)
            {
                listItems = new List<ListItem>();
                foreach (var listItemEntity in listEntity.Items)
                {
                    var listItemHandler = new JatsListItemHandler(listItemEntity, listItemStyleItem, styleName, document);
                    listItems.Add(listItemHandler.Handle() as ListItem);
                }

                for (int i = 0; i < listItems.Count; i++)
                {
                    AddItemToList(i);
                }
            }

            return list;
        }

        public virtual void AddItemToList(int index)
        {
            if (index == listItems.Count - 1 && listItemStyleItem.LastPaddingBottom.HasValue)
            {
                listItems[index].SetPaddingBottom(listItemStyleItem.LastPaddingBottom.Value);
            }
            list.Add(listItems[index]);
        }
    }
}
