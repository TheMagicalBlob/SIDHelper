using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace SIDHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            //#
            //## Search for and load the sidbase.bin
            //#
            mode = "ENCODE";
            byte[] sidbase;
            var sidbasePath = Directory.GetCurrentDirectory();

            if (File.Exists(sidbasePath + @"\sidbase.bin"))
            {
                sidbasePath += @"\sidbase.bin";
            }
            else if (File.Exists(Directory.GetCurrentDirectory() + @"\sid\sidbase.bin"))
            {
                sidbasePath += @"\sid\sidbase.bin";
            }
            else if (File.Exists(Directory.GetCurrentDirectory() + @"\sid1\sidbase.bin"))
            {
                sidbasePath += @"\sid1\sidbase.bin";
            }


            if (sidbasePath == string.Empty || !File.Exists(sidbasePath))
            {
                echo ("No valid sidbase.bin in working folder, please provide a valid sidbase path.");
                sidbasePath = read().Replace("\"", string.Empty);
                
                if (sidbasePath.ToLower().Remove(2) == "no")
                {
                    sidbasePath = string.Empty;
                }
                // Repeatedly ask for a valid path until one's provided
                else {
                    while (!File.Exists(sidbasePath))
                    {
                        Console.Clear();
                        echo ("Invalid path provided, try again. (or type \"no\" to run in encoder-only mode)");
                        sidbasePath = read().Replace("\"", string.Empty);

                        
                        if (sidbasePath.ToLower().Remove(2) == "no")
                        {
                            sidbasePath = string.Empty;
                            mode = null;
                            break;
                        }
                    }
                }
            }




            // Load the string id base in to memory for quicker scanning
            Console.Clear();
            echo ("Loading sidbase...");
            sidbase = File.ReadAllBytes(sidbasePath);

            if (sidbase == null || sidbase.Length < 24) // not that it would make sense for anyone to use an sidbase with a single sid... or be using this random exe off of my pc.
            {
                echo("Invalid sidbase provided; Please provide an alternate path.");
                read().Replace("\"", string.Empty);
                Console.Clear();
                return;
            }
            Console.Clear();

            string inputString;
            var hashLines = new List<string[]>();
            var lookupLines = new List<string[]>();
            var sidbaseLength = BitConverter.ToInt64(sidbase, 0) * 16;


            //=====================================\\
            //--|   Main Input & Display Loop   |--\\
            //=====================================\\
            #region [Main Input & Display Loop]
            while (true) {
                start:
                Console.Clear();



                //#
                //## Encode provided strings
                //#
                if (mode == "ENCODE")
                {
                    // Update console display
                    if (hashLines.Count > 0)
                    {
                        echo($"#   SID Encoder   # [little | big -> string]\n\nEncoded Strings:");
                    }
                    else {
                        echo($"# SID Encoder #     [little | big -> string]\n\n[No Strings Provided]:");
                    }

                    foreach (var item in hashLines)
                    {
                        echo($"{item[0]} -> {item[1]}");
                    }
                    if (hashLines?.Count > 0)
                        echo(null);



                    // Wait for next input
                    do {
                        inputString = read();
                    }
                    while (inputString?.Length <1);

                    // Check for switches/other miscellaneous commands before adding the encoded input if no switches were provided
                    if (new[] { "cls", "clear" }.Contains(inputString))
                    {
                        Console.Write($"possible clear command \"{inputString}\" provided\nClear Screen and encoded id's?\n[y/n]: ");
                        while (true)
                        {
                            var key = Console.ReadKey().Key;
                            if (key == ConsoleKey.Y)
                            {
                                hashLines.Clear();
                                goto start;
                            }
                            else if (key == ConsoleKey.N)
                            {
                                break;
                            }

                        }
                    }

                    // Handle switches (backtick + option character)
                    else if (inputString[0] == '`' && inputString.Length == 2)
                    {
                        //## Switch to the Decoder mode
                        if (inputString[1] == '1' && mode != null)
                        {
                            mode = "DECODE";
                        }
                        
                        //## Remove the last item in the list
                        else if (inputString[1] == '!')
                        {
                            hashLines.RemoveAt(hashLines.Count - 1);
                        }
                        
                        //## Clear lookupLines Contents & refresh display
                        else if (inputString.ToLower()[1] == 'c')
                        {
                            hashLines.Clear();
                        }
                        continue;
                    }



                    // Append the encoded version of the provided string to the hashLines list.
                    hashLines.Add(new[] { $"{EncodeString(inputString)[0]}  |  {EncodeString(inputString)[1]}", inputString });
                }
                



                //#
                //## Decode provided hashes
                //#
                else if (mode == "DECODE") {
                    echo("# SID Decoder # [FNV-1a, 64-bit]\n");
                    
                    // Update console display
                    if (lookupLines.Count > 0)
                    {
                        echo($"Decoded SIDs:");
                    }
                    else {
                        echo($"[No SIDs Provided]");
                    }


                    foreach (var item in lookupLines)
                    {
                        echo($"{item[0]} -> {item[1]}");
                    }
                    if (lookupLines?.Count > 0)
                        echo(null);



                    // Wait for / read the next input string
                    switch (inputString = read()?.Replace(" ", string.Empty))
                    {
                        // Invalid / Empty Inputs
                        case var _ when inputString == null || inputString?.Length == 0:
                            break;


                        // Decode the provided hash
                        case var _ when inputString.Length == 16:
                            var hash = BitConverter.GetBytes(long.Parse(inputString, System.Globalization.NumberStyles.HexNumber)).Reverse().ToArray();;

                            foreach (var previousLine in lookupLines)
                            {
                                if (previousLine[0] == BitConverter.ToString(hash).Replace("-", string.Empty))
                                {
                                    lookupLines.Remove(previousLine);
                                    break;
                                }
                            }
                        
                            lookupLines.Add(DecodeSIDHash(sidbase, sidbaseLength, hash));
                            break;

                        case var _ when new[] { "cls", "clear" }.Contains(inputString.ToLower()):
                            lookupLines.Clear();
                            break;


                        // Handle switches (backtick + option character)
                        case var _ when inputString.Length < 3 && inputString[0] == '`':
                            
                            //## Switch to the Encoder mode
                            if (inputString[1] == '1' && mode != null)
                                mode = "ENCODE";


                            //## Remove any invalid / unresolved entries from the lookupList
                            else if (inputString[1] == '?')
                            {
                                foreach (var item in lookupLines)
                                {
                                    if (item[1] == "UNKNOWN_SID_64" || item[1] == "INVALID_SID_64")
                                    {
                                        lookupLines.Remove(item);
                                    }
                                }
                            }


                            //## Remove the last item in the list
                            else if (inputString[1] == '!')
                            {
                                lookupLines.RemoveAt(lookupLines.Count - 1);
                            }

                            //## Clear lookupLines Contents & refresh display
                            else if (inputString.ToLower()[1] == 'c')
                            {
                                lookupLines.Clear();
                            }
                            break;



                        default:
                            echo($"\rUnexpected input \"{inputString}\" provided; must be either a 16 character/64-bit string of bytes (whitespace is stripped and ignored), or a switch (backtick + character).");
                            read();
                            break;
                    }
                }
                else {
                    echo ($"unexpected mode \"{mode ?? "null"}\" provided- switching to encoder (fix your fucking code, dipshit)");
                }
            }
            #endregion
        }



        //#
        //## Variable Declarations
        //#
        private static string mode;


        
        
        //#
        //## Function Declarations
        //#

        /// <summary>
        /// Echo the string representation of a provided object to the standard output, followed by a newline character
        /// (or only a newline if not parameters are provided)
        /// </summary>
        /// <param name="item"> The object to output the string representation of. </param>
        private static void echo (object item = null) => Console.WriteLine(item);
        private static string read () => Console.ReadLine();


        /// <summary>
        /// Attempt to decode a provided 64-bit FNV-1a hash via a provided lookup file (sidbase.bin)
        /// </summary>
        /// <param name="sidbase"> The loaded lookup table. </param>
        /// <param name="bytesToDecode"> The hash to decode, as an array of bytes </param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"> Thrown in the event of an invalid string pointer read from the sidbase after the provided hash is located. </exception>
        private static string[] DecodeSIDHash(byte[] sidbase, long sidLength, byte[] bytesToDecode)
        {
            var ret = new string[] { BitConverter.ToString(bytesToDecode).Replace("-", string.Empty), "UNKNOWN_SID_64"};
            
            if (bytesToDecode.Length == 8)
            {
                for (long mainArrayIndex = 0, subArrayIndex = 0; mainArrayIndex < sidLength; subArrayIndex = 0, mainArrayIndex+=8)
                {
                    if (sidbase[mainArrayIndex] != (byte)bytesToDecode[subArrayIndex])
                    {
                        continue;
                    }


                    // Scan for the rest of the bytes
                    while ((subArrayIndex < 8 && mainArrayIndex < sidbase.Length) && sidbase[mainArrayIndex + subArrayIndex] == (byte)bytesToDecode[subArrayIndex]) // while (subArrayIndex < 8 && sidbase[mainArrayIndex++] == (byte)bytesToDecode[subArrayIndex++]) how the fuck does this behave differently?? I need sleep.
                    {
                        subArrayIndex++;
                    }

                    // continue if there was only a partial match
                    if (subArrayIndex != 8)
                    {
                        continue;
                    }
                

                    // Read the string pointer
                    var stringPtr = BitConverter.ToInt64(sidbase, (int)(mainArrayIndex + subArrayIndex));
                    if (stringPtr >= sidbase.Length)
                    {
                        throw new IndexOutOfRangeException($"ERROR: Invalid Pointer Read for String Data!\n    str* 0x{stringPtr:X} >= len 0x{sidbase.Length:X}.");
                    }


                    // Parse and add the string to the array
                    var stringBuffer = string.Empty;

                    while (sidbase[stringPtr] != 0)
                    {
                        stringBuffer += Encoding.UTF8.GetString(sidbase, (int)stringPtr++, 1);
                    }

                
                    ret[1] = stringBuffer;
                }
            }
            else {
                echo($"Invalid SID provided; unexpected length of \"{bytesToDecode?.Length ?? 0}\". Must be 8 bytes.");
                ret[1] = "INVALID_SID_64";
            }

            return ret;
        }

        /// <summary>
        /// Attempt to decode the string representation of a provided 64-bit FNV-1a hash via a provided lookup file (sidbase.bin)
        /// </summary>
        /// <param name="sidbase"> The loaded lookup table. </param>
        /// <param name="sidLength">  </param>
        /// <param name="stringToDecode"> The big-endian string representation of a 64-bit FNV 1a hash to decode. </param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"> Thrown in the event of an invalid string pointer read from the sidbase after the provided hash is located. </exception>
        private static string[] DecodeSIDHash(byte[] sidbase, long sidLength, string stringToDecode) => DecodeSIDHash(sidbase, sidLength, BitConverter.GetBytes(long.Parse(stringToDecode, System.Globalization.NumberStyles.HexNumber)).Reverse().ToArray());


        /// <summary>
        /// Encode an <paramref name="inputString"/> as a 64-bit FNV-1a hash, and return the string representation of said hash in both endians
        /// </summary>
        /// <param name="inputString"> The string to encode. </param>
        /// <returns> A string[] containing the hashed string in both endians. </returns>
        private static string[] EncodeString(string inputString)
        {
            if (inputString?.Length > 0)
            {
                // Hash input string
                ulong hash = 14695981039346656037;
                ulong prime = 1099511628211;
                foreach (char character in inputString)
                {
                    hash ^= character;
                    hash *= prime;
                }
                return new[] { BitConverter.ToInt64(BitConverter.GetBytes(hash).Reverse().ToArray(), 0).ToString("X").PadLeft(16, '0').PadRight(16, '0'), hash.ToString("X").PadLeft(16, '0').PadRight(16, '0') };
            }

            return new[] { "INVALID_SID_64", string.Empty };
        }
    }
}
