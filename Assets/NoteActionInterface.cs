using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YARG.Data;
using UnityEngine;

namespace YARG
{
    public interface NoteActionInterface
    {
        public void HandleTouchIn(TouchData touchData);

        public void HandleTouchKeep(TouchData touchData);

        public void HandleTouchOut(TouchData touchData);

        public Vector2 GetTouchBoundingBox();
    }
}
