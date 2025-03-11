using iText.Kernel.Geom;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Biz
{
    public class Constants
    {
        public const string OpenAccess = "OA";

        public static float[] DocumentMargins = new float[] { 54.69f, 56.69f, 56.69f, 56.69f };

        public static PageSize DefaultPageSize = new PageSize(595.3f, 841.9f);

        public static float FirstPageBottomMargin = 110f;

        public static float FirstPageBottomMarginForContinuousPublish = 56.69f;

        public static float PageAvailableWidth = 481.5f;

        public const float PageTop = 787.2f;

        public const float ParagraphFirstLineIndent = 19.84f;

        public const float ListIndent = 9.92f;

        
    }
}
