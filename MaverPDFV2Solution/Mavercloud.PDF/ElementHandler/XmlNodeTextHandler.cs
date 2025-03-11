using Force.DeepCloner;
using iText.Kernel.Colors;
using iText.Layout;
using iText.Layout.Element;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Mavercloud.PDF.ElementHandler
{
    public class XmlNodeTextHandler
    {
        private Paragraph p;
        private XmlNode parentNode;
        private XmlNode textNode;
        private ChunkTag chunkTag;
        private string splitCharaters;
        private ElementItem item;
        private string styleName;
        private Document document;
        public XmlNodeTextHandler(Document document, Paragraph p, XmlNode parentNode, XmlNode textNode, ElementItem item, ChunkTag chunkTag, string styleName = null, string splitCharaters = null)
        { 
            this.p = p;
            this.parentNode = parentNode;
            this.textNode = textNode;
            this.chunkTag = chunkTag;
            this.splitCharaters = splitCharaters;
            this.item = item;
            this.styleName = styleName;
            this.document = document;
        }

        public void Handle()
        {
            if (textNode.NodeType == XmlNodeType.Text)
            {
                if (chunkTag == null)
                {
                    chunkTag = SpecialCharHelper.GetFontTag(textNode.Name);
                    if (string.IsNullOrEmpty(item.SplitCharacters))
                    {
                        item.SplitCharacters = splitCharaters;
                    }
                    var textHandler = new TextHandler(textNode.InnerText, item, styleName, chunkTag, document);
                    var text = textHandler.Handle();
                    if (parentNode.Name.ToLower() == "underline")
                    {
                        text.SetUnderline();
                    }
                    p.Add(text);
                }
                else
                {
                    if (string.IsNullOrEmpty(chunkTag.SplitCharacters) && chunkTag.Type != ElementType.Link)
                    {
                        chunkTag.SplitCharacters = splitCharaters;
                    }
                    if (chunkTag.Type == ElementType.Link)
                    {
                        var uri = string.Empty;
                        if (parentNode.Attributes != null)
                        {
                            var uriAttr = parentNode.Attributes["href"];
                            if (uriAttr != null)
                            {
                                uri = uriAttr.InnerText;
                            }
                        }
                        p.Add(ChunkFactory.CreateLink(textNode.InnerText, uri, item, styleName, chunkTag, document));
                    }
                    else if (chunkTag.Type == ElementType.Anchor)
                    {
                        var destination = string.Empty;
                        if (parentNode.Attributes != null)
                        {
                            var destAttr = parentNode.Attributes["Destination"];
                            if (destAttr != null)
                            {
                                destination = destAttr.InnerText;
                            }
                        }

                        var anchor = ChunkFactory.CreateAnchor(textNode.InnerText, destination, item, styleName, chunkTag, document);
                        p.Add(anchor);
                    }
                    else if (chunkTag.Type == ElementType.Image)
                    {
                        var uri = string.Empty;
                        if (parentNode.Attributes != null)
                        {
                            var destAttr = parentNode.Attributes["Href"];
                            if (destAttr != null)
                            {
                                uri = destAttr.InnerText;
                            }
                        }
                        var image = ChunkFactory.CreateImage(textNode.InnerText, uri, item, styleName, chunkTag, document);
                        if (image != null)
                        {
                            p.Add(image);
                        }
                    }
                    else
                    {
                        if (parentNode.Name.ToLower() == "underline" && !chunkTag.Underline.HasValue)
                        {
                            chunkTag.Underline = true;
                        }
                        p.Add(ChunkFactory.Create(textNode.InnerText, item, styleName, chunkTag, document));
                        chunkTag.Underline = null;
                    }
                }
            }
            else if (textNode.NodeType == XmlNodeType.Element)
            {
                if (textNode.Name.ToLower() == "newline")
                {
                    p.Add(Environment.NewLine);
                }
                else if (textNode.Name.ToLower() == "break")
                {
                    p.Add(Environment.NewLine);
                }
                else if (textNode.Name.ToLower() == "space")
                {
                    var text = new Text(" ");
                    if (item.SpaceFontSize.HasValue)
                    {
                        text.SetFontSize(item.SpaceFontSize.Value);
                    }
                    p.Add(text);
                }
                else if (textNode.Name.ToLower() == "hypen")
                {
                    p.Add(new Text("-"));
                }
                else if (textNode.Name.ToLower() == "inlineparagraph")
                {
                    var inlineParagraphHandler = new ParagraphHandler(textNode.InnerXml, item, styleName, document, false, true, chunkTag);
                    var inlineParagraph = inlineParagraphHandler.Handle();

                    p.Add(inlineParagraph);
                }
                else if (textNode.Name.ToLower() == "inline-graphic")
                {
                    var imageHrefAttr = textNode.Attributes["xlink:href"];
                    var imageHeight = textNode.Attributes["Height"];
                    if (imageHrefAttr != null && !string.IsNullOrEmpty(imageHrefAttr.Value))
                    {
                        float? height = null;
                        if (imageHeight != null && !string.IsNullOrEmpty(imageHeight.Value))
                        {
                            height = float.Parse(imageHeight.Value);
                        }
                        var imageHandler = new ImageHandler(imageHrefAttr.Value, new ElementItem() { Padding = 0, Margin = 0 }, styleName, document, null, height, null);
                        var image = imageHandler.Handle();
                        if (image != null)
                        {
                            p.Add(image);
                        }
                    }
                }
                else if (textNode.Name.ToLower() == "styledcontent")
                {
                    var copiedItem = item.DeepClone();
                    var copiedChunkTag = chunkTag;
                    if (chunkTag != null)
                    {
                        copiedChunkTag = chunkTag.DeepClone();
                    }
                    var style = textNode.Attributes["style"];
                    if (style != null && !string.IsNullOrEmpty(style.Value))
                    {
                        var styleArray = style.Value.Split(new char[] { ';' });
                        foreach (var styleItem in styleArray)
                        {
                            if (styleItem.Trim().StartsWith("color:"))
                            {
                                var color = styleItem.Trim().Substring(styleItem.Trim().IndexOf(":") + 1).Trim();
                                if (copiedChunkTag != null)
                                {
                                    copiedChunkTag.FontColor = color;
                                    copiedChunkTag.StyledContent = true;
                                }
                                else
                                {
                                    copiedItem.FontColor = color;
                                    copiedItem.StyledContent = true;
                                }
                            }
                        }
                    }
                    foreach (XmlNode subNode in textNode.ChildNodes)
                    {
                        var nodeTextHanlder = new XmlNodeTextHandler(document, p, textNode, subNode, copiedItem, copiedChunkTag, styleName, splitCharaters);
                        nodeTextHanlder.Handle();
                    }
                }
                else
                {
                    ChunkTag subChunkTag = null;
                    if (chunkTag == null)
                    {
                        subChunkTag = item.Tags.FirstOrDefault(t => t.Name == textNode.Name);
                        if (subChunkTag != null
                            && item.StyledContent.GetValueOrDefault()
                            && !string.IsNullOrEmpty(item.FontColor))
                        {
                            subChunkTag.FontColor = item.FontColor;
                        }
                    }
                    else
                    {
                        if (chunkTag.Tags != null && chunkTag.Tags.Any(t => t.Name == textNode.Name))
                        {
                            subChunkTag = chunkTag.Tags.First(t => t.Name == textNode.Name);

                            if (subChunkTag != null
                            && chunkTag.StyledContent.GetValueOrDefault()
                            && !string.IsNullOrEmpty(chunkTag.FontColor))
                            {
                                subChunkTag.FontColor = chunkTag.FontColor;
                            }
                        }
                    }
                    if (subChunkTag == null)
                    {
                        var fontChunkTag = SpecialCharHelper.GetFontTag(textNode.Name);
                        if (fontChunkTag != null)
                        {
                            subChunkTag = fontChunkTag.DeepClone();
                        }
                    }
                    if (subChunkTag != null && subChunkTag.Name != textNode.Name && textNode.Name.EndsWith("Font"))
                    {
                        var fontChunkTag = SpecialCharHelper.GetFontTag(textNode.Name);

                        var copiedChunkTag = subChunkTag.DeepClone();
                        copiedChunkTag.FontName = fontChunkTag.FontName;
                        foreach (XmlNode subNode in textNode.ChildNodes)
                        {
                            var nodeTextHanlder = new XmlNodeTextHandler(document, p, textNode, subNode, item, copiedChunkTag, styleName, splitCharaters);
                            nodeTextHanlder.Handle();
                        }
                    }
                    else
                    {
                        if (subChunkTag == null)
                        {
                            subChunkTag = chunkTag;
                        }
                        else
                        {
                            if (chunkTag == null)
                            {
                                if (string.IsNullOrEmpty(subChunkTag.FontName))
                                {
                                    subChunkTag.FontName = item.FontName;
                                }
                                if (string.IsNullOrEmpty(subChunkTag.FontColor))
                                {
                                    subChunkTag.FontColor = item.FontColor;
                                }
                                //if (!subChunkTag.FontSize.HasValue)
                                //{
                                //    subChunkTag.FontSize = item.FontSize;
                                //}
                                if (!subChunkTag.TextRise.HasValue)
                                {
                                    subChunkTag.TextRise = item.TextRise;
                                }
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(subChunkTag.FontName))
                                {
                                    subChunkTag.FontName = chunkTag.FontName;
                                }
                                if (string.IsNullOrEmpty(subChunkTag.FontColor))
                                {
                                    subChunkTag.FontColor = chunkTag.FontColor;
                                }
                                if (!string.IsNullOrEmpty(subChunkTag.FontName) && !subChunkTag.FontName.Contains("Bold"))
                                {
                                    if (!string.IsNullOrEmpty(chunkTag.FontName) && chunkTag.FontName.Contains("Bold"))
                                    {
                                        subChunkTag.FontBold = true;
                                    }
                                }
                                if (!subChunkTag.FontSize.HasValue)
                                {
                                    subChunkTag.FontSize = chunkTag.FontSize;
                                }
                                if (!subChunkTag.TextRise.HasValue)
                                {
                                    subChunkTag.TextRise = chunkTag.TextRise;
                                }
                            }
                        }
                        foreach (XmlNode subNode in textNode.ChildNodes)
                        {
                            var nodeTextHanlder = new XmlNodeTextHandler(document, p, textNode, subNode, item, subChunkTag, styleName, splitCharaters);
                            nodeTextHanlder.Handle();
                        }
                    }
                }
            }
        }
    }
}
