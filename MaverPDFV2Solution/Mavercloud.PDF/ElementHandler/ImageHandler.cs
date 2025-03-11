using iText.IO.Image;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mavercloud.PDF.ElementHandler
{
    public class ImageHandler : ElementBaseHandler<Image>
    {
        private float? fixedWidth;

        private float? fixedHeight;

        private float? availableWidth;

        private string actionUrl;

        protected TableDisplayType? tableDisplayType;

        public ImageHandler(object sourceData, ElementItem item, string styleName, Document document, float? fixedWidth)
            : base(sourceData, item, styleName, document)
        {
            this.fixedWidth = fixedWidth;
        }

        public ImageHandler(object sourceData, ElementItem item, string styleName, Document document, float? fixedWidth, float? availableWidth)
            : base(sourceData, item, styleName, document)
        {
            this.fixedWidth = fixedWidth;
            this.availableWidth = availableWidth;
        }
        public ImageHandler(object sourceData, ElementItem item, string styleName, Document document) 
            : base(sourceData, item, styleName, document)
        {
           
        }

        public ImageHandler(object sourceData, ElementItem item, string styleName, Document document, string actionUrl)
            : base(sourceData, item, styleName, document)
        {
            this.actionUrl = actionUrl;
        }

        public ImageHandler(object sourceData, ElementItem item, string styleName, Document document, float? fixedWidth, TableDisplayType? tableDisplayType)
            : base(sourceData, item, styleName, document)
        {
            this.fixedWidth = fixedWidth;
            this.tableDisplayType = tableDisplayType;
        }

        public ImageHandler(object sourceData, ElementItem item, string styleName, Document document, float? fixedWidth, float? fixedHeight, TableDisplayType? tableDisplayType)
            : base(sourceData, item, styleName, document)
        {
            this.fixedWidth = fixedWidth;
            this.fixedHeight = fixedHeight;
            this.tableDisplayType = tableDisplayType;
        }

        public override Image Handle()
        {
            Image image = null;
            if (CheckStyle())
            {
                try
                {
                    var imageUrl = string.Empty;
                    if (sourceData is string)
                    {
                        imageUrl = sourceData.ToString();
                    }
                    else
                    {
                        imageUrl = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(item.ValuePath, sourceData).SafeString();
                    }
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        if (item.Rectangle != null)
                        {
                            ImageData imageData = null;
                            if (imageUrl.ToLower().StartsWith("http"))
                            {
                                imageData = ImageDataFactory.Create(new Uri(imageUrl));
                            }
                            else
                            {
                                imageData = ImageDataFactory.Create(imageUrl);
                            }
                            var recWidth = 0f;
                            if (item.Rectangle.Width.HasValue)
                            {
                                recWidth = item.Rectangle.Width.Value;
                            }
                            else if (item.Rectangle.Height.HasValue && !item.Rectangle.Width.HasValue) 
                            {
                                recWidth = GetImageRatioWidth(imageData, item.Rectangle.Height.Value);
                            }
                            image = new Image(imageData, item.Rectangle.Left.Value, item.Rectangle.Bottom.Value, recWidth);
                        }
                        else
                        {
                            ImageData imageData = null;
                            if (imageUrl.ToLower().StartsWith("http"))
                            {
                                imageData = ImageDataFactory.Create(new Uri(imageUrl));
                            }
                            else
                            {
                                imageData = ImageDataFactory.Create(imageUrl);
                            }
                            image = new Image(imageData);

                            if (item.Width.HasValue)
                            {

                                if (item.Height.HasValue)
                                {
                                    image.SetWidth(item.Width.Value);
                                    image.SetHeight(item.Height.Value);
                                }
                                else
                                {
                                    image.SetHeight(GetImageRatioHeight(imageData, item.Width.Value));
                                    image.SetWidth(item.Width.Value);
                                }
                            }
                            else if (item.Height.HasValue)
                            {
                                if (item.Width.HasValue)
                                {
                                    image.SetWidth(item.Width.Value);
                                    image.SetHeight(item.Height.Value);
                                }
                                else
                                {
                                    image.SetWidth(GetImageRatioWidth(imageData, item.Height.Value));
                                    image.SetHeight(item.Height.Value);
                                }

                            }
                            else
                            {
                                image.ScaleToFit(GetImageWidth(imageData), GetImageHeight(imageData));
                            }

                        }
                        SetMargin(image);
                        SetPadding(image);
                        SetOuterAlignment(image);
                        if (item.FloatProperty.HasValue)
                        {
                            image.SetProperty(Property.FLOAT, item.FloatProperty.Value);
                        }
                        if (!string.IsNullOrEmpty(actionUrl))
                        {
                            SetUriAction(image, actionUrl);
                        }
                        image.SetBorder(Border.NO_BORDER);
                    }
                }
                catch (Exception ex)
                {
                    if (!item.CatchImageEx.GetValueOrDefault(true))
                    {
#if !DEBUG
                        throw ex;
#endif
                    }
                }
            }
            return image;
        }

        private float GetImageHeight(ImageData image)
        {
            float availdHeight = 0;

            if (tableDisplayType == TableDisplayType.PageRotation)
            {
                availdHeight = document.GetPdfDocument().GetDefaultPageSize().GetWidth() - ElementGenerator.DocumentMargins[0] - ElementGenerator.DocumentMargins[1];
            }
            else
            {
                availdHeight = document.GetPdfDocument().GetDefaultPageSize().GetHeight() - ElementGenerator.DocumentMargins[0] - ElementGenerator.DocumentMargins[1];
            }
            if (!fixedHeight.HasValue)
            {
                if (!fixedWidth.HasValue)
                {
                    float dpi = image.GetDpiY();
                    //if (dpi > 300f)
                    //{
                    //    dpi = 72f * 2.5f;
                    //}
                    float height = Math.Min((image.GetHeight() / float.Parse(dpi.ToString())) * 72f, availdHeight);
                    if (height == availdHeight)
                    {
                        height -= 20;
                    }
                    return height;
                }
                else
                {
                    float height = image.GetHeight() * fixedWidth.Value / image.GetWidth();
                    return Math.Min(height, availdHeight);
                }
            }
            else
            {
                return fixedHeight.Value;
            }
        }

        private float GetImageWidth(ImageData image)
        {
            float availdWidth = 0f;
            if (availableWidth.HasValue)
            {
                availdWidth = availableWidth.Value;
            }
            else
            {
                if (tableDisplayType == TableDisplayType.PageRotation)
                {
                    availdWidth = document.GetPdfDocument().GetDefaultPageSize().GetHeight() - ElementGenerator.DocumentMargins[1] - ElementGenerator.DocumentMargins[3];
                }
                else
                {
                    availdWidth = document.GetPdfDocument().GetDefaultPageSize().GetWidth() - ElementGenerator.DocumentMargins[1] - ElementGenerator.DocumentMargins[3];
                }
            }
            if (fixedHeight.HasValue)
            {
                float width = image.GetWidth() * fixedHeight.Value / image.GetHeight();
                return width;
            }
            else
            {
                if (!fixedWidth.HasValue)
                {
                    float dpi = image.GetDpiX();
                    //if (dpi > 300)
                    //{
                    //    dpi = 72f * 2.5f;
                    //}

                    float width = Math.Min((image.GetWidth() / float.Parse(dpi.ToString())) * 72f, availdWidth);
                    if (width == availdWidth)
                    {
                        width -= 1;
                    }
                    return width;
                }
                else
                {
                    return Math.Min(fixedWidth.Value, availdWidth);
                }
            }
        }

        private float GetImageY(ImageData image)
        {
            float dpi = image.GetDpiY();
            return image.GetHeight() / float.Parse(dpi.ToString()) * 72f;
        }

        private float GetImageX(ImageData image)
        {
            float dpi = image.GetDpiX();
            return image.GetWidth() / float.Parse(dpi.ToString()) * 72f;
        }

        private float GetImageRatioWidth(ImageData image, float height)
        {
            var x = GetImageX(image);
            var y = GetImageY(image);

            return x * height / y;
        }

        private float GetImageRatioHeight(ImageData image, float width)
        {
            var x = GetImageX(image);
            var y = GetImageY(image);

            return y * width / x;
        }
    }
}
