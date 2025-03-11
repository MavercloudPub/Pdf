using iText.Layout;
using iText.Layout.Element;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.ElementHandler
{
    public static class ChunkFactory
    {
        public static ILeafElement Create(object sourceData, ElementItem item, string styleName, ChunkTag tag, string fontName, Document document)
        {
            ILeafElement element = null;
            if (tag.Type == ElementType.Text)
            {
                var textHandler = new TextHandler(sourceData, item, styleName, tag, fontName, document);
                element = textHandler.Handle();
            }
            else if (tag.Type == ElementType.Link)
            {
                var linkHandler = new LinkHandler(sourceData, string.Empty, item, styleName, tag, document);
                element = linkHandler.Handle();
            }
            else if (tag.Type == ElementType.NewLine)
            {
                var textHandler = new TextHandler(Environment.NewLine, item, styleName, tag, document);
                element = textHandler.Handle();
            }
            else if (tag.Type == ElementType.Space)
            {
                var textHandler = new TextHandler(" ", item, styleName, tag, document);
                element = textHandler.Handle();
            }
            return element;
        }
        public static ILeafElement Create(object sourceData, ElementItem item, string styleName, ChunkTag tag, Document document)
        {
            return Create(sourceData, item, styleName, tag, null, document);
        }

        public static Link CreateLink(string text, string destination, ElementItem item, string styleName, ChunkTag tag, Document document)
        {
            var linkHandler = new LinkHandler(text, destination, item, styleName, tag, document);
            return linkHandler.Handle() as Link;
        }

        public static Text CreateAnchor(string text, string destination, ElementItem item, string styleName, ChunkTag tag, Document document)
        {
            var anchorHandler = new AnchorHandler(text, destination, item, styleName, tag, document);
            return anchorHandler.Handle() as Text;
        }

        public static Image CreateImage(string imagePath, string uri, ElementItem item, string styleName, ChunkTag tag, Document document)
        {
            var imageHandler = new ImageHandler(imagePath, tag, styleName, document, uri);
            return imageHandler.Handle();
        }
    }
}
