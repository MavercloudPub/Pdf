using iText.Layout;
using iText.Layout.Element;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.Renderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.ElementHandler
{
    public class DivHandler : ElementBaseHandler<Div>
    {
        public DivHandler(object sourceData, ElementItem item, string styleName, Document document)
            : base(sourceData, item, styleName, document)
        {
        }
        public override Div Handle()
        {
            Div div = null;
            if (CheckStyle())
            {
                div = new Div();
                if (!string.IsNullOrEmpty(item.BackColor))
                {
                    div.SetBackgroundColor(GetColor(item.BackColor));
                }
                if (item.Width.HasValue)
                {
                    div.SetWidth(item.Width.Value);
                }
                if (item.Height.HasValue)
                {
                    div.SetHeight(item.Height.Value);
                }
                CreateChildren(div);

                SetInnerAlignment(div);
                SetOuterAlignment(div);
                SetBorder(div);
                SetPadding(div);
                SetMargin(div);

                if (item.BorderAxialShading != null)
                {
                    div.SetNextRenderer(new BorderShadingDivRenderer(div, item.BorderAxialShading));
                }
            }
            return div;
        }
    }
}
