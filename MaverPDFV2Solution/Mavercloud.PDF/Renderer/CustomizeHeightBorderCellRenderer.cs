using iText.Layout.Element;
using iText.Layout.Renderer;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    /// <summary>
    /// 自定义单元格边框高度
    /// </summary>
    public class CustomizeHeightBorderCellRenderer : CellRenderer
    {
        private CustomizeHeightBorderStyle borderStyle;
        public CustomizeHeightBorderCellRenderer(Cell modelElement, CustomizeHeightBorderStyle borderStyle) : base(modelElement)
        {
            this.borderStyle = borderStyle;
        }

        public override void DrawBorder(DrawContext drawContext)
        {
            //Draw the right border
            if (borderStyle.BorderRight != null)
            {
                var borderColor = ElementGenerator.GetColor(borderStyle.BorderRight.Color);
                var canvas = drawContext.GetCanvas()
                .SaveState()
                .SetLineWidth(borderStyle.BorderRight.Width.Value).SetStrokeColor(borderColor);

                var rectangle = occupiedArea.GetBBox();
                canvas.MoveTo(rectangle.GetRight(), rectangle.GetBottom() + borderStyle.BorderRight.BottomDistance.GetValueOrDefault(0));
                canvas.LineTo(rectangle.GetRight(), rectangle.GetTop() - borderStyle.BorderRight.TopDistance.GetValueOrDefault(0));


                canvas.Stroke();
                canvas.RestoreState();
            }
        }
    }
}
