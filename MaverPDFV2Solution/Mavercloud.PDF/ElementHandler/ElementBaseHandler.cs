using Force.DeepCloner;
using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Action;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Hyphenation;
using iText.Layout.Renderer;
using iText.Layout.Splitting;
using Mavercloud.PDF;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.Layout;
using Mavercloud.PDF.Renderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml;

namespace Mavercloud.PDF.ElementHandler
{
    public abstract class ElementBaseHandler<T> where T : IElement
    {
        protected object sourceData;
        protected ElementItem item;
        protected string styleName;
        protected ChunkTag tag;
        protected Document document;
        protected float? containerWidth;


        protected ElementBaseHandler(object sourceData, ElementItem item, string styleName, Document document)
        {
            this.sourceData = sourceData;
            this.item = item;
            this.styleName = styleName;
            this.tag = null;
            this.document = document;
        }

        protected ElementBaseHandler(object sourceData, ElementItem item, string styleName, Document document, float? containerWidth)
        {
            this.sourceData = sourceData;
            this.item = item;
            this.styleName = styleName;
            this.tag = null;
            this.document = document;
            this.containerWidth = containerWidth;
        }

        protected ElementBaseHandler(object sourceData, ElementItem item, string styleName, ChunkTag tag, Document document)
            : this(sourceData, item, styleName, document)
        {
            this.tag = tag;
        }

        protected ElementBaseHandler(object sourceData, ElementItem item, string styleName, ChunkTag tag, Document document, float? containerWidth)
            : this(sourceData, item, styleName, document, containerWidth)
        {
            this.tag = tag;
        }

        public abstract T Handle();

        protected Color GetFontColor()
        {
            Color color = null;
            if (tag != null && !string.IsNullOrEmpty(tag.FontColor))
            {
                color = GetColor(tag.FontColor);
            }
            else if (!string.IsNullOrEmpty(item.FontColor))
            {
                color = GetColor(item.FontColor);
            }
            return color;

        }

        protected Color GetColor(string rgbColor)
        {
            return ElementGenerator.GetColor(rgbColor);
        }

        protected PdfFont GetFont()
        {
            if (tag == null || string.IsNullOrEmpty(tag.FontName))
            {
                if (!string.IsNullOrEmpty(item.FontName))
                {
                    return PDFFontHelper.GetFont(document, item.FontName);
                    //return iText.Kernel.Font.PdfFontFactory.CreateRegisteredFont(item.FontName, PdfEncodings.IDENTITY_H);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return PDFFontHelper.GetFont(document, tag.FontName);
                //return iText.Kernel.Font.PdfFontFactory.CreateRegisteredFont(tag.FontName, PdfEncodings.IDENTITY_H);
            }
        }

        protected void SetFont(T t, string fontName = null)
        {
            if (t is ElementPropertyContainer<T>)
            {
                PdfFont font = null;
                if (string.IsNullOrEmpty(fontName))
                {
                    font = this.GetFont();
                }
                else
                {
                    font = PDFFontHelper.GetFont(document, fontName);
                }
                if (font != null)
                {
                    if (t is Text)
                    {
                        (t as Text).SetFont(font);
                        
                    }
                    else
                    {
                        var element = t as ElementPropertyContainer<T>;
                        element.SetFont(font);
                        
                    }
                }
            }
        }

        protected void SetFontStyle(T t)
        {
            if (t is ElementPropertyContainer<T>)
            {
                var element = t as ElementPropertyContainer<T>;

                if (tag != null)
                {
                    if (tag.FontBold.GetValueOrDefault())
                    {
                        element.SetBold();
                    }
                    if (tag.FontItalic.GetValueOrDefault())
                    {
                        element.SetItalic();
                    }
                    if (tag.Underline.GetValueOrDefault())
                    {
                        element.SetUnderline();
                    }
                    if (tag.Strike.GetValueOrDefault())
                    {
                        element.SetLineThrough();
                    }
                }
                else if (item != null)
                {
                    if (item.FontBold.GetValueOrDefault())
                    {
                        element.SetBold();
                    }
                    if (item.FontItalic.GetValueOrDefault())
                    {
                        element.SetItalic();
                    }
                    if (item.Underline.GetValueOrDefault())
                    {
                        element.SetUnderline();
                    }
                    if (item.Strike.GetValueOrDefault())
                    {
                        element.SetLineThrough();
                    }
                }
            }
        }

        protected void SetFontSize(T t)
        {
            if (t is ElementPropertyContainer<T>)
            {
                var element = t as ElementPropertyContainer<T>;
                
                if (tag != null && tag.FontSize.HasValue)
                {
                    element.SetFontSize(tag.FontSize.Value);
                }
                else if (item.FontSize.HasValue)
                {
                    element.SetFontSize(item.FontSize.Value);
                }
            }
        }

        protected void SetFontColor(T t)
        {
            if (t is ElementPropertyContainer<T>)
            {
                var element = t as ElementPropertyContainer<T>;
                var color = GetFontColor();
                if (color != null)
                {
                    element.SetFontColor(color);
                }
            }
            
        }

        protected void SetTextRise(T t)
        {
            if (t is Text)
            {
                var element = t as Text;
                if (tag != null && tag.TextRise.HasValue)
                {
                    element.SetTextRise(tag.TextRise.Value);
                }
                else if (item.TextRise.HasValue)
                {
                    element.SetTextRise(item.TextRise.Value);
                }
                
            }
        }

        protected void SetOpacity(T t)
        {
            if (t is ElementPropertyContainer<T>)
            {
                var element = t as ElementPropertyContainer<T>;
                if (tag != null && tag.Opacity.HasValue)
                {
                    element.SetOpacity(tag.Opacity.Value);
                }
                else if (item.Opacity.HasValue)
                {
                    element.SetOpacity(item.Opacity.Value);
                }
            }

        }

        private static readonly object staticLock = new object();
        private static Dictionary<string, HyphenationConfig> hyphenationConfigCache;
        private static bool useHyphenCache = false;
        protected void SetHypenation(T t)
        {
            if (t is ElementPropertyContainer<T>)
            {
                var element = t as ElementPropertyContainer<T>;
                HyphenationConfig hypenConfig = null;
                if (!string.IsNullOrEmpty(item.Hyphenation))
                {
                    var hypenConfigArray = item.Hyphenation.Split(new char[] { ',' });
                    if (hypenConfigArray.Length == 4)
                    {
                        if (useHyphenCache)
                        {
                            var key = string.Format("{0}-{1}-{2}-{3}", hypenConfigArray[0].Trim(), hypenConfigArray[1].Trim(), int.Parse(hypenConfigArray[2].Trim()), int.Parse(hypenConfigArray[3].Trim()));
                            lock (staticLock)
                            {

                                if (hyphenationConfigCache == null)
                                {
                                    hyphenationConfigCache = new Dictionary<string, HyphenationConfig>();
                                }
                                if (hyphenationConfigCache.ContainsKey(key))
                                {
                                    hypenConfig = hyphenationConfigCache[key];
                                }
                                else
                                {
                                    hypenConfig = new HyphenationConfig(hypenConfigArray[0].Trim(), hypenConfigArray[1].Trim(), int.Parse(hypenConfigArray[2].Trim()), int.Parse(hypenConfigArray[3].Trim()));
                                    hyphenationConfigCache[key] = hypenConfig;
                                }

                            }
                        }
                        else
                        {
                            hypenConfig = new HyphenationConfig(hypenConfigArray[0].Trim(), hypenConfigArray[1].Trim(), int.Parse(hypenConfigArray[2].Trim()), int.Parse(hypenConfigArray[3].Trim()));
                        }
                        element.SetHyphenation(hypenConfig);
                    }
                }
                else
                {

                }
            }
        }

        protected bool SetSplitCharacters(T t)
        {
            var setted = false;
            if (t is ElementPropertyContainer<T>)
            {
                var element = t as ElementPropertyContainer<T>;
                if (tag != null)
                {
                    if (tag.BreakAll.GetValueOrDefault())
                    {
                        element.SetSplitCharacters(new iText.Layout.Splitting.BreakAllSplitCharacters());
                        setted = true;
                    }
                    else if (!string.IsNullOrEmpty(tag.SplitCharacters))
                    {
                        Type type = Type.GetType(tag.SplitCharacters);
                        var splitCharacters = Activator.CreateInstance(type) as ISplitCharacters;
                        element.SetSplitCharacters(splitCharacters);
                        setted = true;
                    }
                }
                else
                {
                    if (item.BreakAll.GetValueOrDefault())
                    {
                        element.SetSplitCharacters(new iText.Layout.Splitting.BreakAllSplitCharacters());
                        setted = true;
                    }
                    else if (!string.IsNullOrEmpty(item.SplitCharacters))
                    {
                        Type type = Type.GetType(item.SplitCharacters);
                        var splitCharacters = Activator.CreateInstance(type) as ISplitCharacters;
                        element.SetSplitCharacters(splitCharacters);
                        setted = true;
                    }
                }
            }
            return setted;
        }
        

        protected bool CheckStyle()
        {
            return string.IsNullOrEmpty(styleName) || string.IsNullOrEmpty(item.StyleName) || item.StyleName.ToLower() == "all" || item.StyleName.Contains(styleName);
        }

        protected bool CheckStyle(ElementItem item1)
        {
            return string.IsNullOrEmpty(item1.StyleName) || item1.StyleName.ToLower() == "all" || item1.StyleName.Contains(styleName);
        }

        protected Border GetBorder(BorderStyle elementBorder)
        {
            return ElementGenerator.GetBorder(elementBorder);
        }

        protected void SetBorder(T t, ElementItem elementItem = null)
        {
            if (elementItem == null)
            {
                elementItem = item;
            }
            if (t is ElementPropertyContainer<T>)
            {
                var container = t as ElementPropertyContainer<T>;
                container.SetBorder(Border.NO_BORDER);
                if (elementItem.Border != null)
                {
                    if (elementItem.Border.Type != BorderType.None && elementItem.Border.Width > 0)
                    {
                        container.SetBorder(GetBorder(elementItem.Border));
                        if (elementItem.Border.Radius.HasValue)
                        {
                            container.SetBorderRadius(new iText.Layout.Properties.BorderRadius(elementItem.Border.Radius.Value, elementItem.Border.Radius.Value));
                        }
                    }
                }
                else
                {
                    container.SetBorderLeft(Border.NO_BORDER);
                    container.SetBorderRight(Border.NO_BORDER);
                    container.SetBorderTop(Border.NO_BORDER);
                    container.SetBorderBottom(Border.NO_BORDER);

                    if (elementItem.BorderLeft != null
                        && elementItem.BorderLeft.Width > 0
                        && elementItem.BorderLeft.Type != BorderType.None)
                    {
                        container.SetBorderLeft(GetBorder(elementItem.BorderLeft));
                    }
                    if (elementItem.BorderTop != null
                        && elementItem.BorderTop.Width > 0
                        && elementItem.BorderTop.Type != BorderType.None)
                    {
                        container.SetBorderTop(GetBorder(elementItem.BorderTop));
                    }
                    if (elementItem.BorderRight != null
                        && elementItem.BorderRight.Width > 0
                        && elementItem.BorderRight.Type != BorderType.None)
                    {
                        container.SetBorderRight(GetBorder(elementItem.BorderRight));
                    }
                    if (elementItem.BorderBottom != null
                        && elementItem.BorderBottom.Width > 0
                        && elementItem.BorderBottom.Type != BorderType.None)
                    {
                        container.SetBorderBottom(GetBorder(elementItem.BorderBottom));
                    }
                }
            }
        }

        protected void SetPadding(T t, ElementItem elementItem = null)
        {
            if (elementItem == null)
            {
                elementItem = item;
            }
            if (t is BlockElement<T>)
            {
                var blockElement = t as BlockElement<T>;
                if (elementItem.Padding.HasValue)
                {
                    blockElement.SetPadding(elementItem.Padding.Value);
                }
                if (elementItem.PaddingLeft.HasValue)
                {
                    blockElement.SetPaddingLeft(elementItem.PaddingLeft.Value);
                }
                if (elementItem.PaddingTop.HasValue)
                {
                    blockElement.SetPaddingTop(elementItem.PaddingTop.Value);
                }
                if (elementItem.PaddingRight.HasValue)
                {
                    blockElement.SetPaddingRight(elementItem.PaddingRight.Value);
                }
                if (elementItem.PaddingBottom.HasValue)
                {
                    blockElement.SetPaddingBottom(elementItem.PaddingBottom.Value);
                }
            }
        }

        protected void SetMargin(T t, ElementItem elementItem = null)
        {
            if (elementItem == null)
            {
                elementItem = item;
            }
            if (t is BlockElement<T>)
            {
                var blockElement = t as BlockElement<T>;
                if (elementItem.Margin.HasValue)
                {
                    blockElement.SetMargin(elementItem.Margin.Value);
                }
                if (elementItem.MarginLeft.HasValue)
                {
                    blockElement.SetMarginLeft(elementItem.MarginLeft.Value);
                }
                if (elementItem.MarginTop.HasValue)
                {
                    blockElement.SetMarginTop(elementItem.MarginTop.Value);
                }
                if (elementItem.MarginRight.HasValue)
                {
                    blockElement.SetMarginRight(elementItem.MarginRight.Value);
                }
                if (elementItem.MarginBottom.HasValue)
                {
                    blockElement.SetMarginBottom(elementItem.MarginBottom.Value);
                }
            }
            else if (t is Image)
            {
                var image = t as Image;
                if (elementItem.MarginLeft.HasValue)
                {
                    image.SetMarginLeft(elementItem.MarginLeft.Value);
                }
                if (elementItem.MarginTop.HasValue)
                {
                    image.SetMarginTop(elementItem.MarginTop.Value);
                }
                if (elementItem.MarginRight.HasValue)
                {
                    image.SetMarginRight(elementItem.MarginRight.Value);
                }
                if (elementItem.MarginBottom.HasValue)
                {
                    image.SetMarginBottom(elementItem.MarginBottom.Value);
                }
            }
        }

        protected void SetKeepTogether(T t)
        {
            if (item.KeepTogether.HasValue)
            {
                if (t is BlockElement<T>)
                {
                    var blockElement = t as BlockElement<T>;
                    blockElement.SetKeepTogether(item.KeepTogether.Value);
                }
            }
        }

        protected void SetKeepWithNext(T t)
        {
            if (item.KeepWithNext.HasValue)
            {
                if (t is BlockElement<T>)
                {
                    var blockElement = t as BlockElement<T>;
                    blockElement.SetKeepWithNext(item.KeepWithNext.Value);
                }
            }
        }

        protected void SetInnerAlignment(T t, ElementItem elementItem = null)
        {
            if (elementItem == null)
            {
                elementItem = item;
            }

            if (t is ElementPropertyContainer<T>)
            {
                var blockElement = t as ElementPropertyContainer<T>;
                if (elementItem.TextAlignment.HasValue)
                {
                    blockElement.SetTextAlignment(elementItem.TextAlignment.Value);
                }
            }

            if (t is BlockElement<T>)
            {
                var blockElement = t as BlockElement<T>;
                if (elementItem.VerticalAlignment.HasValue)
                {
                    blockElement.SetVerticalAlignment(elementItem.VerticalAlignment.Value);
                }
            }
        }

        protected void SetOuterAlignment(T t, ElementItem elementItem = null)
        {
            if (elementItem == null)
            {
                elementItem = item;
            }
            if (t is ElementPropertyContainer<T>)
            {
                var blockElement = t as ElementPropertyContainer<T>;
                if (elementItem.HorizontalAlignment.HasValue)
                {
                    blockElement.SetHorizontalAlignment(elementItem.HorizontalAlignment.Value);
                }
            }
        }

        protected void CreateChildren(T t, object sourceObj = null)
        {
            if (t is Cell || t is Div)
            {
                var parentCell = t as Cell;
                var parentDiv = t as Div;

                if (sourceObj == null)
                {
                    sourceObj = sourceData;
                }
                if (sourceObj != null && item.Items != null && item.Items.Count > 0)
                {
                    foreach (var subItem in item.Items)
                    {
                        if (CheckStyle(subItem))
                        {
                            if (subItem.Type == ElementType.Paragraph)
                            {
                                if (subItem.ListValueStyleType.HasValue 
                                    && subItem.ListValueStyleType.Value == ListValueStyleType.MultipleElements)
                                {
                                    if (sourceObj != null && sourceObj.GetType().IsGenericType && sourceObj is IEnumerable)
                                    {
                                        foreach (var sObj in sourceObj as IEnumerable)
                                        {
                                            CreateListChildrens(sObj, parentCell, parentDiv, subItem);
                                        }
                                    }
                                    else
                                    {
                                        CreateListChildrens(sourceObj, parentCell, parentDiv, subItem);
                                    }
                                }
                                else
                                {
                                    var paraHandler = new ParagraphHandler(sourceObj, subItem, styleName, document, containerWidth);
                                    var paragraph = paraHandler.Handle();
                                    if (paragraph != null)
                                    {
                                        if (parentCell != null)
                                        {
                                            parentCell.Add(paragraph);
                                        }
                                        if (parentDiv != null)
                                        {
                                            parentDiv.Add(paragraph);
                                        }
                                    }
                                }
                            }
                            else if (subItem.Type == ElementType.Image)
                            {
                                float? fixedWidth = null;
                                if (sourceObj != null)
                                {
                                    var width = Mavercloud.PDF.Helpers.Object.GetPropertyValue("Width", sourceObj);
                                    if (width != null && !string.IsNullOrEmpty(width.ToString()))
                                    {
                                        fixedWidth = float.Parse(width.ToString());
                                    }
                                }
                                var imageHandler = new ImageHandler(sourceObj, subItem, styleName, document, fixedWidth);
                                var image = imageHandler.Handle();
                                if (image != null)
                                {
                                    if (parentCell != null)
                                    {
                                        parentCell.Add(image);
                                    }
                                    if (parentDiv != null)
                                    {
                                        parentDiv.Add(image);
                                    }
                                }
                            }
                            else if (subItem.Type == ElementType.Table)
                            {
                                var tableHandler = new TableHandler(sourceObj, subItem, styleName, document);
                                var table = tableHandler.Handle();
                                if (table != null)
                                {
                                    if (parentCell != null)
                                    {
                                        parentCell.Add(table);
                                    }
                                    if (parentDiv != null)
                                    {
                                        parentDiv.Add(table);
                                    }
                                }
                            }
                            else if (subItem.Type == ElementType.Div)
                            {
                                var divHandler = new DivHandler(sourceObj, subItem, styleName, document);
                                var div = divHandler.Handle();
                                if (div != null)
                                {
                                    if (parentCell != null)
                                    {
                                        parentCell.Add(div);
                                    }
                                    if (parentDiv != null)
                                    {
                                        parentDiv.Add(div);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
        }

        private void CreateListChildrens(object sourceObj, Cell parentCell, Div parentDiv, ElementItem subItem)
        {
            var valueObject = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(subItem.ValuePath, sourceObj);
            if (valueObject != null)
            {
                if (valueObject.GetType().IsGenericType && valueObject is IEnumerable)
                {
                    var listObject = valueObject as IEnumerable;
                    StringBuilder textBuilder = new StringBuilder();
                    foreach (var valueObj in listObject)
                    {
                        var paraHandler = new ParagraphHandler(valueObj, subItem, styleName, document, containerWidth);
                        var paragraph = paraHandler.Handle();
                        if (paragraph != null)
                        {
                            if (parentCell != null)
                            {
                                parentCell.Add(paragraph);
                            }
                            if (parentDiv != null)
                            {
                                parentDiv.Add(paragraph);
                            }
                        }
                    }
                }
                else
                {
                    var paraHandler = new ParagraphHandler(valueObject, subItem, styleName, document, containerWidth);
                    var paragraph = paraHandler.Handle();
                    if (paragraph != null)
                    {
                        if (parentCell != null)
                        {
                            parentCell.Add(paragraph);
                        }
                        if (parentDiv != null)
                        {
                            parentDiv.Add(paragraph);
                        }
                    }
                }
            }
        }

        protected void SetFixedPosition(Paragraph p, ElementPosition position)
        {
            if (position != null)
            {
                if (p != null)
                {
                    Rectangle pageSize = null;
                    if (document.GetPdfDocument().GetNumberOfPages() > 0)
                    {
                        pageSize = document.GetPdfDocument().GetLastPage().GetPageSize();
                    }
                    else
                    {
                        pageSize = document.GetPdfDocument().GetDefaultPageSize();
                    }
                    var pWidth = p.GetWidthOnRendering(document);
                    if (!position.Left.HasValue && position.HorizontalAlignment.HasValue)
                    {
                        if (position.HorizontalAlignment.Value == iText.Layout.Properties.HorizontalAlignment.LEFT)
                        {
                            position.Left = ElementGenerator.DocumentMargins[3];
                        }
                        else if (position.HorizontalAlignment.Value == iText.Layout.Properties.HorizontalAlignment.RIGHT)
                        {
                            position.Left = pageSize.GetWidth() - pWidth - ElementGenerator.DocumentMargins[1];
                        }
                    }
                    if (position.Top.HasValue)
                    {
                        position.Bottom = pageSize.GetHeight() - position.Top.Value;
                    }
                    p.SetFixedPosition(item.Position.Left.Value, item.Position.Bottom.Value, pWidth);
                }
            }
        }

        protected void SetDestination(T t)
        {
            if (t is ElementPropertyContainer<T>)
            {
                var element = t as ElementPropertyContainer<T>;
                if (!string.IsNullOrEmpty(item.LinkValuePath))
                {
                    var destinationObject = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(item.LinkValuePath, sourceData);
                    if (destinationObject != null)
                    {
                        element.SetDestination(destinationObject.SafeString());
                    }
                }
            }
        }

        protected void SetUriAction(T t, string uri)
        {
            if (t is AbstractElement<T>)
            {
                var element = t as AbstractElement<T>;
                element.SetAction(PdfAction.CreateURI(uri));
            }
        }

        protected void AddTextByXmlNode(T element, XmlNode parentNode, XmlNode textNode, ChunkTag chunkTag, string splitCharaters = null)
        {
            if (element is Paragraph)
            {
                var p = element as Paragraph;

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
                            
                            p.Add(ChunkFactory.CreateAnchor(textNode.InnerText, destination, item, styleName, chunkTag, document));
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
                            var imageHandler = new ImageHandler(imageHrefAttr.Value, new ElementItem() { Padding = 0, Margin= 0}, styleName, document, null, height);
                            p.Add(imageHandler.Handle());
                        }
                    }
                    else
                    {
                        if (chunkTag == null)
                        {
                            chunkTag = item.Tags.FirstOrDefault(t => t.Name == textNode.Name);
                        }
                        else
                        {
                            if (chunkTag.Tags != null && chunkTag.Tags.Any(t => t.Name == textNode.Name))
                            {
                                chunkTag = chunkTag.Tags.First(t => t.Name == textNode.Name);
                            }
                        }
                        if (chunkTag == null)
                        {
                            chunkTag = SpecialCharHelper.GetFontTag(textNode.Name);
                        }
                        if (chunkTag != null && chunkTag.Name != textNode.Name && textNode.Name.EndsWith("Font"))
                        {
                            var fontChunkTag = SpecialCharHelper.GetFontTag(textNode.Name);

                            var copiedChunkTag = chunkTag.DeepClone();
                            copiedChunkTag.FontName = fontChunkTag.FontName;
                            foreach (XmlNode subNode in textNode.ChildNodes)
                            {
                                AddTextByXmlNode(element, textNode, subNode, copiedChunkTag, splitCharaters);
                            }
                        }
                        else
                        {
                            foreach (XmlNode subNode in textNode.ChildNodes)
                            {
                                AddTextByXmlNode(element, textNode, subNode, chunkTag, splitCharaters);
                            }
                        }
                    }
                }
            }
        }

        protected void SetStyle(T t)
        {
            SetInnerAlignment(t);
            SetOuterAlignment(t);
            SetBorder(t);
            SetPadding(t);
            SetMargin(t);
        }

        protected void SetStyle(T t, ElementItem item)
        { 
            
        }
    }
}
