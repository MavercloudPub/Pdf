using iText.Layout.Element;
using iText.Layout.Renderer;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    public class BorderMarginTableRenderer: TableRenderer
    {
        private BorderMarginStyle borderMarginStyle;

        private bool top;

        private bool bottom;

        private bool right;

        private bool left;
        public BorderMarginTableRenderer(Table modelElement, BorderMarginStyle borderMarginStyle) : base(modelElement)
        {
            this.borderMarginStyle = borderMarginStyle;
            this.top = borderMarginStyle.Top.GetValueOrDefault();
            this.bottom = borderMarginStyle.Bottom.GetValueOrDefault();
            this.right = borderMarginStyle.Right.GetValueOrDefault();
            this.left = borderMarginStyle.Left.GetValueOrDefault();
        }

        public override IRenderer GetNextRenderer()
        {
            return new BorderMarginTableRenderer((Table)modelElement, borderMarginStyle);
        }

        protected override TableRenderer[] Split(int row, bool hasContent, bool cellWithBigRowspanAdded)
        {
            TableRenderer[] renderers = base.Split(row, hasContent, cellWithBigRowspanAdded);

            // Being inside this method implies that split has occurred

            TableRenderer[] results = base.Split(row, hasContent, cellWithBigRowspanAdded);

            BorderMarginTableRenderer splitRenderer = (BorderMarginTableRenderer)results[0];

            // iText shouldn't draw the bottom split renderer's border,
            // because there is an overflow renderer
            if (this.borderMarginStyle.Bottom.GetValueOrDefault())
            {
                splitRenderer.bottom = false;
            }

            // If top is true, the split renderer is the first renderer of the document.
            // If false, iText has already processed the first renderer
            splitRenderer.top = this.top;

            BorderMarginTableRenderer overflowRenderer = (BorderMarginTableRenderer)results[1];

            // iText shouldn't draw the top overflow renderer's border
            if (this.borderMarginStyle.Top.GetValueOrDefault() && hasContent)
            {
                overflowRenderer.top = false;
            }

            return results;
        }

        protected override void DrawBorders(DrawContext drawContext)
        {

            var rectangle = occupiedArea.GetBBox();

            var canvas = drawContext.GetCanvas()
                .SaveState()
                .SetLineWidth(borderMarginStyle.Width.GetValueOrDefault(1));

            if (this.bottom)
            {
                canvas.MoveTo(rectangle.GetRight(), rectangle.GetBottom() - borderMarginStyle.Margin.GetValueOrDefault(0));
                canvas.LineTo(rectangle.GetLeft(), rectangle.GetBottom() - borderMarginStyle.Margin.GetValueOrDefault(0));
            }
            if (this.top)
            {
                canvas.MoveTo(rectangle.GetRight(), rectangle.GetTop() + borderMarginStyle.Margin.GetValueOrDefault(0));
                canvas.LineTo(rectangle.GetLeft(), rectangle.GetTop() + borderMarginStyle.Margin.GetValueOrDefault(0));
            }
            if (this.right)
            {
                canvas.MoveTo(rectangle.GetRight() + borderMarginStyle.Margin.GetValueOrDefault(0), rectangle.GetTop());
                canvas.LineTo(rectangle.GetRight() + borderMarginStyle.Margin.GetValueOrDefault(0), rectangle.GetBottom());

            }

            if (this.left)
            {
                canvas.MoveTo(rectangle.GetLeft() + borderMarginStyle.Margin.GetValueOrDefault(0), rectangle.GetTop());
                canvas.LineTo(rectangle.GetLeft() + borderMarginStyle.Margin.GetValueOrDefault(0), rectangle.GetBottom());

            }

            canvas.Stroke();
            canvas.RestoreState();

        }
    }
}
