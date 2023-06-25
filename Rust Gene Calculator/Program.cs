using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace Rust_Gene_Calculator
{
    class Program {
        /**
         * Gene bytes
         */
        const byte U = 0 << 0;
        const byte W = 1 << 0;
        const byte X = 1 << 1;
        const byte Y = 1 << 2;
        const byte G = 1 << 3;
        const byte H = 1 << 4;

        /**
         * Good an bad gene bitmasks
         *                  HGYXW
         */
        const byte GOOD = 0b11100;
        const byte BAD  = 0b00011;

        static void Main(string[] args) {
            Console.WriteLine("------- ------- ------- ------- ------- ------- -------");

            // return if no file was dropped on the exe
            if ( args.Length == 0 ) {
                Console.WriteLine("Usage: Drop a genes text file on the .exe file");
                Console.ReadKey();

                return;
            }

            List<string> genesStringList = File.ReadAllLines(args[0]).ToList();

            // get priorities
            double priorityY = 1;
            double priorityG = 0;
            double priorityH = 0;
            if (genesStringList[0] == "genes") {
                Console.WriteLine("DEFAULT priority:");
                Console.WriteLine("Y = " + priorityY);
                Console.WriteLine("G = " + priorityG);
                Console.WriteLine("H = " + priorityH);
            } else {
                try {
                    var priorities = JsonSerializer.Deserialize<Dictionary<string, double>>(genesStringList[0]);
                    priorityY = priorities["Y"];
                    priorityG = priorities["G"];
                    priorityH = priorities["H"];
                    Console.WriteLine("Priority:");
                    Console.WriteLine("Y = " + priorityY);
                    Console.WriteLine("G = " + priorityG);
                    Console.WriteLine("H = " + priorityH);
                } catch {
                    Console.WriteLine("File should start with 'genes' for default priorities, or '{\"Y\":number, \"G\":number, \"H\":number}', where 'number' is priority between 0 and 1");
                    Console.WriteLine("e.g. '{\"Y\":1, \"G\":1, \"H\":0}' would mean 100% priority for Y and G genes (50/50 split) and 0% priority for H gene");
                    Console.ReadKey();

                    return;
                }
            }

            // remove first "genes" line
            genesStringList = genesStringList.Skip(1).ToList();
            // remove empty lines
            genesStringList = genesStringList.Where(x => !string.IsNullOrEmpty(x.Trim())).ToList();

            // remove all plants that do not match gene priority
            for (int i = genesStringList.Count - 1; i >= 0; i--) {
                bool hasUsefulGenes = false;
                if ((priorityY > 0 && genesStringList[i].Contains('Y'))
                    || (priorityG > 0 && genesStringList[i].Contains('G'))
                    || (priorityH > 0 && genesStringList[i].Contains('H'))
                ) {
                    hasUsefulGenes = true;
                }

                if (!hasUsefulGenes) {
                    genesStringList.RemoveAt(i);
                }
            }

            { // validation scope
                bool isValidGenes = true;
                foreach (string gene in genesStringList) {
                    // check length
                    if (gene.Length < 6) {
                        isValidGenes = false;
                        break;
                    }

                    // check genes
                    for (int i = 0; i < 6; i++) {
                        if (gene[i] != 'W' && gene[i] != 'X' && gene[i] != 'Y' && gene[i] != 'G' && gene[i] != 'H') {
                            isValidGenes = false;
                            break;
                        }
                    }
                    if (!isValidGenes) {
                        break;
                    }
                }
                if (!isValidGenes) {
                    Console.WriteLine("Genes should be strings with exactly six W, X, Y, G or H characters");
                    Console.ReadKey();

                    return;
                }
            }

            Console.WriteLine("------- ------- ------- ------- ------- ------- -------");
            Console.WriteLine("Processing...");

            // find the best combination
            List<byte[]> bestCropParents = new();
            double bestCropValue = -7;
            byte[] bestCrop = new byte[6];
            byte[][] genesByteArray = new byte[genesStringList.Count][];
            for (int i = 0; i < genesStringList.Count; i++) {
                // change the string gene to a byte array
                byte[] crop = GeneStringToByteArray(genesStringList[i]);

                // save string genes as byte arrays
                genesByteArray[i] = crop;

                // find the best seed in the input
                double value = EvaluateCrop(crop, priorityY, priorityG, priorityH);
                if (value > bestCropValue) {
                    bestCropValue = value;
                    bestCropParents = new();
                    bestCrop = crop;
                }
            }

            // reduce heap size
            genesStringList = null;

            // for every possible combination
            foreach (List<byte[]> parents in BwPowerSet(genesByteArray, 8)) {
                string parentsString = "";
                foreach (byte[] parent in parents) {
                    parentsString += ByteArrayToGeneString(parent) + ", ";
                }

                byte[] crop = Crossbreed(parents);

                double value = EvaluateCrop(crop, priorityY, priorityG, priorityH);

                // set better crop if it is better or if it is equal and has less parents
                if (value > bestCropValue || (value == bestCropValue && parents.Count < bestCropParents.Count)) {
                    bestCropValue = value;
                    bestCropParents = parents.ToList();
                    bestCrop = crop;
                }
            }

            // generate best genes as string
            string bestCropString = "|";
            { // conversion scope
                byte[] geneBytes = new byte[6] { U, W, X, Y, G, H };
                string[] geneStrings = new string[6] { "U", "W", "X", "Y", "G", "H", };

                for (int i = 0; i < 6; i++) {
                    bool multipleGenesChance = false;
                    byte gene = bestCrop[i];
                    for (int j = 0; j < 6; j++) {
                        if ((gene & geneBytes[j]) > 0) {
                            if (multipleGenesChance) {
                                bestCropString += "/";
                            }
                            bestCropString += geneStrings[j];
                            multipleGenesChance = true;
                        }
                    }

                    bestCropString += "|";
                }
            }

            Console.WriteLine("------- ------- ------- ------- ------- ------- -------");
            Console.WriteLine("Best genes:");
            Console.WriteLine(bestCropString);
            Console.WriteLine();

            // print parent genes as strings
            Console.WriteLine("Crossbreed:");
            foreach (byte[] parent in bestCropParents) {
                Console.WriteLine(ByteArrayToGeneString(parent));
            }

            Console.WriteLine("------- ------- ------- ------- ------- ------- -------");
            Console.ReadKey();
        }

        /**
         * Calculates the worth of the crop depending on its genes - simple one for now could be improved
         */
        static double EvaluateCrop(byte[] crop, double priorityY, double priorityG, double priorityH) {
            double value = 0;

            // for each of the 6 genes
            for (int i = 0; i < 6; i++) {
                // evaluate each gene
                byte g = crop[i];
                if ((g & BAD) > 1) {
                    value -= 1;
                }
                value += Math.Max(
                    (g & Y) > 0 ? priorityY : 0,
                    Math.Max(
                        (g & G) > 0 ? priorityG : 0,
                        (g & H) > 0 ? priorityH : 0
                    )
                );

                // add a penalty for multi-choice genes
                int n = 0;
                while( g != 0 ) {
                    g = (byte)(g & (g - 1));
                    n++;
                }
                n = (n < 1) ? 1 : 0;
                value -= 0.1 * (n - 1);
            }

            return value;
        }

        /**
         * Taken from https://github.com/trekhleb/javascript-algorithms/tree/master/src/algorithms/sets/power-set
         * and modified to include max depth - cant crosbreed more than 8 at a time
         * Translated to C#
         */
        static List<List<byte[]>> BwPowerSet(byte[][] genesByteArray, int maxDepth = -1) {
            if (maxDepth == -1) {
                maxDepth = genesByteArray.Length;
            }

            double numberOfCombinations = Math.Pow(2, genesByteArray.Length);
            Console.WriteLine(numberOfCombinations + " combinations");

            List<List<byte[]>> subSets = new();
            for (int combinationIndex = 0; combinationIndex < numberOfCombinations; combinationIndex++) {
                List<byte[]> subSet = new();

                // get depth by counting the number of set bits
                int depth = 0;
                int depthC = combinationIndex;
                while (depthC != 0) {
                    depthC &= (depthC - 1);
                    depth++;
                    if (depth > maxDepth) {
                        break;
                    }
                }

                if (depth <= maxDepth) {
                    for (int setElementIndex = 0; setElementIndex < genesByteArray.Length; setElementIndex += 1) {
                        // decide whether we need to include current element into the subset or not
                        if ((combinationIndex & (1 << setElementIndex)) > 0) {
                            subSet.Add(genesByteArray[setElementIndex]);
                        }
                    }

                    // add current subset to the list of all subsets
                    subSets.Add(subSet);
                }
            }

            return subSets;
        }

        /**
         * Crossbreeding function that takes an array of parent crops and calculates the child
         */
        static byte[] Crossbreed(List<byte[]> parents) {
            byte[] child = new byte[6];
            // for each of the 6 genes
            for (int i = 0; i < 6; i++) {
                double[] geneTable = new double[5] { 0, 0, 0, 0, 0 };
                byte[] genes = new byte[5] { W, X, Y, G, H};

                // add up all the parent genes at i-th gene
                for (int p = 0; p < parents.Count; p++) {
                    switch (parents[p][i]) {
                        case W: geneTable[0] += 1; break;
                        case X: geneTable[1] += 1; break;
                        case Y: geneTable[2] += 0.6; break;
                        case G: geneTable[3] += 0.6; break;
                        case H: geneTable[4] += 0.6; break;
                        case U: break;
                    }
                }

                // find the dominant one
                byte bestGene = U;
                double bestCropValue = 0.6;
                for (int gene = 0; gene < 5; gene++) {
                    // set new dominant gene if it is stronger
                    if (geneTable[gene] > bestCropValue) {
                        bestGene = genes[gene];
                        bestCropValue = geneTable[gene];
                    }
                    // or add it if its equaly strong
                    else if (geneTable[gene] == bestCropValue) {
                        bestGene |= genes[gene];
                    }
                }

                // set it for the child
                child[i] = bestGene;
            }

            return child;
        }

        /**
         * Converts a gene as a string to gene as byte array
         */
        static byte[] GeneStringToByteArray(string str) {
            byte[] arr = new byte[6];
            for (int i = 0; i < 6; i++) {
                switch (str[i]) {
                    case 'W': arr[i] = W; break;
                    case 'X': arr[i] = X; break;
                    case 'Y': arr[i] = Y; break;
                    case 'G': arr[i] = G; break;
                    case 'H': arr[i] = H; break;
                    case 'U': arr[i] = U; break;
                }
            }

            return arr;
        }

        /**
         * Converts byte array genes to gene string
         */
        static string ByteArrayToGeneString(byte[] arr) {
            string str = "";
            for (int i = 0; i < 6; i++) {
                switch (arr[i]) {
                    case W: str += 'W'; break;
                    case X: str += 'X'; break;
                    case Y: str += 'Y'; break;
                    case G: str += 'G'; break;
                    case H: str += 'H'; break;
                    case U: str += '?'; break;
                }
            }

            return str;
        }
    }
}
