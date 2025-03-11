using System;
using System.Collections.Generic;
using System.Text;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Renderer;

namespace Mavercloud.PDF
{
    public static partial class Extensions
    {
        public static float GetHeightOnRendering(this IElement element, float elementWidth, float pageHeight, Document document)
        {
            var subTree = element.CreateRendererSubTree();
            var rootRender = document.GetRenderer();
            var irender = subTree.SetParent(rootRender);
            LayoutResult layoutResult = null;
            layoutResult = irender.Layout(new LayoutContext(new LayoutArea(0, new Rectangle(elementWidth, pageHeight))));
            var elementRec = layoutResult.GetOccupiedArea().GetBBox();
            var height = elementRec.GetHeight();
            return height;
        }

        public static float GetWidthOnRendering(this Paragraph paragraph, Document document)
        {
            var paragraphRenderer = paragraph.CreateRendererSubTree();
            // Do not forget setParent(). Set the dimensions of the viewport as needed
            LayoutResult result = paragraphRenderer.SetParent(document.GetRenderer()).
                    Layout(new LayoutContext(new LayoutArea(0, 
                    new Rectangle(document.GetPdfDocument().GetDefaultPageSize().GetWidth(), document.GetPdfDocument().GetDefaultPageSize().GetHeight()))));
            return ((ParagraphRenderer)paragraphRenderer).GetMinMaxWidth().GetMaxWidth() + 1f;
        }

        public static float GetWidthOnRendering(this List list, Document document)
        {
            var listRenderer = list.CreateRendererSubTree();
            // Do not forget setParent(). Set the dimensions of the viewport as needed
            LayoutResult result = listRenderer.SetParent(document.GetRenderer()).
                    Layout(new LayoutContext(new LayoutArea(0,
                    new Rectangle(document.GetPdfDocument().GetDefaultPageSize().GetWidth(), document.GetPdfDocument().GetDefaultPageSize().GetHeight()))));
            return ((ListRenderer)listRenderer).GetMinMaxWidth().GetMaxWidth() + 1f;
        }
    }
}
