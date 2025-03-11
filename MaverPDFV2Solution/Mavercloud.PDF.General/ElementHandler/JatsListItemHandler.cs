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
    public class JatsListItemHandler : ElementBaseHandler<Div>
    {
        private JatsListItemInfo listItemEntity;
        public JatsListItemHandler(JatsListItemInfo sourceData, ElementItem item, string styleName, Document document)
            : base(sourceData, item, styleName, document)
        {
            this.listItemEntity = sourceData;
        }

        public override Div Handle()
        {
            var listItem = new ListItem();
            SetStyle(listItem);
            SetDestination(listItem);
            if (!string.IsNullOrEmpty(item.ListSymbol))
            {
                listItem.SetListSymbol(item.ListSymbol);
            }
            else if (!string.IsNullOrEmpty(listItemEntity.Symbol))
            {
                listItem.SetListSymbol(listItemEntity.Symbol);
            }
            
            foreach (var element in listItemEntity.Elements)
            {
                if (element is ParagraphInfo)
                {
                    var elementItem = item.Items.FirstOrDefault(t => t.Type == ElementType.Paragraph);
                    var paragraphHandler = new ParagraphHandler((element as ParagraphInfo).Text, elementItem, styleName, document);
                    listItem.Add(paragraphHandler.Handle());
                }
                else if (element is JatsListInfo)
                {
                    var elementItem = item.Items.FirstOrDefault(t => t.Type == ElementType.List);

                    var jatsListHandler = new JatsListHandler(element as JatsListInfo, elementItem, styleName, document);
                    listItem.Add(jatsListHandler.Handle());
                }
            }
            return listItem;
        }
    }
}
