using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberLab.Resources.Services
{
    public class TimerEventService
    {
        public event Action TimerTriggered;
        public void RaiseTimerEvent() => TimerTriggered?.Invoke();
    }
}
