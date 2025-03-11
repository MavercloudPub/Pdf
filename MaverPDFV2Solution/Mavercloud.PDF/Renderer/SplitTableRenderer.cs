using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Renderer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    public abstract class SplitTableRenderer : TableRenderer
    {
        private bool startPartial;
        public SplitTableRenderer(Table modelElement) : base(modelElement)
        {
            
        }

        public void SetStartPartial()
        {
            this.startPartial = true;
        }

        public abstract override IRenderer GetNextRenderer();
        

        public override LayoutResult Layout(LayoutContext layoutContext)
        {
            LayoutResult layoutResult = base.Layout(layoutContext);
            if (layoutResult.GetStatus() == LayoutResult.PARTIAL)
            {
                if (!startPartial)
                {
                    startPartial = true;
                    ActionBeforeDrawLeftover(false, layoutResult.GetStatus());
                }
                else
                {
                    ActionBeforeDrawLeftover(true, layoutResult.GetStatus());
                }
                (layoutResult.GetOverflowRenderer() as SplitTableRenderer).SetStartPartial();
            }
            else if (startPartial)
            {
                ActionBeforeDrawLeftover(true, layoutResult.GetStatus());
            }
            else
            {
                ActionBeforeDrawLeftover(false, layoutResult.GetStatus());
            }
            return layoutResult;
        }

        public abstract void ActionBeforeDrawLeftover(bool enabled, int layoutResult);
    }
}
