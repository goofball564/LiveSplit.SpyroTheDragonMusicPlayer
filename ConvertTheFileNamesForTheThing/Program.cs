using System.Collections.Generic;
using System.IO;

namespace ConvertTheFileNamesForTheThing
{
    class Program
    {
        private static readonly Dictionary<string, int[]> WorldsOfFiles = new Dictionary<string, int[]>
        {
            {"PETEXA0.STR[0]", new int[] {10,}},
            {"PETEXA0.STR[1]", new int[] {45,}},
            {"PETEXA0.STR[2]", new int[] {11,}},
            {"PETEXA0.STR[3]", new int[] {21,}},
            {"PETEXA0.STR[4]", new int[] {53,}},
            {"PETEXA0.STR[5]", new int[] {61,}},
            {"PETEXA0.STR[6]", new int[] {15,}},
            {"PETEXA0.STR[7]", new int[] {41,}},
            {"PETEXA1.STR[0]", new int[] {24,}},
            {"PETEXA1.STR[1]", new int[] {33,}},
            {"PETEXA1.STR[2]", new int[] {55,}},
            {"PETEXA1.STR[3]", new int[] {25,}},
            {"PETEXA1.STR[4]", new int[] {51,}},
            {"PETEXA1.STR[5]", new int[] {23,}},
            {"PETEXA1.STR[6]", new int[] {52,}},
            {"PETEXA1.STR[7]", new int[] {40,}},
            {"PETEXA2.STR[0]", new int[] {34,}},
            {"PETEXA2.STR[1]", new int[] {20,}},
            {"PETEXA2.STR[2]", new int[] {35,}},
            {"PETEXA2.STR[3]", new int[] {64,}},
            {"PETEXA2.STR[4]", new int[] {}},
            {"PETEXA2.STR[5]", new int[] {42,}},
            {"PETEXA2.STR[6]", new int[] {31,}},
            {"PETEXA2.STR[7]", new int[] {44,}},
            {"PETEXA3.STR[0]", new int[] {62,}},
            {"PETEXA3.STR[1]", new int[] {63,}},
            {"PETEXA3.STR[2]", new int[] {13,}},
            {"PETEXA3.STR[3]", new int[] {22,}},
            {"PETEXA3.STR[4]", new int[] {54,}},
            {"PETEXA3.STR[5]", new int[] {43,}},
            {"PETEXA3.STR[6]", new int[] {}},
            {"PETEXA3.STR[7]", new int[] {30,}},
            {"PETEXA4.STR[0]", new int[] {12,}},
            {"PETEXA4.STR[1]", new int[] {14, 60,}},
            {"PETEXA4.STR[2]", new int[] {50,}},
            {"PETEXA4.STR[3]", new int[] {}},
            {"PETEXA4.STR[4]", new int[] {}},
            {"PETEXA4.STR[5]", new int[] {}},
            {"PETEXA4.STR[6]", new int[] {}},
            {"PETEXA4.STR[7]", new int[] {}},
            {"PETEXA5.STR[0]", new int[] {32,}},
            {"PETEXA5.STR[1]", new int[] {}},
            {"PETEXA5.STR[2]", new int[] {}},
            {"PETEXA5.STR[3]", new int[] {}},
            {"PETEXA5.STR[4]", new int[] {}},
            {"PETEXA5.STR[5]", new int[] {}},
            {"PETEXA5.STR[6]", new int[] {}},
            {"PETEXA5.STR[7]", new int[] {}},
        };

        private const string OUTPUT_FOLDER_NAME = "Spyro1Music";
        private const string REPLACEMENT_NAME = "Spyro1Music_old";

        static void Main(string[] args)
        {
            if (File.Exists(OUTPUT_FOLDER_NAME))
                File.Move(OUTPUT_FOLDER_NAME, REPLACEMENT_NAME);

            if (!Directory.Exists(OUTPUT_FOLDER_NAME))
                Directory.CreateDirectory(OUTPUT_FOLDER_NAME);

            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());

            foreach(string file in files)
            {
                string fileStem = Path.GetFileNameWithoutExtension(file);
                string fileSuffix = Path.GetExtension(file);
                int[] worlds;
                if (WorldsOfFiles.TryGetValue(fileStem, out worlds))
                {
                    for(int i = 0; i < worlds.Length; i++)
                    {
                        if (i == worlds.Length - 1)
                            File.Move(file, Path.Combine(OUTPUT_FOLDER_NAME, worlds[i] + fileSuffix));
                        else
                            File.Copy(file, Path.Combine(OUTPUT_FOLDER_NAME, worlds[i] + fileSuffix));
                    }
                }
            }
        }
    }
}
