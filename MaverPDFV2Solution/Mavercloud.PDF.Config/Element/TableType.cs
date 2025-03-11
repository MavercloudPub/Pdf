using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Config.Element
{
    public enum TableType
    {
        Normal = 1,
        Large = 2,
    }

    public enum TableDisplayType
    { 
        Normal = 1,
        PageRotation = 2,
        EndOfMain = 3,
    }

    public enum TablePosition
    { 
        TopOfPage = 1,
        FollowingWithContent = 2,
        TopColumn = 3,
    }
}
