using iText.Kernel.Colors;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Hyphenation;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Mavercloud.PDF.ElementHandler
{
    public class TextHandler : ElementBaseHandler<Text>
    {
        protected string fontName;

        public TextHandler(object sourceData, ElementItem item, string styleName, string fontName, Document document) : base(sourceData, item, styleName, document)
        {
            this.fontName = fontName;
        }
        public TextHandler(object sourceData, ElementItem item, string styleName, Document document) : base(sourceData, item, styleName, document)
        {

        }

        public TextHandler(object sourceData, ElementItem item, string styleName, ChunkTag tag, Document document) : base(sourceData, item, styleName, tag, document)
        {

        }

        public TextHandler(object sourceData, ElementItem item, string styleName, ChunkTag tag, string fontName, Document document) : base(sourceData, item, styleName, tag, document)
        {
            this.fontName = fontName;
        }

        public override Text Handle()
        {
            Text element = null;
            if (CheckStyle())
            {
                var textStr = string.Empty;
                if (!string.IsNullOrEmpty(item.ConstantValue))
                {
                    textStr = item.ConstantValue;
                }
                else
                {
                    if (sourceData is string || sourceData is ValueType)
                    {
                        textStr = sourceData.ToString();
                    }
                    else
                    {
                        textStr = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(item.ValuePath, sourceData).SafeString();
                    }
                }
                if (textStr != null)
                {
                    //textStr = textStr.Replace(" ", "&#160;");
                    if (tag != null)
                    {
                        if (tag.TextUpper.GetValueOrDefault())
                        {
                            textStr = textStr.ToUpper();
                        }
                    }
                    if (item != null)
                    {
                        if (item.TextUpper.GetValueOrDefault())
                        {
                            textStr = textStr.ToUpper();
                        }
                    }
                    element = new Text(textStr);//.SetHyphenation(new HyphenationConfig("en", "EN", 5, 5));
                    this.SetSplitCharacters(element);
                    this.SetHypenation(element);
                    this.SetFont(element, fontName);
                    this.SetFontSize(element);
                    this.SetFontColor(element);
                    this.SetOpacity(element);
                    this.SetTextRise(element);
                    SetFontStyle(element);

                    if (item.CharacterSpacing.HasValue)
                    {
                        element.SetCharacterSpacing(item.CharacterSpacing.Value);
                    }

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
