using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSOL
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() < 2 || string.IsNullOrWhiteSpace(args[0])
                || string.IsNullOrWhiteSpace(args[1]) || !args[0].Substring(args[0].Length - 3).Equals(".mc")
                || !args[1].Substring(args[1].Length - 4).Equals(".txt"))
            {
                Console.WriteLine("Error! Incorrect file names!");
                Console.ReadLine();
                return;
            }

            var filesInCurrentDirectory = Directory.GetFiles(Directory.GetCurrentDirectory());
            if (!filesInCurrentDirectory.Any(el => el.Equals($"{Directory.GetCurrentDirectory()}\\{args[0]}")))
            {
                Console.WriteLine($"Error! The file {args[0]} is not in the current directory!");
                Console.ReadLine();
                return;
            }

            SSOL.Simulate(args[0], args[1]);


        }
    }
}
