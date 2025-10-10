using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SIDHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            //#
            //## VARIABLE DECLARATIONS
            //#
            mode = "ENCODE";

            var sidbasePath = Directory.GetCurrentDirectory();
            
            int hashTableRawLength;
            
            byte[]
                wholeSidbase,
                sidbaseRawHashTable,
                sidbaseRawStringTable
            ;

            var hashLines = new List<string[]>();
            var lookupLines = new List<string[]>();

            string inputString;




            //#
            //## Search for and load the sidbase.bin
            //#
            if (args != null && args.Length > 0 && args[0].Length > 3 && File.Exists(args[0]))
            {
                sidbasePath = args[0];
            }
            else
            {
                new[] { @"\sidbase.bin", @"\sid\sidbase.bin", @"\sid1\sidbase.bin", @"\..\sidbase.bin" }
                .Any(path =>
                {
                    if (File.Exists(sidbasePath + path))
                    {
                        sidbasePath += path;
                        return true;
                    }
                    else
                        return false;
                });
            }


            // Check the sidbase path before proceeding.
            if (sidbasePath == Directory.GetCurrentDirectory() || !File.Exists(sidbasePath))
            {
                echo ("No valid sidbase.bin in working folder, please provide a valid sidbase path.\nNew path: ");

                // switch to encoder-only mode (why did I implement it this way??)
                if (sidbasePath.ToLower().Remove(2) == "no")
                {
                    sidbasePath = "N/A";
                }
                // Repeatedly ask for a valid path until one's provided
                else {
                    while (!File.Exists(sidbasePath = read().Replace("\"", string.Empty)))
                    {
                        Console.Clear();
                        echo ("Invalid path provided, try again. (or type \"no\" to run in encoder-only mode)");
                        sidbasePath = read().Replace("\"", string.Empty);
                    }
                }
            }



            // Load the string id base in to memory for scanning
            Console.Clear();

            if (sidbasePath == "N/A")
            {
                mode = "Encode"; // I keep changing which is default, so may as well do this here jic.
            }


            echo ("Loading sidbase...");


        retry:
            wholeSidbase = File.ReadAllBytes(sidbasePath);
            if (wholeSidbase.Length < 24) // not that it would make sense for anyone to use an sidbase with a single sid... or be using this random exe off of my pc.
            {
                echo($"Invalid sidbase provided ({nameof(wholeSidbase)}.Length < 24); Please provide an alternate path.");

                sidbasePath = read().Replace("\"", string.Empty);
                    
                Console.Clear();
                goto retry;
            }


            // Read the table length to get the expected size of the hash table (don't really need it anymore)
            var check = BitConverter.ToUInt64(wholeSidbase, 0) * 16;
            if (check >= int.MaxValue)
            {
                Console.Clear();
                echo($"ERROR: Sidbase is too large for 64-bit addresses, blame Microsoft for limiting me to that, then blame me for not bothering to try splitting the sidbases.");
                
                echo($"\n[Press Any Button to Close the Application.]");
                read();
            }

            hashTableRawLength = (int) BitConverter.ToInt64(wholeSidbase, 0) * 16;

            // Initialize the hash/string tables, and read them from the active sidbase
            sidbaseRawHashTable   = ReadBytes(wholeSidbase, 8, (int)hashTableRawLength);
            sidbaseRawStringTable = ReadBytes(wholeSidbase, sidbaseRawHashTable.Length + 8, wholeSidbase.Length - (int) (hashTableRawLength + 8));
            
            


            //=====================================\\
            //--|   Main Input & Display Loop   |--\\
            //=====================================\\
            #region [Main Input & Display Loop]

            while (true)
            {
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
                        echo($"#  SID Encoder  # [little | big -> string]\n\nEncoded Strings:");
                    }
                    else {
                        echo($"#  SID Encoder  # [little | big -> string]\n\n[No Strings Provided]:");
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
                    while (inputString?.Length < 1);


                    // Check for switches/other miscellaneous commands before adding the encoded input if no switches were provided
                    if (new[] { "cls", "clear" }.Contains(inputString.ToLower()))
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
                        if (inputString[1] == '1' || inputString[1] == '`' && mode != null)
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
                else if (mode == "DECODE")
                {
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
                            var hash = BitConverter.GetBytes(long.Parse(inputString, System.Globalization.NumberStyles.HexNumber)).Reverse().ToArray();

                            foreach (var previousLine in lookupLines)
                            {
                                if (previousLine[0] == BitConverter.ToString(hash).Replace("-", string.Empty))
                                {
                                    lookupLines.Remove(previousLine);
                                    break;
                                }
                            }
                            
                        
                            lookupLines.Add(DecodeSIDHash(sidbaseRawHashTable, sidbaseRawStringTable, hashTableRawLength, hash));
                            break;




                        // Handle clear screen command
                        case var _ when new[] { "cls", "clear" }.Contains(inputString.ToLower()):
                            lookupLines.Clear();
                            break;


                        // Handle switches (backtick + option character)
                        case var _ when inputString.Length < 3 && inputString[0] == '`':
                            
                            //## Switch to the Encoder mode
                            if (inputString[1] == '1' || inputString[1] == '`')
                                mode = "ENCODE";


                            //## Remove any invalid / unresolved entries from the lookupList
                            else if (inputString[1] == '?')
                            {
                                for (int i = 0; i < lookupLines.Count; )
                                {
                                    var item = lookupLines[i];
                                    if (item[1] == "UNKNOWN_SID_64" || item[1] == "INVALID_SID_64")
                                    {
                                        lookupLines.Remove(item);
                                    }
                                    else ++i;
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
                            echo($"\r\t\nUnexpected input \"{inputString}\" provided; must be either a 16 character/64-bit string of bytes (whitespace is stripped and ignored), or a switch (backtick + character).");
                            System.Threading.Thread.Sleep(1133);
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
        private static void echo (object item = null)
        {
            Console.WriteLine(item);

            Debug.WriteLineIf(!Console.IsInputRedirected, item);
        }
        private static string read () => Console.ReadLine();
        private static void exit (bool isError = false) => Environment.Exit(isError ? 1 : 0);



        /// <summary>
        /// Get a sub-array of the specified <paramref name="length"/> from a larger <paramref name="array"/> of bytes, starting at the <paramref name="index"/> specified.
        /// </summary>
        /// <param name="array"> The array from which to take the sub-array. </param>
        /// <param name="index"> The start index of the sub-array within <paramref name="array"/>. </param>
        /// <param name="length"> The length of the sub-array. </param>
        /// <returns> Home with milk, unlike your dad. </returns>
        private static byte[] ReadBytes(byte[] array, int index, int length)
        {
            var ret = new byte[length];
            for (; length > 0; length--) // wooo we go backwards 'cause I'm gonna pretend that creating one less variable will actually help
            {
                ret[length - 1] = array[index + (length - 1)];
            }
            return ret;
        }
        


        /// <summary>
        /// Encode an <paramref name="inputString"/> as a 64-bit FNV-1a hash, and return the string representation of said hash in both endians
        /// </summary>
        /// <param name="inputString"> The string to encode. </param>
        /// <returns> A string[] containing the hashed string in both endians. </returns>
        private static string[] EncodeString(string inputString)
        {
            // Hash input string
            var hash = 14695981039346656037ul;
            var prime = 1099511628211ul;
            var inputLen = inputString?.Length ?? 0;
                
            for (int i = 0; i < inputLen; i++)
            {
                hash ^= inputString[i];
                hash *= prime;
            }
            return new[]
            {
                BitConverter.ToUInt64(BitConverter.GetBytes(hash).Reverse().ToArray(), 0).ToString("X").PadLeft(16, '0').PadRight(16, '0'),
                hash.ToString("X").PadLeft(16, '0').PadRight(16, '0')
            };
            
            //return new[] { "INVALID_SID_64", string.Empty };
        }


        
        /// <summary>
        /// Attempt to decode a provided 64-bit FNV-1a hash via a provided lookup file (sidbase.bin)
        /// </summary>
        /// <param name="hashTable"> The loaded hash lookup table. </param>
        /// <param name="stringTable"> The loaded string table. </param>
        /// <param name="fullHashTableLength">  </param>
        /// <param name="bytesToDecode"> The hash to decode, as an array of bytes </param>
        /// <exception cref="IndexOutOfRangeException"> Thrown in the event of an invalid string pointer read from the sidbase after the provided hash is located. </exception>
        private static string[] DecodeSIDHash(byte[] hashTable, byte[] stringTable, int fullHashTableLength, byte[] bytesToDecode)
        {
            var ret = new[]
            {
                BitConverter.ToString(bytesToDecode).Replace("-", string.Empty),
                "UNKNOWN_SID_64"
            };

            if (bytesToDecode.Length == 8)
            {
                ulong
                    currentHash,
                    expectedHash
                ;
                int
                    previousAddress = 0xBADBEEF, // Used for checking whether the hash could not be decoded
                    scanAddress = fullHashTableLength / 2,
                    currentRange = scanAddress
                ;


                expectedHash = BitConverter.ToUInt64(bytesToDecode, 0);

                // check whether or not the chunk can be evenly split; if not, check
                // the odd one out for the expected hash, then exclude it and continue as normal if it isn't a match.
                if (((fullHashTableLength >> 4) & 1) == 1)
                {
                    var checkedHash = BitConverter.ToUInt64(hashTable, fullHashTableLength - 0x10);

                    if (checkedHash == expectedHash)
                    {
                        scanAddress = fullHashTableLength - 0x10;
                        goto readString;
                    }

                    scanAddress = currentRange -= 8;
                }
                

                while (true)
                {
                    if (((scanAddress >> 4) & 1) == 1)
                    {
                        var checkedHash = BitConverter.ToUInt64(hashTable, (int) scanAddress);
                    
                        if (checkedHash == expectedHash)
                        {
                            goto readString;
                        }
                    
                        scanAddress -= 0x10;
                    }
                    if (((currentRange >> 4) & 1) == 1)
                    {
                        currentRange += 0x10;
                    }


                    currentHash = BitConverter.ToUInt64(hashTable, scanAddress);

                    if (expectedHash < currentHash)
                    {
                        scanAddress -= currentRange / 2;
                        currentRange /= 2;
                    }
                    else if (expectedHash > currentHash)
                    {
                        scanAddress += currentRange / 2;
                        currentRange /= 2;
                    }
                    else
                    {
                        break;
                    }
                    


                    // Handle missing sid's. How did I forget about that?
                    if (scanAddress == previousAddress)
                    {
                        return ret;
                    }

                    previousAddress = scanAddress;
                }





                // Read the string pointer
                readString:
                var stringPtr = (int) BitConverter.ToInt64(hashTable, (int) scanAddress + 8); // Get the string pointer for the read hasha, located immediately after said hash
                stringPtr -= (int) fullHashTableLength + 8; // Adjust the string pointer to account for the lookup table being a separate array, and table length being removed
                
                if (stringPtr >= stringTable.Length)
                {
                    throw new IndexOutOfRangeException($"ERROR: Invalid Pointer Read for String Data!\n    str* 0x{stringPtr:X} >= len 0x{hashTable.Length + stringTable.Length + 8:X}.");
                }


                // Parse and add the string to the array
                var stringBuffer = string.Empty;

                while (stringTable[stringPtr] != 0)
                {
                    stringBuffer += Encoding.UTF8.GetString(stringTable, (int)stringPtr++, 1);
                }

                
                ret[1] = stringBuffer;
            }
            else {
                echo($"Invalid SID provided; unexpected length of \"{bytesToDecode?.Length ?? 0}\". Must be 8 bytes.");
                ret[1] = "INVALID_SID_64";
            }

            return ret;
        }




        /// <summary>
        /// Attempt to decode a provided 64-bit FNV-1a hash via a provided lookup file (sidbase.bin)
        /// </summary>
        /// <param name="sidbase"> The loaded lookup table. </param>
        /// <param name="bytesToDecode"> The hash to decode, as an array of bytes </param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"> Thrown in the event of an invalid string pointer read from the sidbase after the provided hash is located. </exception>
        private static string[] Old_DecodeSIDHash(byte[] sidbase, int fullHashTableLength, byte[] bytesToDecode)
        {
            var ret = new string[] { BitConverter.ToString(bytesToDecode).Replace("-", string.Empty), "UNKNOWN_SID_64"};
            
            if (bytesToDecode.Length == 8)
            {
                for (int mainArrayIndex = 0, subArrayIndex = 0; mainArrayIndex < fullHashTableLength; subArrayIndex = 0, mainArrayIndex+=8)
                {
                    if (sidbase[mainArrayIndex] != (byte)bytesToDecode[subArrayIndex])
                    {
                        continue;
                    }


                    // Scan for the rest of the bytes
                    while ((subArrayIndex < 8 && mainArrayIndex < (uint) sidbase.Length) && sidbase[mainArrayIndex + subArrayIndex] == (byte)bytesToDecode[subArrayIndex]) // while (subArrayIndex < 8 && sidbase[mainArrayIndex++] == (byte)bytesToDecode[subArrayIndex++]) how the fuck does this behave differently?? I need sleep.
                    {
                        subArrayIndex++;
                    }

                    // continue if there was only a partial match
                    if (subArrayIndex != 8)
                    {
                        continue;
                    }
                




                    // Read the string pointer
                    var stringPtr = BitConverter.ToInt64(sidbase, (int)(mainArrayIndex + 8));
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
    }
}
