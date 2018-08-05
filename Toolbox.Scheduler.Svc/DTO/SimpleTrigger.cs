﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Toolbox.Scheduler.Svc.DTO
{
    [DataContract]
    public enum SimpleTriggerType { second, minutes, hours, days, weeks }

    [DataContract]
    public class SimpleTrigger : AbstractTrigger
    {
        public SimpleTriggerType SimpleTriggerType { get; set; }
        public int Interval { get; set; }
    }
}