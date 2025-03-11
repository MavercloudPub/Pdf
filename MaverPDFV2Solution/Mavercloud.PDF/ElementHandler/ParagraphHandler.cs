using iText.IO.Image;
using iText.IO.Util;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Action;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Splitting;
using Mavercloud.PDF;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.Renderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace Mavercloud.PDF.ElementHandler
{
    public class ParagraphHandler : ElementBaseHandler<Paragraph>
    {
        private bool ignoreSourceNull;
        private bool isInlineParagraph;

        private Paragraph p = null;

        public ParagraphHandler(Paragraph p, ElementItem item, string styleName, Document document) : base(null, item, styleName, document)
        {
            this.p = p;
        }

        public ParagraphHandler(object sourceData, ElementItem item, string styleName, Document document, float? containerWidth)
            : base(sourceData, item, styleName, document, containerWidth)
        {
            this.ignoreSourceNull = false;
        }

        public ParagraphHandler(object sourceData, ElementItem item, string styleName, Document document) 
            : base(sourceData, item, styleName, document)
        {
            this.ignoreSourceNull = false;
        }

        public ParagraphHandler(object sourceData, ElementItem item, string styleName, Document document, bool ignoreSourceNull) : base(sourceData, item, styleName, document)
        {
            this.ignoreSourceNull = ignoreSourceNull;
        }

        public ParagraphHandler(object sourceData, ElementItem item, string styleName, Document document, bool ignoreSourceNull, bool isInlineParagraph) : base(sourceData, item, styleName, document)
        {
            this.ignoreSourceNull = ignoreSourceNull;
            this.isInlineParagraph = isInlineParagraph;
        }

        public ParagraphHandler(object sourceData, ElementItem item, string styleName, Document document, bool ignoreSourceNull, bool isInlineParagraph, ChunkTag tag) : base(sourceData, item, styleName, document)
        {
            this.ignoreSourceNull = ignoreSourceNull;
            this.isInlineParagraph = isInlineParagraph;
            this.tag = tag;
        }

        public override Paragraph Handle()
        {
            
            if (CheckStyle())
            {
                if (sourceData != null)
                {
                    if (item.Items != null && item.Items.Count > 0)
                    {
                        foreach (var subItem in item.Items)
                        {
                            if (CheckStyle(subItem))
                            {
                                if (string.IsNullOrEmpty(subItem.SplitCharacters))
                                {
                                    subItem.SplitCharacters = item.SplitCharacters;
                                }
                                if (subItem.Type == ElementType.Text)
                                {
                                    
                                    var textHandler = new TextHandler(sourceData, subItem, styleName, document);
                                    var text = textHandler.Handle();
                                    if (text != null)
                                    {
                                        if (p == null)
                                        {
                                            p = new Paragraph();
                                            SetHypenation(p);
                                            SetSplitCharacters(p);
                                        }
                                        p.Add(text);
                                    }
                                }
                                else if (subItem.Type == ElementType.NewLine)
                                {
                                    p.Add(Environment.NewLine);
                                }
                                else if (subItem.Type == ElementType.Image)
                                {
                                    float? fixedWidth = null;
                                    if (sourceData != null)
                                    {
                                        var width = Mavercloud.PDF.Helpers.Object.GetPropertyValue("Width", sourceData);
                                        if (width != null && !string.IsNullOrEmpty(width.ToString()))
                                        {
                                            fixedWidth = float.Parse(width.ToString());
                                        }
                                    }
                                    var imageHandler = new ImageHandler(sourceData, subItem, styleName, document, fixedWidth);
                                    var image = imageHandler.Handle();
                                    if (image != null)
                                    {
                                        if (p == null)
                                        {
                                            p = new Paragraph();
                                            SetHypenation(p);
                                            SetSplitCharacters(p);
                                        }
                                        p.Add(image);
                                    }
                                }
                                else if (subItem.Type == ElementType.Link)
                                {
                                    var linkHandler = new LinkHandler(sourceData, string.Empty, subItem, styleName, document);
                                    var link = linkHandler.Handle();
                                    if (link != null)
                                    {
                                        if (p == null)
                                        {
                                            p = new Paragraph();
                                            SetHypenation(p);
                                            SetSplitCharacters(p);
                                        }
                                        p.Add(link);
                                    }
                                }
                                else if (subItem.Type == ElementType.Anchor)
                                {
                                    var anchorHandler = new AnchorHandler(sourceData, string.Empty, subItem, styleName, document);
                                    var link = anchorHandler.Handle();
                                    if (link != null)
                                    {
                                        if (p == null)
                                        {
                                            p = new Paragraph();
                                            SetHypenation(p);
                                            SetSplitCharacters(p);
                                        }
                                        p.Add(link);
                                    }
                                }
                            }
                        }
                        SetFixedPosition(p, item.Position);
                    }
                    else
                    {
                        string textStr = null;
                        if (!string.IsNullOrEmpty(item.ConstantValue))
                        {
                            textStr = item.ConstantValue;
                        }
                        else if (sourceData is string || sourceData is ValueType)
                        {
                            textStr = sourceData.ToString();
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(item.ValuePath))
                            {
                                var valueObject = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(item.ValuePath, sourceData);
                                if (valueObject != null)
                                {
                                    if (valueObject.GetType().IsGenericType && valueObject is IEnumerable)
                                    {
                                        var listObject = valueObject as IList;
                                        if (listObject != null && listObject.Count > 0)
                                        {
                                            StringBuilder textBuilder = new StringBuilder();
                                            int oindex = 0;
                                            foreach (var textObj in listObject)
                                            {
                                                textBuilder.Append(textObj.SafeString());
                                                if (oindex != listObject.Count - 1)
                                                {
                                                    if (item.ListValueStyleType.HasValue && item.ListValueStyleType.Value == ListValueStyleType.SingleElementWithLinesContent)
                                                    {
                                                        textBuilder.Append(Environment.NewLine);
                                                    }
                                                }
                                                oindex++;
                                            }
                                            textStr = textBuilder.ToString();
                                        }
                                    }
                                    else
                                    {

                                        textStr = valueObject.SafeString();
                                    }
                                }
                            }
                        }
                        if (textStr != null || ignoreSourceNull)
                        {
                            p = new Paragraph();
                            SetHypenation(p);
                            SetSplitCharacters(p);
                            bool centeredText = false;
                            ElementPosition position = item.Position;

                            if (position != null)
                            {
                                if (position.HorizontalAlignment.HasValue && position.HorizontalAlignment.Value == HorizontalAlignment.CENTER)
                                {
                                    position.Left = ElementGenerator.DocumentMargins[3];
                                    centeredText = true;
                                    Rectangle pageSize = null;
                                    if (document.GetPdfDocument().GetNumberOfPages() > 0)
                                    {
                                        pageSize = document.GetPdfDocument().GetLastPage().GetPageSize();
                                    }
                                    else
                                    {
                                        pageSize = document.GetPdfDocument().GetDefaultPageSize();
                                    }
                                    position.Width = pageSize.GetWidth() - ElementGenerator.DocumentMargins[3] - ElementGenerator.DocumentMargins[1];
                                    centeredText = true;

                                    List<TabStop> tabStops = new List<TabStop>();

                                    // Create a TabStop at the middle of the page
                                    tabStops.Add(new TabStop(position.Width.Value / 2, TabAlignment.CENTER));

                                    // Create a TabStop at the end of the page
                                    tabStops.Add(new TabStop(position.Width.Value, TabAlignment.LEFT));

                                    p = p.AddTabStops(tabStops);
                                }
                            }

                            if (item.HorizontalAlignment.HasValue && item.HorizontalAlignment == HorizontalAlignment.CENTER)
                            {
                                centeredText = true;
                                Rectangle pageSize = null;
                                if (document.GetPdfDocument().GetNumberOfPages() > 0)
                                {
                                    pageSize = document.GetPdfDocument().GetLastPage().GetPageSize();
                                }
                                else
                                {
                                    pageSize = document.GetPdfDocument().GetDefaultPageSize();
                                }
                                var pageWidth = pageSize.GetWidth() - ElementGenerator.DocumentMargins[3] - ElementGenerator.DocumentMargins[1];

                                List<TabStop> tabStops = new List<TabStop>();

                                // Create a TabStop at the middle of the page
                                tabStops.Add(new TabStop(pageWidth / 2, TabAlignment.CENTER));

                                // Create a TabStop at the end of the page
                                tabStops.Add(new TabStop(pageWidth, TabAlignment.LEFT));

                                p = p.AddTabStops(tabStops);
                            }

                            if (centeredText)
                            {
                                p.Add(new Tab());
                            }
                            if (textStr != null)
                            {
#if DEBUG
                                if (textStr.Contains("it is concluded that modern analytical methods are fully"))
                                {

                                }
#endif
                                if (!string.IsNullOrEmpty(textStr))
                                {
                                    textStr = textStr.Replace("> <", "><Space /><");
                                    if (item.SpaceFontSize.HasValue)
                                    {
                                        textStr = ReplaceSpaceForSetFontSize(textStr);
                                    }
                                    var textXml = "<Paragraph>" + TextHelper.InitXml(textStr) + "</Paragraph>";
                                    XmlDocument contentDoc = new XmlDocument();
                                    contentDoc.LoadXml(textXml);

                                    var rootNode = contentDoc.ChildNodes[0];
                                    foreach (XmlNode textNode in rootNode.ChildNodes)
                                    {
#if DEBUG
                                        if (textNode.Name == "AsteriskSup")
                                        {

                                        }
#endif
                                        var nodeTextHanlder = new XmlNodeTextHandler(document, p, rootNode, textNode, item, tag, styleName, item.SplitCharacters);
                                        nodeTextHanlder.Handle();
                                    }


                                }
                            }
                            if (centeredText)
                            {
                                p.Add(new Tab());
                            }
                            if (position != null && !isInlineParagraph)
                            {
                                SetFixedPosition(p, position);
                            }
                            
                        }
                    }
                }
                else
                {
                    if (ignoreSourceNull)
                    {
                        p = new Paragraph();
                        SetHypenation(p);
                        SetSplitCharacters(p);
                    }
                }

                
                if (p != null)
                {
                    
                    if (item.MultipliedLeading.HasValue)
                    {
                        if (isInlineParagraph && item.InlineFixedLeading.GetValueOrDefault())
                        {
                            p.SetFixedLeading(item.MultipliedLeading.Value);
                        }
                        else
                        {
                            p.SetMultipliedLeading(item.MultipliedLeading.Value);
                        }
                    }

                    if (item.FixedLeading.HasValue)
                    {
                        p.SetFixedLeading(item.FixedLeading.Value);
                    }

                    if (item.FirstLineIndent.HasValue)
                    {
                        if (!isInlineParagraph)
                        {
                            p.SetFirstLineIndent(item.FirstLineIndent.Value);
                        }
                        else
                        {
                            if (item.InlineItem != null && item.InlineItem.FirstLineIndent.HasValue)
                            {
                                p.SetFirstLineIndent(item.InlineItem.FirstLineIndent.Value);
                            }
                            else
                            {
                                p.SetFirstLineIndent(0f);
                            }
                        }
                    }
                    

                    
                    this.SetFont(p);
                    this.SetFontSize(p);
                    this.SetFontColor(p);
                    this.SetOpacity(p);
                    if (!string.IsNullOrEmpty(item.BackColor))
                    {
                        p.SetBackgroundColor(GetColor(item.BackColor));
                    }
                    SetKeepTogether(p);
                    SetKeepWithNext(p);
                    if (item.WordSpacing.HasValue)
                    {
                        p.SetWordSpacing(item.WordSpacing.Value);
                        //p.SetSpacingRatio(0.2f);
                    }

                    this.SetStyle();

                    if (item.TextAlignmentCenterForShortContent.GetValueOrDefault())
                    {
                        var paragraphWidth = p.GetWidthOnRendering(document);

                        var thisContainerWidth = 0f;
                        if (containerWidth.HasValue)
                        {
                            thisContainerWidth = containerWidth.Value;
                        }
                        else
                        {
                            thisContainerWidth = document.GetPdfDocument().GetDefaultPageSize().GetWidth() - ElementGenerator.DocumentMargins[1] - ElementGenerator.DocumentMargins[3];
                        }
                        if (paragraphWidth < thisContainerWidth)
                        {
                            p.SetTextAlignment(TextAlignment.CENTER);
                        }

                    }

                    if (isInlineParagraph && item.InlineItem != null)
                    {
                        SetBorder(p, item.InlineItem);
                        SetPadding(p, item.InlineItem);
                        SetMargin(p, item.InlineItem);
                        if (item.InlineItem.MultipliedLeading.HasValue)
                        {
                            p.SetMultipliedLeading(item.MultipliedLeading.Value);
                        }
                        else if (item.InlineItem.FixedLeading.HasValue)
                        {
                            p.SetFixedLeading(item.FixedLeading.Value);
                        }
                        if (item.InlineItem.FirstLineIndent.HasValue)
                        {
                            p.SetFirstLineIndent(item.InlineItem.FirstLineIndent.Value);
                        }
                        
                    }

                    if (!isInlineParagraph)
                    {
                        
                        if (item.Width.HasValue)
                        {
                            p.SetWidth(item.Width.Value);
                        }
                        SetDestination(p);
                        
                        if (item.BackAxialShading != null)
                        {
                            // Use a custom renderer in which drawing is overridden
                            p.SetNextRenderer(new BackgroundShadingParagraphRenderer(p, item.BackAxialShading));
                        }
                        if (item.BorderAxialShading != null)
                        {
                            p.SetNextRenderer(new BorderShadingParagraphRenderer(p, item.BorderAxialShading));
                        }
                        if (item.RoundRectangle != null)
                        {
                            p.SetNextRenderer(new RoundParagraphRenderer(p, item.RoundRectangle));
                        }
                        if (item.RotationRatio.HasValue)
                        {
                            p.SetRotationAngle(Math.PI / item.RotationRatio.Value);
                        }
                    }

                }
            }
            return p;
        }

        public void SetStyle()
        {
            this.SetStyle(p);
        }

        private string ReplaceSpaceForSetFontSize(string textStr)
        {
            var textXml = "<Paragraph>" + TextHelper.InitXml(textStr) + "</Paragraph>";
            XmlDocument contentDoc = new XmlDocument();
            contentDoc.LoadXml(textXml);

            var rootNode = contentDoc.ChildNodes[0];
            foreach (XmlNode textNode in rootNode.ChildNodes)
            {
                if (!string.IsNullOrEmpty(textNode.InnerText))
                {
                    textStr = textStr.Replace(textNode.InnerText, textNode.InnerText.Replace(" ", "<Space />"));
                }
            }
            return textStr;
        }
    }
}
