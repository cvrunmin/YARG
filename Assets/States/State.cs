using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YARG.States
{
    public abstract class State
    {
        public abstract void OnEnter();

        public abstract void Tick();

        public abstract void OnExit();
    }

    public class StateType
    {
        public string Name { get; private set; }

        
    }
}
