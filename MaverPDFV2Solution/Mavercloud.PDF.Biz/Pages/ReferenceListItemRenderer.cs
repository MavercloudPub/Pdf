using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Renderer;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Mavercloud.PDF.Biz.Pages
{
    public class ReferenceListItemRenderer : ListItemRenderer
    {
        private ListItem nextItem;
        private Document document;
        private float currentYLine;
        private float pageWidth;
        private int index;
        public ReferenceListItemRenderer(int index, ListItem modelElement, ListItem nextItem, Document document) : base(modelElement)
        {
            this.index = index;
            this.nextItem = nextItem;
            this.document = document;
            this.pageWidth = document.GetPdfDocument().GetDefaultPageSize().GetWidth() - document.GetLeftMargin() - document.GetRightMargin();
        }

        public override void DrawChildren(DrawContext drawContext)
        {
            base.DrawChildren(drawContext);
        }
        public override void Draw(DrawContext drawContext)
        {
            base.Draw(drawContext);
#if DEBUG
            if (index == 16 || index == 36)
            { 
                
            }
#endif
            //if (nextItem != null)
            //{
                
            //    currentYLine = this.GetOccupiedArea().GetBBox().GetBottom();
            //    var remainSpace = currentYLine - document.GetBottomMargin();
            //    if (remainSpace > 3f)
            //    {
            //        var nextItemHeight = nextItem.GetHeightOnRendering(pageWidth, 100000f, document);
            //        if (nextItemHeight > remainSpace)
            //        {
            //            document.Add(new AreaBreak(iText.Layout.Properties.AreaBreakType.NEXT_AREA));
            //        }
            //    }
            //}
        }
    }
}
