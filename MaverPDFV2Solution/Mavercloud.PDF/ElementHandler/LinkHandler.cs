using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Annot;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.Layout;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.ElementHandler
{
    public class LinkHandler : TextHandler
    {
        private string uri;
        public LinkHandler(object sourceData, string uri, ElementItem item, string styleName, Document document) : base(sourceData, item, styleName, document)
        {
            this.uri = uri;
        }

        public LinkHandler(object sourceData, string uri, ElementItem item, string styleName, ChunkTag tag, Document document) : base(sourceData, item, styleName, tag, document)
        {
            this.uri = uri;
        }

        public override Text Handle()
        {
            Link element = null;
            if (CheckStyle())
            {
                var textStr = string.Empty;
                if (sourceData is string || sourceData is ValueType)
                {
                    textStr = sourceData.ToString();
                }
                else
                {
                    textStr = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(item.ValuePath, sourceData).SafeString();

                    if (string.IsNullOrEmpty(uri) && !string.IsNullOrEmpty(item.LinkValuePath))
                    {
                       uri = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(item.LinkValuePath, sourceData).SafeString();
                    }
                }
                if (string.IsNullOrEmpty(uri))
                {
                    uri = textStr;
                }
                
                if (textStr != null)
                {
                    PdfLinkAnnotation linkAnnotation = new PdfLinkAnnotation(new Rectangle(0, 0, 0, 0));
                    linkAnnotation.SetBorder(new PdfAnnotationBorder(0, 0, 0));
                    linkAnnotation.SetAction(PdfAction.CreateURI(uri));

                    linkAnnotation.SetHighlightMode(PdfLinkAnnotation.HIGHLIGHT_NONE);

                    element = new Link(textStr, linkAnnotation);
                    if (!this.SetSplitCharacters(element))
                    {
                        element.SetSplitCharacters(new LinkSplitCharacters());
                    }
                    this.SetFont(element);
                    this.SetFontSize(element);
                    this.SetFontColor(element);
                    this.SetTextRise(element);
                    this.SetSplitCharacters(element);
                    

                    //var font = this.GetFont();
                    //if (font != null)
                    //{
                    //    var textWidth = font.GetWidth(textStr, item.FontSize.Value);
                    //    element.SetStrokeWidth(font.GetWidth(textStr, item.FontSize.Value));
                    //}
                    
                }
            }
            return element;
        }
    }
}
