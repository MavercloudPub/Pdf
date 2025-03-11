using iText.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.EventHandler
{
    public abstract class EventHandlerBase : IEventHandler
    {
        public abstract void HandleEvent(Event @event);
        
    }
}
