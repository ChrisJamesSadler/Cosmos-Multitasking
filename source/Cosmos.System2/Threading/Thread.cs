using System;
using System.Collections.Generic;
using System.Text;

namespace Cosmos.System.Threading
{
    public delegate void ThreadStart();

    public class Thread
    {
        private ThreadStart entry;

        public Thread(ThreadStart entryPoint)
        {
            entry = entryPoint;
        }

        public void Start()
        {
            HAL.Global.CreateThread((uint)entry.GetHashCode());
        }
    }
}
