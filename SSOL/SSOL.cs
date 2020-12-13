using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSOL
{
    public static class SSOL
    {
        /// <summary>
        /// max program size
        /// </summary>
        public static int MaxProgramSize { get; } = 65536;

        /// <summary>
        /// number of registers
        /// </summary>
        public static int RegNumbers { get; } = 16;

        /// <summary>
        /// length of bus
        /// </summary>
        public static int BusLength { get; } = 32;

        /// <summary>
        /// max stack capacity
        /// </summary>
        public static int StackCapacity { get; } = 32;

        /// <summary>
        /// Max cout of labels
        /// </summary>
        public static int MaxCountOfLabels { get; } = (MaxProgramSize / BusLength) * 8;


        /// <summary>
        /// available instructions
        /// </summary>
        public enum Instruction
        {
            ADD, NAND, LW, SW, BEQ, JARL, HALT, MUL, DIV, IMUL, XIDIV, AND, XOR, CMPGE, JMAE, JMNAE,
            BSR, BSF, JNE, POP, PUSH
        }

        /// <summary>
        /// Memmory
        /// </summary>
        private static List<int> Memory { get; set; } = new List<int>();

        /// <summary>
        /// Registers
        /// </summary>
        private static List<int> Registers { get; set; } = new List<int>();

        /// <summary>
        /// Stack
        /// </summary>
        private static Stack<int> Stack { get; set; } = new Stack<int>();

        /// <summary>
        /// Zero flag
        /// </summary>
        private static bool ZeroFlag { get; set; }

        /// <summary>
        /// Programm counter
        /// </summary>
        private static int PC { get; set; }

        /// <summary>
        /// Count of iterations
        /// </summary>
        private static int CoutOfInstructions { get; set; }

        public static void Simulate(string inputPath, string outputPath)
        {
            try
            {
                // initialize
                CoutOfInstructions = 0;
                PC = 0;
                ZeroFlag = false;
                for (int i = 0; i < RegNumbers; i++)
                    Registers.Add(0);

                using (var input = new StreamReader(inputPath))
                {
                    while (!input.EndOfStream)
                    {
                        Memory.Add(int.Parse(input.ReadLine()));
                    }
                }

                // simulate
                using (var output = new StreamWriter(outputPath))
                {
                    //print memory before all
                    PrintMemory(output);
                    PrintState(output);

                    for(; ; CoutOfInstructions++)
                    {
                        var opcode = (Instruction)((Memory[PC] >> 23) & 31);
                        if(opcode == Instruction.HALT)
                        {
                            PrintState(output, true);
                            break;
                        }

                        var arg0 = (Memory[PC] >> 19) & 15;
                        var arg1 = (Memory[PC] >> 15) & 15;
                        var arg2 = Memory[PC] & 15;
                        var offset = 0; 
                        if((Memory[PC] & 0x4000) == 0x4000)
                        {
                            offset = -32768 | (Memory[PC] & 0x7FFF);
                        }
                        else
                        {
                            offset = Memory[PC] & 0x7FFF;
                        }

                        switch (opcode)
                        {
                            case Instruction.ADD:
                                Registers[arg2] = Registers[arg0] + Registers[arg1];
                                PC++;
                                break;
                            case Instruction.NAND:
                                Registers[arg2] = ~(Registers[arg0] & Registers[arg1]);
                                PC++;
                                break;
                            case Instruction.LW:
                                Registers[arg1] = Memory[arg0 + offset];
                                PC++;
                                break;
                            case Instruction.SW:
                                Memory[arg0 + offset] = Registers[arg1];
                                PC++;
                                break;
                            case Instruction.BEQ:
                                PC = Registers[arg0] == Registers[arg1] ? PC + offset + 1: PC + 1;
                                break;
                            case Instruction.JARL:
                                if (Registers[arg0] == Registers[arg1])
                                {
                                    PC++;
                                    Registers[arg0] = PC;
                                }
                                else
                                {
                                    Registers[arg1] = PC + 1;
                                    PC = Registers[arg0];
                                }
                                break;
                            case Instruction.MUL:
                                {
                                    Int64 temp1 = Registers[arg0] & 0xFFFFFFFF;
                                    Int64 temp2 = Registers[arg1] & 0xFFFFFFFF;
                                    Registers[arg2] = (int)(temp1 * temp2);
                                }
                                PC++;
                                break;
                            case Instruction.DIV:
                                {
                                    Int64 temp1 = Registers[arg0] & 0xFFFFFFFF;
                                    Int64 temp2 = Registers[arg1] & 0xFFFFFFFF;
                                    Registers[arg2] = (int)(temp1 / temp2);
                                }
                                PC++;
                                break;
                            case Instruction.IMUL:
                                Registers[arg2] = Registers[arg0] * Registers[arg1];
                                PC++;
                                break;
                            case Instruction.XIDIV:
                                Registers[arg2] = Registers[arg0] / Registers[arg1];
                                var temp = Registers[arg0];
                                Registers[arg0] = Registers[arg1];
                                Registers[arg1] = temp;
                                PC++;
                                break;
                            case Instruction.AND:
                                Registers[arg2] = Registers[arg0] & Registers[arg1];
                                PC++;
                                break;
                            case Instruction.XOR:
                                Registers[arg2] = Registers[arg0] ^ Registers[arg1];
                                PC++;
                                break;
                            case Instruction.CMPGE:
                                Registers[arg2] = Registers[arg0] >= Registers[arg1] ? 1 : 0;
                                PC++;
                                break;
                            case Instruction.JMAE:
                                PC = Registers[arg0] >= Registers[arg1] ? PC + offset + 1 : PC + 1;
                                break;
                            case Instruction.JMNAE:
                                PC = Registers[arg0] < Registers[arg1] ? PC + offset + 1: PC + 1;
                                break;
                            case Instruction.BSR:
                                {
                                    var str = Convert.ToString(Registers[arg0], 2);
                                    var substr = "";
                                    for (int i = 0; i < 32 - str.Length; i++)
                                        substr += "0";
                                    str = substr + str;
                                    var index = -1;
                                    ZeroFlag = false;
                                    for (int i = 0; i < str.Length; i++)
                                    {
                                        if (str[i] == '1')
                                        {
                                            index = i;
                                            ZeroFlag = true;
                                            break;
                                        }
                                    }
                                    Registers[arg1] = index;
                                }
                                PC++;
                                break;
                            case Instruction.BSF:
                                {
                                    var str = Convert.ToString(Registers[arg0], 2);
                                    var substr = "";
                                    for (int i = 0; i < 32 - str.Length; i++)
                                        substr += "0";
                                    str = substr + str;
                                    var index = -1;
                                    ZeroFlag = false;
                                    for (int i = str.Length - 1; i >= 0; i--)
                                    {
                                        if (str[i] == '1')
                                        {
                                            index = 31 - i;
                                            ZeroFlag = true;
                                            break;
                                        }
                                    }
                                    Registers[arg1] = index;
                                }
                                PC++;
                                break;
                            case Instruction.JNE:
                                PC = ZeroFlag ? PC + offset + 1 : PC + 1;
                                break;
                            case Instruction.POP:
                                Registers[1] = Stack.Pop();
                                PC++;
                                break;
                            case Instruction.PUSH:
                                Stack.Push(Registers[1]);
                                PC++;
                                break;
                            default:
                                break;
                        }
                        PrintState(output);
                    }
                }

                Console.WriteLine("Done...");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                try
                {
                    var directory = Directory.GetCurrentDirectory();
                    File.Delete(directory + "\\" + outputPath);
                }
                catch { }
            }
        }

        private static void PrintMemory(StreamWriter output)
        {
            string text = "";
            for (var i = 0; i < Memory.Count(); i++)
                text += $"memory[ {i} ] = {Memory[i]}\n";
            output.WriteLine(text+ "\n");
        }

        private static void PrintState(StreamWriter output, bool isHalt = false)
        {
            string text = $"@@@\nstate:\n\tpc {PC}\n\tmemory:\n";
            for (var i = 0; i < Memory.Count(); i++)
                text += $"\t\tmem[ {i} ] {Memory[i]}\n";
            text += "\tregisters:\n";
            for (var i = 0; i < Registers.Count(); i++)
                text += $"\t\treg[ {i} ] {Registers[i]}\n";
            text += $"\tstack:\n";
            for (var i = 0; i < Stack.Count(); i++)
                text += $"\t\tst[ {i} ] {Stack.ElementAt(i)}\n";
            text += $"\tzero flag: {ZeroFlag}\n";
            text += "end state";
            if (isHalt)
            {
                text += $"\nmachine halted\ntotal of {CoutOfInstructions+1} executed\nfinal state of machine:";
                output.WriteLine(text + "\n");
                PC++;
                PrintState(output);
                return;
            }
            output.WriteLine(text + "\n");
        }
    }

}
