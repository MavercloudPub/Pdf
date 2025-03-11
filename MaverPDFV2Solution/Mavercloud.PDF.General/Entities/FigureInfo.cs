using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Entities
{
    [Serializable]
    public class FigureInfo : BodyItemBase
    {
        public string Id { get; set; }

        public string IdInXml { get; set; }

        public TableDisplayType? TableDisplayType { get; set; }

        public string Label
        {
            get;
            set;
        }

        public string Title { get; set; }

        public string Caption
        {
            get;
            set;
        }

        public Dictionary<string, string> Captions
        {
            get; set;
        }



        public List<string> ImageUrls
        {
            get;
            set;
        }

        public int? DrawAtPdfPage { get; set; }

        public float? Width { get; set; }

        public float? Height { get; set; }

        public ImageType ImageType { get; set; }
    }

    public enum ImageType
    { 
        Image = 1,
        Equation = 2,
        Scheme = 3,
    }
}
