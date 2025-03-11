using iText.Layout.Element;
using iText.Layout.Renderer;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    public class BorderMarginCellRenderer : CellRenderer
    {
        private BorderMarginStyle borderMarginStyle;

        private bool top;

        private bool bottom;

        private bool right;

        private bool left;
        public BorderMarginCellRenderer(Cell modelElement, BorderMarginStyle borderMarginStyle) : base(modelElement)
        {
            this.borderMarginStyle = borderMarginStyle;
            this.top = borderMarginStyle.Top.GetValueOrDefault();
            this.bottom = borderMarginStyle.Bottom.GetValueOrDefault();
            this.right = borderMarginStyle.Right.GetValueOrDefault();
            this.left = borderMarginStyle.Left.GetValueOrDefault();
        }

        public override IRenderer GetNextRenderer()
        {
            return new BorderMarginCellRenderer((Cell)modelElement, borderMarginStyle);
        }

        public override void DrawBorder(DrawContext drawContext)
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
