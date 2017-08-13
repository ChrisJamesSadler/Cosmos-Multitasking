using System;
using System.Collections.Generic;
using System.Text;

namespace Cosmos.Core
{
    public static unsafe class Scheduler
    {
        private static int LTID;
        public static int currentThread = 0;
        public static List<ThreadData> allThreads;

        public static bool usingMultitasking = false;

        public static void Init()
        {
            allThreads = new List<ThreadData>();
            CreateNewThread(0);
            Timer.Init();
            INTs.SetIrqHandler(0, SwitchTast);
        }

        public static void SwitchTast(ref INTs.IRQContext aContext)
        {
            if(!usingMultitasking || allThreads.Count == 0)
            {
                return;
            }
            if (allThreads.Count > 0)
            {
                allThreads[currentThread].ESP = INTs.old_esp;
                currentThread++;
                if (currentThread >= allThreads.Count)
                {
                    currentThread = 0;
                }
                INTs.old_esp = allThreads[currentThread].ESP;
            }
        }

        public static void CreateNewThread(uint entry)
        {
            ThreadData data = new ThreadData();
            uint userStack = (uint)GCImplementation.AllocNewObject(4096);
            uint* stack = (uint*)(userStack + 4096);
            *--stack = 0x10; // ss ?
            *--stack = 0x00000202; // eflags
            *--stack = 0x8; // cs
            *--stack = entry; // eip
            *--stack = 0; // error
            *--stack = 0; // int
            *--stack = 0; // eax
            *--stack = 0; // ecx
            *--stack = 0; // edx
            *--stack = 0; // ebx
            *--stack = 0; // offset
            *--stack = userStack + 4096; //ebp
            *--stack = 0; // esi
            *--stack = 0; // edi

            data.ESP = (uint)stack;
            allThreads.Add(data);
        }

        public static void AddThread(ThreadData data)
        {
            bool found = false;
            for (int i = 0; i < allThreads.Count; i++)
            {
                if (allThreads[i] == data)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                allThreads.Add(data);
            }
        }

        public static void RemoveThread(ThreadData data)
        {
            for (int i = 0; i < allThreads.Count; i++)
            {
                if (allThreads[i] == data)
                {
                    allThreads.RemoveAt(i);
                    return;
                }
            }
        }

        public class ThreadData
        {
            public int ID;
            public uint ESP;

            public ThreadData()
            {
                ID = LTID++;
            }
        }
    }

    public static class Timer
    {
        private const byte PIT_A = 0x40;
        private const byte PIT_B = 0x41;
        private const byte PIT_C = 0x42;
        private const byte CONTROL = 0x43;

        private const byte MASK = 0xFF;
        private const uint SCALE = 1193180;
        private const byte SET = 0x36;

        public static uint IPS = 20;

        public static void Init()
        {
            IOPort p20 = new IOPort(0x20);
            IOPort pa0 = new IOPort(0xA0);
            IOPort p21 = new IOPort(0x21);
            IOPort pa1 = new IOPort(0xA1);

            p20.Byte = 0x11;
            pa0.Byte = 0x11;
            p21.Byte = 0x20;
            pa1.Byte = 0x28;
            p21.Byte = 0x04;
            pa1.Byte = 0x02;
            p21.Byte = 0x01;
            pa1.Byte = 0x01;
            p21.Byte = 0x00;
            p21.Byte = 0x00;
            pa1.Byte = 0x00;
            timer_phase(IPS);
        }

        public static void timer_phase(uint hz)
        {
            uint divisor = SCALE / hz;
            new IOPort(CONTROL).Byte = SET;
            new IOPort(PIT_A).Byte = (byte)(divisor & MASK);
            new IOPort(PIT_A).Byte = (byte)((divisor >> 8) & MASK);
        }
    }
}
