using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Renderer;
using Mavercloud.PDF.Elements;

namespace Mavercloud.PDF.ElementHandler
{
    public class ListHandler : ElementBaseHandler<List>
    {
        protected List list = null;


        protected List<ListItem> listItems;
        public ListHandler(object sourceData, ElementItem item, string styleName, Document document)
            : base(sourceData, item, styleName, document)
        {
            listItems = new List<ListItem>();
        }

        public override List Handle()
        {
            if (CheckStyle())
            {
                list = new List();
                if (item.ListNumberingType.HasValue)
                {
                    list.SetListSymbol(item.ListNumberingType.Value);
                }
                else if (!string.IsNullOrEmpty(item.ListSymbol))
                {
                    if (item.ListSymbol == "[Space]")
                    {
                        list.SetListSymbol("");
                    }
                    else
                    {
                        list.SetListSymbol(item.ListSymbol);
                    }
                }

                if (!string.IsNullOrEmpty(item.PostSymbolText))
                {
                    list.SetPostSymbolText(item.PostSymbolText.Replace("[Space]", " "));
                }
                if (!string.IsNullOrEmpty(item.PreSymbolText))
                {
                    list.SetPreSymbolText(item.PreSymbolText.Replace("[Space]", " "));
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

                var listItemStyleItem = GetListItemStyleItem();

                if (listItemStyleItem != null)
                {
                    var valuePath = item.ValuePath;
                    if (string.IsNullOrEmpty(valuePath))
                    {
                        valuePath = listItemStyleItem.ValuePath;
                    }
                    var valueObject = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(item.ValuePath, sourceData);

                    if (valueObject.GetType().IsGenericType && valueObject is IEnumerable)
                    {
                        var listObject = valueObject as IList;
                        foreach (var obj in listObject)
                        {
                            var symbol = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue("Symbol", obj) as string;
                            var listItem = CreateItem(obj, listItemStyleItem, symbol);
                            listItems.Add(listItem);

                        }
                        for (int i = 0; i < listItems.Count; i++)
                        {
                            AddItemToList(i);
                        }

                    }
                }
            }
            return list;
        }

        protected virtual ElementItem GetListItemStyleItem()
        { 
            return item.Items.FirstOrDefault(t => t.Type == ElementType.ListItem);
        }

        public virtual ListItem CreateItem(Object valueObj, ElementItem listItemStyleItem, string symbol)
        {
            if (symbol != null)
            {
                listItemStyleItem.ListSymbol = symbol;
            }
            var listItemHandler = new ListItemHandler(valueObj, listItemStyleItem, styleName, document);
            var listItem = listItemHandler.Handle() as ListItem;
            return listItem;
        }

        public virtual void AddItemToList(int index)
        {
            list.Add(listItems[index]);
        }
    }
}
