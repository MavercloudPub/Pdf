using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Layout;
using iText.Layout.Renderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavercloud.PDF.Renderer
{
    public class CustomColumnDocumentRenderer : ColumnDocumentRenderer
    {
        public CustomColumnDocumentRenderer(Document document, Rectangle[] columns) : base(document, columns)
        {

        }

        public CustomColumnDocumentRenderer(Document document, bool immediateFlush, Rectangle[] columns) : base(document, immediateFlush, columns)
        {
        }

        IDictionary<int, float> leftColumnBottom = new Dictionary<int, float>();
        IDictionary<int, float> rightColumnBottom = new Dictionary<int, float>();

        protected override void FlushSingleRenderer(IRenderer resultRenderer)
        {
            TraverseRecursively(resultRenderer, leftColumnBottom, rightColumnBottom);

            base.FlushSingleRenderer(resultRenderer);
        }

        public void SetColumns(Rectangle[] columns)
        {
            this.columns = columns;
        }

        public void SetPageNumber(int pageNumber)
        {
            
        }

        


        void TraverseRecursively(IRenderer child, IDictionary<int, float> leftColumnBottom, IDictionary<int, float> rightColumnBottom)
        {
            if (child is LineRenderer)
            {
                int page = child.GetOccupiedArea().GetPageNumber();
                if (!leftColumnBottom.ContainsKey(page))
                {
                    leftColumnBottom[page] = 1000;
                }

                if (!rightColumnBottom.ContainsKey(page))
                {
                    rightColumnBottom[page] = 1000;
                }

                bool isLeftColumn = !(child.GetOccupiedArea().GetBBox().GetX() > PageSize.A4.GetWidth() / 2);

                if (isLeftColumn)
                {
                    leftColumnBottom[page] =
                        Math.Min(leftColumnBottom[page], child.GetOccupiedArea().GetBBox().GetBottom());
                }
                else
                {
                    rightColumnBottom[page] = Math.Min(rightColumnBottom[page],
                        child.GetOccupiedArea().GetBBox().GetBottom());
                }
            }
            else
            {
                foreach (IRenderer ownChild in (child is ParagraphRenderer ? (((ParagraphRenderer)child).GetLines().Cast<IRenderer>()) : child.GetChildRenderers()))
                {
                    TraverseRecursively(ownChild, leftColumnBottom, rightColumnBottom);
                }
            }
        }

        public List<float> getDiffs()
        {
            List<float> ans = new List<float>();
            foreach (int pageNum in leftColumnBottom.Keys)
            {
                ans.Add(leftColumnBottom[pageNum] - rightColumnBottom[pageNum]);
            }

            return ans;
        }
    }
}
