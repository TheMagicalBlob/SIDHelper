using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace lookupSID
{
    class Program
    {
        private static void echo (object str) => Console.WriteLine(str);
        private static void read () => Console.ReadLine();

        private static void DisplayUpdate(List<object[]> items)
        {
            Console.Clear();

            foreach (var item in items)
            {
                echo($"{item[0]}: {item[1]}");
            }
            echo(null);
        }

        private static byte[] getBytes(string stringRepresentation)
        {
            return BitConverter.GetBytes(long.Parse(stringRepresentation, System.Globalization.NumberStyles.HexNumber)).Reverse().ToArray();
        }

        private static object[] DecodeArray(byte[] sidbase, byte[] bytesToDecode)
        {
            var ret = new object[] { BitConverter.ToString(bytesToDecode).Replace("-", string.Empty), (string) "UNKNOWN_SID_64"};
            
            for (long mainArrayIndex = 0, subArrayIndex = 0; mainArrayIndex < sidbase.Length; subArrayIndex = 0, mainArrayIndex++)
            {
                if (sidbase[mainArrayIndex] != (byte)bytesToDecode[subArrayIndex])
                {
                    continue;
                }


                // Scan for the rest of the bytes
                while (subArrayIndex < 8 && sidbase[mainArrayIndex] == (byte)bytesToDecode[subArrayIndex]) // while (subArrayIndex < 8 && sidbase[mainArrayIndex++] == (byte)bytesToDecode[subArrayIndex++]) how the fuck does this behave differently?? I need sleep.
                {
                    mainArrayIndex++;
                    subArrayIndex++;
                }

                // continue if there was only a partial match
                if (subArrayIndex != 8)
                {
                    continue;
                }


                // Read the string pointer
                mainArrayIndex = BitConverter.ToInt64(sidbase, (int)mainArrayIndex);

                // Parse and add the string to the array
                var stringBuffer = string.Empty;
                while (sidbase[mainArrayIndex] != 0)
                {
                    stringBuffer += Encoding.UTF8.GetString(sidbase, (int)mainArrayIndex++, 1);
                }

                ret[1] = stringBuffer;
                break;
            }


            return ret;
        }


        static void Main(string[] args)
        {
            var sidbasePath = Directory.GetCurrentDirectory();
            byte[] sidbase;


            if (File.Exists(sidbasePath + @"\sidbase.bin"))
            {
                sidbasePath += @"\sidbase.bin";
            }
            else if (File.Exists(Directory.GetCurrentDirectory() + @"\sid\sidbase.bin"))
            {
                sidbasePath += @"\sid\sidbase.bin";
            }


            if (sidbasePath == string.Empty || !File.Exists(sidbasePath))
            {
                echo ("No valid sidbase.bin in working folder, please provide a valid sidbase path.");
                sidbasePath = Console.ReadLine().Replace("\"", string.Empty);
                
                while (!File.Exists(sidbasePath))
                {
                    Console.Clear();
                    echo ("Invalid path provided, try again.");
                    sidbasePath = Console.ReadLine().Replace("\"", string.Empty);
                }
            }




            // Load the string id base in to memory for quicker scanning
            sidbase = File.ReadAllBytes(sidbasePath);

            if (sidbase == null || sidbase.Length < 24) // not that it would make sense for anyone to use an sidbase with a single sid... or be using this random exe off of my pc.
            {
                echo ("Invalid sidbase provided");
            }

            var lines = new List<object[]>();

            string line;
            while (true)
            {
                DisplayUpdate(lines);


                switch (line = Console.ReadLine().Replace(" ", string.Empty))
                {
                    case var _ when line.Length == 16:
                        lines.Add(DecodeArray(sidbase, getBytes(line)));
                        break;


                    default:
                        Console.WriteLine(line + $" {line.Length} != 8");
                        break;
                }
            }
        }
    }
}
