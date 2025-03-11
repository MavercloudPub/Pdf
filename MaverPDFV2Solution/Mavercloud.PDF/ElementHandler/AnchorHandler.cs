using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Navigation;
using iText.Layout;
using iText.Layout.Element;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.ElementHandler
{
    public class AnchorHandler : TextHandler
    {
        private string destination;
        public AnchorHandler(object sourceData, string destination, ElementItem item, string styleName, Document document) : base(sourceData, item, styleName, document)
        {
            this.destination = destination;
        }

        public AnchorHandler(object sourceData, string destination, ElementItem item, string styleName, ChunkTag tag, Document document) : base(sourceData, item, styleName, tag, document)
        {
            this.destination = destination;
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

                    if (string.IsNullOrEmpty(destination) && !string.IsNullOrEmpty(item.LinkValuePath))
                    {
                        destination = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(item.LinkValuePath, sourceData).SafeString();
                    }
                }
                if (string.IsNullOrEmpty(destination))
                {
                    destination = textStr;
                }

                if (textStr != null)
                {
                    if (destination.Contains("B59"))
                    { 
                        
                    }
                    //iText.Kernel.Pdf.Navigation.PdfDestination.MakeDestination(new PdfString(destination));

                    iText.Kernel.Pdf.Navigation.PdfStringDestination pdfdestination = new PdfStringDestination(new PdfString(destination));
                    PdfLinkAnnotation linkAnnotation = new PdfLinkAnnotation(new Rectangle(0, 0, 0, 0));
                    linkAnnotation.SetBorder(new PdfAnnotationBorder(0, 0, 0));
                    linkAnnotation.SetAction(PdfAction.CreateGoTo(PdfExplicitDestination.MakeDestination(new PdfString(destination))));
                    //linkAnnotation.SetAction(PdfAction.CreateGoTo(pdfdestination));
                    linkAnnotation.SetHighlightMode(PdfLinkAnnotation.HIGHLIGHT_NONE);
                    
                    element = new Link(textStr, linkAnnotation);
                    this.SetSplitCharacters(element);
                    this.SetFont(element);
                    this.SetFontSize(element);
                    this.SetFontColor(element);
                    this.SetTextRise(element);
                }
            }
            return element;
        }
    }
}
