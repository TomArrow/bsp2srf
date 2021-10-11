using System;

namespace bsp2srf
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach(string arg in args)
            {
                BSP bsp = new BSP(arg);
                bsp.doStuff(arg + ".srf");
            }

#if DEBUG
            BSP bsp2 = new BSP("ctf_yavin.bsp");
            bsp2.doStuff("ctf_yavin.srf");
#endif
            Console.ReadKey();
        }
    }
}
