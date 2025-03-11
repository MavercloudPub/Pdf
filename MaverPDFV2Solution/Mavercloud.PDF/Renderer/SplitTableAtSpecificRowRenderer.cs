using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Properties;
using iText.Layout.Renderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    public class SplitTableAtSpecificRowRenderer: TableRenderer
    {
        private List<int> breakPoints;
        private int amountOfRowsThatAreGoingToBeRendered = 0;

        public SplitTableAtSpecificRowRenderer(Table modelElement, List<int> breakPoints):base(modelElement)
        {
            this.breakPoints = breakPoints;
        }

        public override IRenderer GetNextRenderer()
        {
            return new SplitTableAtSpecificRowRenderer((Table)modelElement, this.breakPoints);
        }

        public override LayoutResult Layout(LayoutContext layoutContext)
        {
            LayoutResult result = null;
            while (result == null)
            {
                result = AttemptLayout(layoutContext, this.breakPoints);
            }
            var newBreakPoints = new List<int>();
            this.breakPoints.ForEach(t => newBreakPoints.Add(t - this.amountOfRowsThatAreGoingToBeRendered));
            this.breakPoints = newBreakPoints;
            return result;
        }

        private LayoutResult AttemptLayout(LayoutContext layoutContext, List<int> breakPoints)
        {
            LayoutResult layoutResult = base.Layout(layoutContext);
            if (layoutResult.GetStatus() == LayoutResult.FULL || breakPoints == null || !breakPoints.Any())
            {
                this.amountOfRowsThatAreGoingToBeRendered = GetAmountOfRows(layoutResult);
                return layoutResult;
            }
            else
            {
                int breakPointToFix = CalculateBreakPoint(layoutContext);
                if (breakPointToFix >= 0)
                {
                    ForceAreaBreak(breakPointToFix);
                    this.amountOfRowsThatAreGoingToBeRendered = breakPointToFix - 1;
                    return null;
                }
                else
                {
                    return layoutResult;
                }
            }
        }


        private int CalculateBreakPoint(LayoutContext layoutContext)
        {
            LayoutResult layoutResultWithoutSplits = AttemptLayout(layoutContext, null);
            if (layoutResultWithoutSplits == null)
            {
                return int.MinValue;
            }
            int amountOfRowsThatFitWithoutSplit = GetAmountOfRows(layoutResultWithoutSplits);
            int breakPointToFix = int.MinValue;
            foreach (int breakPoint in breakPoints)
            {
                if (breakPoint <= amountOfRowsThatFitWithoutSplit)
                {
                    breakPoints.Remove(breakPoint);
                    if (breakPoint < amountOfRowsThatFitWithoutSplit && breakPoint > breakPointToFix)
                    {
                        breakPointToFix = breakPoint;
                    }
                }
            }
            return breakPointToFix;
        }

        private void ForceAreaBreak(int rowIndex)
        {
            rowIndex++;
            if (rowIndex <= rows.Count())
            {
                foreach (CellRenderer cellRenderer in rows[rowIndex])
                {
                    if (cellRenderer != null)
                    {
                        cellRenderer.GetChildRenderers()
                                .Insert(0, new AreaBreakRenderer(new AreaBreak(AreaBreakType.NEXT_PAGE)));
                        break;
                    }
                }
            }
            
        }

        private static int GetAmountOfRows(LayoutResult layoutResult)
        {
            if (layoutResult.GetSplitRenderer() == null)
            {
                return 0;
            }
            return ((SplitTableAtSpecificRowRenderer)layoutResult.GetSplitRenderer()).rows.Count();
        }
    }
}

