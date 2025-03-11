using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Renderer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Renders
{
    public class SplittingTableRenderer : TableRenderer
    {
        private SplittingTableRenderer leftover;
        private SplittingTableRenderer toDraw;
        private IList<CellRenderer[]> cellRenderers;
        public SplittingTableRenderer(Table modelElement) : base(modelElement)
        {

        }

        public override LayoutResult Layout(LayoutContext layoutContext)
        {
            var result = base.Layout(layoutContext);
            if (result.GetStatus() == LayoutResult.PARTIAL)
            {
                leftover = (SplittingTableRenderer)result.GetOverflowRenderer();
                cellRenderers = leftover.rows;
                toDraw = (SplittingTableRenderer)result.GetSplitRenderer();
                return new LayoutResult(LayoutResult.FULL, result.GetSplitRenderer().GetOccupiedArea(), null, null);
            }
            else
            {
                return result;
            }
        }

        public override void Draw(DrawContext drawContext)
        {

            if (toDraw != null)
            {
                toDraw.Draw(drawContext);
            }
            else
            {
                base.Draw(drawContext);
            }


        }

        public override IRenderer GetNextRenderer()
        {
            return new SplittingTableRenderer((Table)modelElement);
        }


        public SplittingTableRenderer Leftover
        {
            get { return leftover; }
        }

        public IList<CellRenderer[]> CellRenderers
        {
            get { return cellRenderers; }
        }
    }
}
