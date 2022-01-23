using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace YARG.Data
{
    public class TouchData : ICloneable
    {
        public int FingerId { get; set; }

        public Vector2 LastPosition { get; set; }

        public Vector2 DeltaPosition { get; set; }

        public TouchPhase Phase { get; set; }

        public int TriggeredChannel { get; set; }

        public int OldTriggeredChannel { get; set; } = -1;

        public float ChannelPosition { get; set; }

        public object Clone()
        {
            return new TouchData() { 
                FingerId = FingerId,
                LastPosition = new Vector2(LastPosition.x, LastPosition.y),
                DeltaPosition = new Vector2(DeltaPosition.x, DeltaPosition.y),
                Phase = Phase,
                TriggeredChannel = TriggeredChannel,
                OldTriggeredChannel = OldTriggeredChannel,
                ChannelPosition = ChannelPosition
            };
        }
    }
}
