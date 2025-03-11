using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Elements
{
    [Serializable]
    public class ElementItem
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string ValuePath { get; set; }

        [XmlAttribute]
        public string ConstantValue { get; set; }

        [XmlAttribute]
        public string LinkValuePath { get; set; }

        [XmlAttribute]
        public ElementType Type { get; set; }

        [XmlAttribute]
        public string BasedValuePath { get; set; }

        [XmlElement("Location")]
        public string Location { get; set; }

        [XmlElement("StyleName")]
        public string StyleName { get; set; }

        [XmlElement("FontSize")]
        public float? FontSize { get; set; }

        [XmlElement("FontColor")]
        public string FontColor { get; set; }

        public bool? StyledContent { get; set; }

        [XmlElement("FontName")]
        public string FontName { get; set; }

        [XmlElement("FontBold")]
        public bool? FontBold { get; set; }

        [XmlElement("FontItalic")]
        public bool? FontItalic { get; set; }

        [XmlElement("Underline")]
        public bool? Underline { get; set; }

        [XmlElement("Strike")]
        public bool? Strike { get; set; }

        [XmlElement("TextRise")]
        public float? TextRise { get; set; }

        [XmlElement("TextUpper")]
        public bool? TextUpper { get; set; }

        [XmlElement("BackColor")]
        public string BackColor { get; set; }

        [XmlElement("Opacity")]
        public float? Opacity { get; set; }

        [XmlElement("RoundRectangle")]
        public RoundRectangleStyle RoundRectangle { get; set; }

        [XmlElement("BackAxialShading")]
        public AxialShadingStyle BackAxialShading { get; set; }

        [XmlElement("BorderAxialShading")]
        public BorderAxialShadingStyle BorderAxialShading { get; set; }

        [XmlElement("BorderMargin")]
        public BorderMarginStyle BorderMargin { get; set; }

        [XmlElement("Height")]
        public float? Height { get; set; }

        [XmlElement("MinHeight")]
        public float? MinHeight { get; set; }

        [XmlElement("MinWidth")]
        public float? MinWidth { get; set; }

        [XmlElement("Width")]
        public float? Width { get; set; }

        [XmlElement("WidthStyle")]
        public WidthStyle? WidthStyle { get; set; }

        [XmlElement("HeightStyle")]
        public HeightStyle? HeightStyle { get; set; }

        [XmlElement("MultipliedLeading")]
        public float? MultipliedLeading { get; set; }

        [XmlElement("FixedLeading")]
        public float? FixedLeading { get; set; }

        [XmlElement("InlineFixedLeading")]
        public bool? InlineFixedLeading { get; set; }

        [XmlElement("TextAlignment")]
        public TextAlignment? TextAlignment { get; set; }

        [XmlElement("TextAlignmentCenterForShortContent")]
        public bool? TextAlignmentCenterForShortContent { get; set; }

        [XmlElement("HorizontalAlignment")]
        public HorizontalAlignment? HorizontalAlignment { get; set; }

        [XmlElement("VerticalAlignment")]
        public VerticalAlignment? VerticalAlignment { get; set; }

        [XmlElement("WordSpacing")]
        public float? WordSpacing { get; set; }

        [XmlElement("SpaceFontSize")]
        public float? SpaceFontSize { get; set; }

        [XmlElement("CharacterSpacing")]
        public float? CharacterSpacing { get; set; }

        [XmlElement("Rowspan")]
        public int? Rowspan { get; set; }

        [XmlElement("Colspan")]
        public int? Colspan { get; set; }

        [XmlElement("UseHeaderCell")]
        public bool? UseHeaderCell { get; set; }

        [XmlElement("TextCellWidthEnabled")]
        public bool? TextCellWidthEnabled { get; set; }

        [XmlElement("CellColumnWidthEnabled")]
        public bool? CellColumnWidthEnabled { get; set; }

        

        [XmlElement("Border")]
        public BorderStyle Border { get; set; }

        [XmlElement("CustomizeHeightBorder")]
        public CustomizeHeightBorderStyle CustomHeightBorder { get; set; }

        [XmlElement("BorderRight")]
        public BorderStyle BorderRight { get; set; }

        [XmlElement("BorderTop")]
        public BorderStyle BorderTop { get; set; }

        [XmlElement("BorderBottom")]
        public BorderStyle BorderBottom { get; set; }

        [XmlElement("BorderLeft")]
        public BorderStyle BorderLeft { get; set; }

        [XmlElement("Padding")]
        public float? Padding { get; set; }

        [XmlElement("PaddingLeft")]
        public float? PaddingLeft { get; set; }

        [XmlElement("PaddingTop")]
        public float? PaddingTop { get; set; }

        [XmlElement("PaddingRight")]
        public float? PaddingRight { get; set; }

        [XmlElement("PaddingBottom")]
        public float? PaddingBottom { get; set; }

        [XmlElement("LastPaddingBottom")]
        public float? LastPaddingBottom { get; set; }

        [XmlElement("PaddingLeftExceptFirst")]
        public float? PaddingLeftExceptFirst { get; set; }

        [XmlElement("Margin")]
        public float? Margin { get; set; }

        [XmlElement("MarginLeft")]
        public float? MarginLeft { get; set; }

        [XmlElement("MarginTop")]
        public float? MarginTop { get; set; }

        [XmlElement("MarginRight")]
        public float? MarginRight { get; set; }

        [XmlElement("MarginBottom")]
        public float? MarginBottom { get; set; }

        [XmlElement("Position")]
        public ElementPosition Position { get; set; }

        [XmlElement("Rectangle")]
        public ElementRectangle Rectangle { get; set; }

        [XmlElement("ColCount")]
        public int? ColumnCount { get; set; }

        [XmlElement("ColumnPercentWidth")]
        public string ColumnPercentWidth { get; set; }

        [XmlElement("ColumnPointWidth")]
        public string ColumnPointWidth { get; set; }

        [XmlElement("ListValueStyleType")]
        public ListValueStyleType? ListValueStyleType { get; set; }

        [XmlElement("KeepTogether")]
        public bool? KeepTogether { get; set; }

        [XmlElement("KeepTogetherWhenNoneRowSpan")]
        public bool? KeepTogetherWhenNoneRowSpan { get; set; }

        [XmlElement("KeepWithNext")]
        public bool? KeepWithNext { get; set; }

        [XmlElement("RotationRatio")]
        public double? RotationRatio { get; set; }

        [XmlElement("LargeTable")]
        public bool? LargeTable { get; set; }

        [XmlElement("FirstLineIndent")]
        public float? FirstLineIndent { get; set; }

        [XmlElement("Item")]
        public List<ElementItem> Items { get; set; }

        [XmlElement("ChunkTag")]

        public List<ChunkTag> Tags { get; set; }

        [XmlElement("InlineParagrah")]
        public ElementItem InlineItem { get; set; }

        [XmlElement("ListNumberingType")]
        public ListNumberingType? ListNumberingType { get; set; }

        [XmlElement("ListSymbol")]
        public string ListSymbol { get; set; }

        [XmlElement("ListSymbolIndent")]
        public float? ListSymbolIndent { get; set; }

        [XmlElement("PostSymbolText")]
        public string PostSymbolText { get; set; }

        [XmlElement("PreSymbolText")]
        public string PreSymbolText { get; set; }

        [XmlElement("ListSymbolAlignment")]
        public ListSymbolAlignment? ListSymbolAlignment { get; set; }

        [XmlElement("FloatProperty")]
        public FloatPropertyValue? FloatProperty { get; set; }

        [XmlElement("Hyphenation")]
        public string Hyphenation { get; set; }

        [XmlElement("SplitCharacters")]
        public string SplitCharacters { get; set; }

        public bool? DrawObjectRows { get; set; }

        [XmlElement("CatchImageEx")]
        public bool? CatchImageEx { get; set; }

        [XmlElement("BreakAll")]
        public bool? BreakAll { get; set; }

        [XmlElement("CaptionFootnotesSameWidthWithTable")]
        public bool? CaptionFootnotesSameWidthWithTable { get; set; }

        public bool? FontSizeCustomized { get; set; }

    }
}
