using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASOL
{
    public static class ASOL
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
        /// available opcodes
        /// </summary>
        public static List<string> opCodeList = new List<string>
        {
            "add","nand","lw","sw","beq","jarl","halt","mul",".fill","div","imul","xidiv","and","xor","cmpge","jmae","jmnae","bsr","bsf","jne","pop","push"
        };

        /// <summary>
        /// validation errors
        /// </summary>
        private static List<string> Errors = new List<string>();

        /// <summary>
        /// Stack instanse
        /// </summary>
        private static Stack<int> Stack = new Stack<int>();

        /// <summary>
        /// method for validate and assembly
        /// </summary>
        /// <param name="inputPath">input file path (*.as)</param>
        /// <param name="outputPath">output file path (*.mc)</param>
        public static void Assembly(string inputPath, string outputPath)
        {
            try
            {
                int address = 0;
                
                var labelsNameList = new List<string>();
                var labelsAddressList = new List<int>();
                var label = "";
                var opcode = "";
                var arg0 = "";
                var arg1 = "";
                var arg2 = "";

                int stackCapacity = 0;

                using (var stream = new StreamReader(inputPath))
                {
                    while (!stream.EndOfStream)
                    {
                        if(address > MaxCountOfLabels)
                        {
                            Errors.Add($"Addres '{address}' is bigger than MaxCountOfLabels '{MaxCountOfLabels}'");
                            break;
                        }

                        // parse one line from input file
                        ParseLine(stream.ReadLine(), ref label, ref opcode, ref arg0, ref arg1, ref arg2);

                        if (!opCodeList.Contains(opcode))
                            Errors.Add($"Line: {address}. Opcode '{opcode}' is invalid");

                        // check register fields
                        List<string> testArg0Arg1 = new List<string>
                        {
                            "add","nand","lw", "sw", "beq", "jarl", "mul", "div", "imul", "xidiv", 
                            "and", "xor", "cmpge","jmae", "jmnae", "bsr" ,"bsf"
                        };
                        if (testArg0Arg1.Contains(opcode))
                        {
                            testRegArg(arg0, address);
                            testRegArg(arg1, address);
                        }

                        List<string> testArg2 = new List<string>
                        {
                            "add", "nand", "mul", "div", "imul", "xidiv", "and", "xor", "cmpge"
                        };
                        if (testArg2.Contains(opcode))
                        {
                            testRegArg(arg2, address);
                        }

                        // check adress fields
                        if(opcode == "lw" || opcode == "sw" || opcode == "beq" || opcode == "jmae" || opcode == "jmnae")
                        {
                            testAddrArg(arg2, address);
                        }

                        if(opcode == ".fill" || opcode == "jne")
                        {
                            testAddrArg(arg0, address);
                        }

                        if(opcode == "pop")
                        {
                            if(stackCapacity == 0)
                            {
                                Errors.Add($"Line: {address}. Stack capacity = 0");
                            }
                            else
                            {
                                stackCapacity--;
                            }
                        }

                        if(opcode == "push")
                        {
                            if(stackCapacity == StackCapacity)
                            {
                                Errors.Add($"Line: {address}. Stack capacity = 32");
                            }
                            else
                            {
                                stackCapacity++;
                            }
                        }

                        // check for enough arguments
                        if((opcode != "halt" && opcode != ".fill" && opcode != "pop" && opcode!= "push" && opcode != "bsr" && opcode != "bsf" && opcode != "jne" && arg2 == "")||
                            (opcode != "halt" && opcode != ".fill" && opcode != "pop" && opcode != "push" && opcode != "jne" && arg1 == "") ||
                            (opcode != "halt" && opcode != "pop" && opcode != "push" && arg0 == ""))
                        {
                            Errors.Add($"Line: {address}. Not enough arguments");
                        }

                        if (label != "")
                        {
                            // make sure label starts with letter
                            if (!Char.IsLetter(label[0]))
                            {
                                Errors.Add($"Line: {address}. Label: '{label}' doesnt start with letter");
                            }

                            // make sure label consists only from letters and numbers
                            if(!label.All(el => Char.IsLetterOrDigit(el)))
                            {
                                Errors.Add($"Line: {address}. Label: '{label}' has character other than letters and numbers");
                            }

                            // lool for duplicate label
                            if (labelsNameList.Contains(label))
                            {
                                Errors.Add($"Line: {address}. Duplicate label '{label}' at address: {address}");
                            }

                            if(labelsAddressList.Count() >= MaxCountOfLabels)
                            {
                                Errors.Add($"Line: {address}. Too many labels");
                            }

                            labelsNameList.Add(label);
                            labelsAddressList.Add(address);
                        }

                        address++;
                    }
                }

                if (Errors.Any())
                    throw new Exception();

                // print machine code

                int numLabels = 0;
                int addressField = 0;
                address = 0;
                string outputResult = "";
                using(var input = new StreamReader(inputPath))
                {
                    using(var output = new StreamWriter(outputPath))
                    {
                        while (!input.EndOfStream)
                        {
                            int machineCode = 0;
                            ParseLine(input.ReadLine(), ref label, ref opcode, ref arg0, ref arg1, ref arg2);

                            if(opcode == "add")
                            {
                                machineCode = ((int)Instruction.ADD << 22) | (int.Parse(arg0)<<18) | (int.Parse(arg1) <<14) | int.Parse(arg2);
                            } 
                            else if(opcode == "nand")
                            {
                                machineCode = ((int)Instruction.NAND << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | int.Parse(arg2);
                            }
                            else if (opcode == "mul")
                            {
                                machineCode = ((int)Instruction.MUL << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | int.Parse(arg2);
                            }
                            else if (opcode == "imul")
                            {
                                machineCode = ((int)Instruction.IMUL << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | int.Parse(arg2);
                            }
                            else if (opcode == "mul")
                            {
                                machineCode = ((int)Instruction.MUL << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | int.Parse(arg2);
                            }
                            else if (opcode == "div")
                            {
                                machineCode = ((int)Instruction.DIV << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | int.Parse(arg2);
                            }
                            else if (opcode == "xidiv")
                            {
                                machineCode = ((int)Instruction.XIDIV << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | int.Parse(arg2);
                            }
                            else if (opcode == "and")
                            {
                                machineCode = ((int)Instruction.AND << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | int.Parse(arg2);
                            }
                            else if (opcode == "xor")
                            {
                                machineCode = ((int)Instruction.XOR << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | int.Parse(arg2);
                            }
                            else if (opcode == "cmpge")
                            {
                                machineCode = ((int)Instruction.CMPGE << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | int.Parse(arg2);
                            }
                            else if(opcode == "pop")
                            {
                                machineCode = ((int)Instruction.POP << 22);
                            }
                            else if (opcode == "push")
                            {
                                machineCode = ((int)Instruction.PUSH << 22);
                            }
                            else if (opcode == "bsf")
                            {
                                machineCode = ((int)Instruction.BSF << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14);
                            }
                            else if (opcode == "bsr")
                            {
                                machineCode = ((int)Instruction.CMPGE << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14);
                            }
                            else if (opcode == "halt")
                            {
                                machineCode = ((int)Instruction.HALT << 22);
                            }
                            else if (opcode == ".fill")
                            {
                                if(arg0.All(el => Char.IsLetter(el)))
                                {
                                    machineCode = translateSymbol(labelsNameList, labelsAddressList, numLabels, arg0);
                                }
                                else
                                {
                                    machineCode = int.Parse(arg0);
                                }
                            }
                            else if(opcode == "")
                            {
                                continue;
                            }
                            else if(opcode == "lw" || opcode == "sw" || opcode == "beq"  || opcode == "jmae" || opcode == "jmnae" || opcode == "jne")
                            {
                                if(arg2.All(el => char.IsLetter(el)))
                                {
                                    addressField = translateSymbol(labelsNameList, labelsAddressList, numLabels, arg2);
                                    if(opcode == "beq" || opcode == "jmae" || opcode == "jmnae" || opcode == "jne")
                                    {
                                        addressField = addressField - address - 1;
                                    }
                                }
                                else
                                {
                                    addressField = int.Parse(arg2);
                                }

                                if(address + addressField > MaxCountOfLabels || address + addressField < 0)
                                {
                                    Errors.Add($"offset {addressField} out of range");
                                }

                                // max length of offset must be less then 15 bits
                                addressField = addressField & 0x7FFF;

                                if(opcode == "beq")
                                {
                                    machineCode = ((int)Instruction.BEQ << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | addressField;
                                }
                                else if(opcode == "lw" || opcode == "sw")
                                {
                                    if(opcode == "sw")
                                    {
                                        machineCode = ((int)Instruction.SW << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | addressField;
                                    }
                                    else
                                    {
                                        machineCode = ((int)Instruction.LW << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | addressField;
                                    }
                                }
                                else
                                {
                                    if(opcode == "jmae")
                                    {
                                        machineCode = ((int)Instruction.JMAE << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | addressField;
                                    }
                                    else if(opcode == "jmnae")
                                    {
                                        machineCode = ((int)Instruction.JMNAE << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | addressField;
                                    }
                                    else if(opcode == "jne")
                                    {
                                        machineCode = ((int)Instruction.JNE << 22) | (int.Parse(arg0) << 18) | (int.Parse(arg1) << 14) | addressField;
                                    }
                                }
                            }
                            outputResult += machineCode.ToString() + "\n";
                            address++;
                        }


                        if (Errors.Any())
                            throw new Exception();

                        if(outputResult.Length>1)
                            output.Write(outputResult.Substring(0, outputResult.Length - 1));
                    }
                    
                }

            }
            catch(Exception ex)
            {
                if (!Errors.Any()){
                    Console.WriteLine(ex.Message);
                }
                else
                {
                    foreach(var error in Errors)
                    {
                        Console.WriteLine($"Error! {error}");
                    }
                }
            }
        }

        private static void ParseLine(string stringfy, ref string label, ref string opcode, ref string arg0, ref string arg1, ref string arg2)
        {
            var sublines = stringfy.Split(' ');

            label = sublines.Length > 0 ? sublines[0] : "";
            opcode = sublines.Length > 1 ? sublines[1] : "";
            arg0 = sublines.Length > 2 ? sublines[2] : "";
            arg1 = sublines.Length > 3 ? sublines[3] : "";
            arg2 = sublines.Length > 4 ? sublines[4] : "";
        }

        public static void testRegArg(string arg, int address)
        {
            int argNum;
            bool isRight = int.TryParse(arg, out argNum);

            if (!isRight)
            {
                Errors.Add($"Line{address}. Incorect arg");
            }
            if(argNum < 0 || argNum >= RegNumbers)
            {
                Errors.Add($"Line{address}. Register out of range");
            }
        }

        public static void testAddrArg(string arg, int address)
        {
            int num;
            bool isRight = int.TryParse(arg, out num);
            if (!isRight)
            {
                var isLetterOnly = arg.All(el => char.IsLetter(el));
                if (!isLetterOnly)
                {
                    Errors.Add($"Line: {address}. Bad character in adress field");
                }
            }
        }

        public static int translateSymbol(List<string> labelNameList, List<int> labelAdressList, int numLabels, string symbol)
        {
            int i;
            numLabels = labelNameList.Count();

            for(i = 0; i< numLabels; i++)
            {
                if (symbol == labelNameList[i]) break;
            }

            if(i > numLabels)
            {
                Errors.Add($"Missing label {symbol}");
            }

            return (labelAdressList[i]);
        }
    }
}
