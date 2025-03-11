using iText.Layout;
using iText.Layout.Element;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mavercloud.PDF.ElementHandler
{
    public class ListItemHandler : ElementBaseHandler<Div>
    {
        private ListItem listItem = null;
        public ListItemHandler(object sourceData, ElementItem item, string styleName, Document document)
            : base(sourceData, item, styleName, document)
        {

        }

        
        public override Div Handle()
        {
            if (CheckStyle())
            {
                listItem = new ListItem();
                SetStyle(listItem);
                SetDestination(listItem);
                SetSplitCharacters(listItem);
                SetHypenation(listItem);
                if (!string.IsNullOrEmpty(item.ListSymbol))
                { 
                    listItem.SetListSymbol(item.ListSymbol);
                }

                var childItem = item.Items.FirstOrDefault();
                if (childItem != null)
                {
                    if (childItem.Type == ElementType.Paragraph)
                    {
                        var paraHandler = new ParagraphHandler(sourceData, childItem, styleName, document);
                        listItem.Add(paraHandler.Handle());
                    }
                }
            }
            return listItem;
        }
    }
}
