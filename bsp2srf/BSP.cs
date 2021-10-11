using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;

namespace bsp2srf
{
    class BSP
    {
        enum LUMPS { 
            LUMP_ENTITIES = 0,
            LUMP_SHADERS = 1,
            LUMP_PLANES = 2,
            LUMP_NODES = 3,
            LUMP_LEAFS = 4,
            LUMP_LEAFSURFACES = 5,
            LUMP_LEAFBRUSHES = 6,
            LUMP_MODELS = 7,
            LUMP_BRUSHES = 8,
            LUMP_BRUSHSIDES = 9,
            LUMP_DRAWVERTS = 10,
            LUMP_DRAWINDEXES = 11,
            LUMP_FOGS = 12,
            LUMP_SURFACES = 13,
            LUMP_LIGHTMAPS = 14,
            LUMP_LIGHTGRID = 15,
            LUMP_VISIBILITY = 16,
            LUMP_LIGHTARRAY = 17,
        }
        private const int HEADER_LUMPS = 18;
        private const int MAX_QPATH = 64;
        private const int MAXLIGHTMAPS = 4;
        private const int LIGHTMAPSIZE = 128;



        byte[] contents;
        string inputFile;

        public BSP(string filepath)
        {
            contents = File.ReadAllBytes(filepath);

            inputFile = filepath;
        }

        struct Lump
        {
            public int offset;
            public int length;
        }
        struct Shader
        {
            public string shaderName;
            public int surfaceFlags;
            public int contentFlags;
        }
        struct Surface
        {
            public int shaderNum;
            public Shader shader;
            public Vector3 lightmapOrigins;
            public Vector3[] lightmapVecs;
            public int numVerts;
            public int numIndexes;
        }

        public void doStuff(string outputFile){
            using(BinaryReader sr = new BinaryReader( new MemoryStream(contents)))
            {

                int ident = sr.ReadInt32();
                int version = sr.ReadInt32();

                Lump[] lumps = new Lump[HEADER_LUMPS];
                for(int i = 0; i < HEADER_LUMPS; i++)
                {
                    int offset = sr.ReadInt32();
                    int length = sr.ReadInt32();
                    lumps[i] = new Lump() { offset = offset, length = length };
                    Console.WriteLine((LUMPS)i);
                    Console.WriteLine(offset);
                    Console.WriteLine(length);
                }

                int singleShaderLength = MAX_QPATH + 4 + 4;
                Shader[] shaders = new Shader[lumps[(int)LUMPS.LUMP_SHADERS].length/ singleShaderLength];

                sr.BaseStream.Seek(lumps[(int)LUMPS.LUMP_SHADERS].offset, SeekOrigin.Begin);
                for(int i=0; i < shaders.Count(); i++)
                {
                    //string shaderName = Encoding.ASCII.GetString( sr.ReadBytes(MAX_QPATH));
                    
                    string shaderName = Encoding.ASCII.GetString( sr.ReadBytes(MAX_QPATH)).TrimEnd((Char)0);
                    int surfaceFlags = sr.ReadInt32();
                    int contentFlags = sr.ReadInt32();
                    shaders[i] = new Shader() { shaderName = shaderName, surfaceFlags = surfaceFlags, contentFlags = contentFlags };
                    Console.WriteLine(shaderName);
                }


                int lightmapLength = LIGHTMAPSIZE*LIGHTMAPSIZE*3;
                Bitmap exampleBitmap = new Bitmap(LIGHTMAPSIZE, LIGHTMAPSIZE);
                LinearAccessByteImageUnsignedNonVectorized exampleByteImage = LinearAccessByteImageUnsignedNonVectorized.FromBitmap(exampleBitmap);
                LinearAccessByteImageUnsignedHusk exampleHusk = exampleByteImage.toHusk();
                int lightmapCount = lumps[(int)LUMPS.LUMP_LIGHTMAPS].length / lightmapLength;
                //Bitmap[] lightmap  = new Bitmap[lumps[(int)LUMPS.LUMP_LIGHTMAPS].length/ lightmapLength];

                Directory.CreateDirectory(inputFile+".lightmaps");

                sr.BaseStream.Seek(lumps[(int)LUMPS.LUMP_LIGHTMAPS].offset, SeekOrigin.Begin);
                for(int i=0; i < lightmapCount; i++)
                {
                    byte[] data = sr.ReadBytes(lightmapLength);
                    byte[] dataFixed = new byte[lightmapLength];

                    // invert rows and bgr to rgb, or rather the other way?
                    for(int y = 0; y < LIGHTMAPSIZE; y++)
                    {
                        int destY = LIGHTMAPSIZE - 1 - y;
                        for (int x = 0; x < LIGHTMAPSIZE; x++)
                        {
                            dataFixed[destY * LIGHTMAPSIZE * 3 + x * 3] = data[y * LIGHTMAPSIZE * 3 + x * 3 +2];
                            dataFixed[destY * LIGHTMAPSIZE * 3 + x * 3 +1] = data[y * LIGHTMAPSIZE * 3 + x * 3 +1];
                            dataFixed[destY * LIGHTMAPSIZE * 3 + x * 3 +2] = data[y * LIGHTMAPSIZE * 3 + x * 3];
                        }
                    }

                    Bitmap lightmap = new LinearAccessByteImageUnsignedNonVectorized(dataFixed, exampleHusk).ToBitmap();
                    lightmap.Save(inputFile + ".lightmaps" + Path.DirectorySeparatorChar + "lightmap_"+i.ToString("0000")+".png");
                    lightmap.Dispose();
                }


                /*
                typedef struct {
	                int			shaderNum;
	                int			fogNum;
	                int			surfaceType;

	                int			firstVert;
	                int			numVerts;

	                int			firstIndex;
	                int			numIndexes; //7*4

	                byte		lightmapStyles[MAXLIGHTMAPS], vertexStyles[MAXLIGHTMAPS]; //8
	                int			lightmapNum[MAXLIGHTMAPS]; //4*4
	                int			lightmapX[MAXLIGHTMAPS], lightmapY[MAXLIGHTMAPS]; // 8*4
	                int			lightmapWidth, lightmapHeight;//8

	                vec3_t		lightmapOrigin; //3*4
	                vec3_t		lightmapVecs[3];	// for patches, [0] and [1] are lodbounds //3*3*4

	                int			patchWidth;
	                int			patchHeight; //2*4
                } dsurface_t; 

                // Total: 7*4 + 8 + 4*4+8*4+8+3*4+3*3*4+2*4 = 148

                 */



                int singleSurfaceLength = 148;
                Surface[] surfaces = new Surface[lumps[(int)LUMPS.LUMP_SURFACES].length/ singleSurfaceLength];

                sr.BaseStream.Seek(lumps[(int)LUMPS.LUMP_SURFACES].offset, SeekOrigin.Begin);
                for(int i=0; i < surfaces.Count(); i++)
                {
                    int shaderNum = sr.ReadInt32();
                    int fogNum = sr.ReadInt32();
                    int surfaceType = sr.ReadInt32();

                    int firstVert = sr.ReadInt32();
                    int numVerts = sr.ReadInt32();

                    int firstIndex = sr.ReadInt32();
                    int numIndexes = sr.ReadInt32();

                    _ = sr.ReadBytes(MAXLIGHTMAPS); // lightmapStyles
                    _ = sr.ReadBytes(MAXLIGHTMAPS); // vertexStyles
                    _ = sr.ReadBytes(MAXLIGHTMAPS * sizeof(int)); // lightmapNum
                    _ = sr.ReadBytes(MAXLIGHTMAPS * sizeof(int)); // lightmapX
                    _ = sr.ReadBytes(MAXLIGHTMAPS * sizeof(int)); // lightmapY
                    _ = sr.ReadInt32(); // lightmapWidth
                    _ = sr.ReadInt32(); // lightmapHeight

                    Vector3 lightmapOrigin;
                    lightmapOrigin.X = sr.ReadSingle();
                    lightmapOrigin.Y = sr.ReadSingle();
                    lightmapOrigin.Z = sr.ReadSingle();

                    Vector3[] lightmapVecs = new Vector3[3];
                    lightmapVecs[0].X = sr.ReadSingle();
                    lightmapVecs[0].Y = sr.ReadSingle();
                    lightmapVecs[0].Z = sr.ReadSingle();
                    lightmapVecs[1].X = sr.ReadSingle();
                    lightmapVecs[1].Y = sr.ReadSingle();
                    lightmapVecs[1].Z = sr.ReadSingle();
                    lightmapVecs[2].X = sr.ReadSingle();
                    lightmapVecs[2].Y = sr.ReadSingle();
                    lightmapVecs[2].Z = sr.ReadSingle();

                    int patchWidth= sr.ReadInt32(); 
                    int patchHeight= sr.ReadInt32();

                    surfaces[i] = new Surface() { shaderNum = shaderNum, shader = shaders[shaderNum], numVerts = numVerts, numIndexes = numIndexes, lightmapOrigins = lightmapOrigin, lightmapVecs = lightmapVecs };
                    //Console.WriteLine(shaderName);
                }

                StringBuilder sb = new StringBuilder();

                sb.AppendLine(@"default
{
	castShadows 1
	receiveShadows 1
	sampleSize 16
	longestCurve 0.000000
}");

                for (int i = 0; i < surfaces.Count(); i++)
                {
                    Surface surface = surfaces[i];
                    sb.AppendLine(i.ToString()+@" // SURFACE_PATCH V: "+surface.numVerts+ @" I: " + surface.numIndexes + @" planar
{
	shader " + surface.shader.shaderName + @"
}");
                }

                File.WriteAllText(outputFile, sb.ToString());

                Console.Write("abc");

            }
        }



    }
}
