using System;

namespace bsp2srf
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] actualArgs = new string[args.Length - 1];

            string command = args[0];
            Array.Copy(args, 1, actualArgs,0, args.Length - 1);

            switch (command)
            {
                case "bsp2srf":
                    bsp2srf(actualArgs);
                    break;
                case "lmpatch":
                    lmpatch(actualArgs);
                    break;
            }
            
            Console.ReadKey();
        }

        static void lmpatch(string[] args)
        {
            string baseBsp = args[0];
            string bspWithNewLightmaps = args[1];


            BSP bsp = new BSP(baseBsp);
            BSP.Surface[] baseSurfaces = bsp.getSurfaces();
            BSP bsp2 = new BSP(bspWithNewLightmaps);
            BSP.Surface[] patchSurfaces = bsp2.getSurfaces();

            byte[] patchFile = BSP.createLightMapPatchFile(baseSurfaces, patchSurfaces);

            Console.WriteLine("test");
        }
        static void bsp2srf(string[] args)
        {
            foreach (string arg in args)
            {
                BSP bsp = new BSP(arg);
                bsp.bsp2srf(arg + ".srf");
            }

        }
    }
}
