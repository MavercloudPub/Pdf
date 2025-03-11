using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Renderer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Renders
{
    public class SplittingDivRenderer : DivRenderer
    {
        private SplittingDivRenderer leftover;
        private SplittingDivRenderer toDraw;
        public SplittingDivRenderer(Div modelElement) : base(modelElement)
        {

        }

        public override LayoutResult Layout(LayoutContext layoutContext)
        {
            var result = base.Layout(layoutContext);
            if (result.GetStatus() == LayoutResult.PARTIAL)
            {
                leftover = (SplittingDivRenderer)result.GetOverflowRenderer();
                toDraw = (SplittingDivRenderer)result.GetSplitRenderer();
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
            return new SplittingDivRenderer((Div)modelElement);
        }

        public SplittingDivRenderer Leftover
        {
            get { return leftover; }
        }
    }
}
