using Force.DeepCloner;
using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.General.Element;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Helpers
{
    public static class StyleHelper
    {
        public static ElementItem GetElementItemByParagraphStyle(ElementItem item, ParagraphStyleInfo paragraphStyle)
        {
            var clonedItem = item.DeepClone();
            if (paragraphStyle.SpaceFontSize.HasValue)
            {
                clonedItem.SpaceFontSize = paragraphStyle.SpaceFontSize.Value;
            }
            if (paragraphStyle.PaddingTop.HasValue)
            {
                clonedItem.PaddingTop = paragraphStyle.PaddingTop.Value;
            }
            if (paragraphStyle.PaddingBottom.HasValue)
            {
                clonedItem.PaddingBottom = paragraphStyle.PaddingBottom.Value;
            }
            if (paragraphStyle.MultipliedLeading.HasValue)
            {
                clonedItem.MultipliedLeading = paragraphStyle.MultipliedLeading.Value;
            }
            if (!string.IsNullOrEmpty(paragraphStyle.Hypenation))
            {
                clonedItem.Hyphenation = paragraphStyle.Hypenation;
                
            }
            return clonedItem;
        }

        public static void SetElementItemParagraphStyle(ElementItem clonedItem, ParagraphStyleInfo paragraphStyle)
        {
            if (paragraphStyle.SpaceFontSize.HasValue)
            {
                clonedItem.SpaceFontSize = paragraphStyle.SpaceFontSize.Value;
            }
            if (paragraphStyle.PaddingTop.HasValue)
            {
                clonedItem.PaddingTop = paragraphStyle.PaddingTop.Value;
            }
            if (paragraphStyle.PaddingBottom.HasValue)
            {
                clonedItem.PaddingBottom = paragraphStyle.PaddingBottom.Value;
            }
            if (paragraphStyle.MultipliedLeading.HasValue)
            {
                clonedItem.MultipliedLeading = paragraphStyle.MultipliedLeading.Value;
            }
            if (!string.IsNullOrEmpty(paragraphStyle.Hypenation))
            {
                clonedItem.Hyphenation = paragraphStyle.Hypenation;
            }
        }
    }
}
