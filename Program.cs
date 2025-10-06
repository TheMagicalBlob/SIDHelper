using System;
using System.Collections.Generic;
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
            
            
            ulong hashTableRawLength;
            
            byte[]
                rawSidbase,
                sidbaseHashTable,
                sidbaseRawStringTable
            ;
            


            //#
            //## Search for and load the sidbase.bin
            //#
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


            if (sidbasePath == Directory.GetCurrentDirectory() || !File.Exists(sidbasePath))
            {
                echo ("No valid sidbase.bin in working folder, please provide a valid sidbase path.\nNew path: ");
                
                //if (sidbasePath.ToLower().Remove(2) == "no") // switch to encoder-only mode (why did I implement it this way??)
                //{
                //    sidbasePath = string.Empty;
                //}
                // Repeatedly ask for a valid path until one's provided
                while (!File.Exists(sidbasePath = read().Replace("\"", string.Empty)))
                {
                    Console.Clear();
                    echo ("Invalid path provided, try again.");
                    //echo ("Invalid path provided, try again. (or type \"no\" to run in encoder-only mode)");
                    sidbasePath = read().Replace("\"", string.Empty);
                }
            }




            // Load the string id base in to memory for scanning
            Console.Clear();
            echo ("Loading sidbase...");

        retry:
            rawSidbase = File.ReadAllBytes(sidbasePath);
            if (rawSidbase.Length < 24) // not that it would make sense for anyone to use an sidbase with a single sid... or be using this random exe off of my pc.
            {
                echo($"Invalid sidbase provided ({nameof(rawSidbase)}.Length < 24); Please provide an alternate path.");

                sidbasePath = read().Replace("\"", string.Empty);
                    
                Console.Clear();
                goto retry;
            }

            // Read the table length to get the expected size of the hash table (don't really need it anymore)
            hashTableRawLength = BitConverter.ToUInt64(rawSidbase, 0) * 16;

            // Initialize the hash/string tables, and read them from the active sidbase
            //sidbaseHashTable = ReadBytes(rawSidbase, 8, (int) hashTableRawLength);
            //sidbaseRawStringTable = ReadBytes(rawSidbase, sidbaseHashTable.Length + 8, rawSidbase.Length - (int) (hashTableRawLength + 8));
            Buffer.BlockCopy(rawSidbase, 8, sidbaseHashTable = new byte[hashTableRawLength], 0, (int) hashTableRawLength); //! these conversions could easily be an issue...
            Buffer.BlockCopy(rawSidbase, sidbaseHashTable.Length + 8, sidbaseRawStringTable = new byte[rawSidbase.Length - (int) (hashTableRawLength + 8)], 0, (int) rawSidbase.Length - (int) hashTableRawLength - 8);

            Console.Clear();

            string inputString;
            var hashLines = new List<string[]>();
            var lookupLines = new List<string[]>();




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
                            
                        
                            lookupLines.Add(New_DecodeSIDHash(sidbaseHashTable, sidbaseRawStringTable, hashTableRawLength, hash));
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
        private static void echo (object item = null) => Console.WriteLine(item);
        private static string read () => Console.ReadLine();
        private static void exit (bool isError = false) => Environment.Exit(isError ? 1 : 0);

        private static byte[] ReadBytes(byte[] array, int index, int length)
        {
            var ret = new byte[length];
            for (int i = 0; i < length; i++)
            {
                ret[i] = array[index + i];
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
            if (inputString?.Length > 0)
            {
                // Hash input string
                var hash = 14695981039346656037ul;
                var prime = 1099511628211ul;
                var inputLen = inputString.Length;
                
                for (int i = 0; i < inputLen; i++)
                {
                    hash ^= inputString[i];
                    hash *= prime;
                }
                return new[] { BitConverter.ToInt64(BitConverter.GetBytes(hash).Reverse().ToArray(), 0).ToString("X").PadLeft(16, '0').PadRight(16, '0'), hash.ToString("X").PadLeft(16, '0').PadRight(16, '0') };
            }

            return new[] { "INVALID_SID_64", string.Empty };
        }

        
        /// <summary>
        /// Attempt to decode a provided 64-bit FNV-1a hash via a provided lookup file (sidbase.bin)
        /// </summary>
        /// <param name="hashTable"> The loaded hash lookup table. </param>
        /// <param name="stringTable"> The loaded string table. </param>
        /// <param name="hashTableRawLength">  </param>
        /// <param name="bytesToDecode"> The hash to decode, as an array of bytes </param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"> Thrown in the event of an invalid string pointer read from the sidbase after the provided hash is located. </exception>
        private static string[] New_DecodeSIDHash(byte[] hashTable, byte[] stringTable, ulong hashTableRawLength, byte[] bytesToDecode)
        {
            #if DEBUG
            bool addressError(ulong scanAddress)
            {
                decimal chk;
                return scanAddress > hashTableRawLength - 0x10 || scanAddress < 0 || (chk = ((decimal) scanAddress) / 16) != decimal.Round(chk);
            }
            #endif

            
            var ret = new[]
            {
                BitConverter.ToString(bytesToDecode).Replace("-", string.Empty),
                "UNKNOWN_SID_64"
            };

            if (bytesToDecode.Length == 8)
            {
                var expectedHash = BitConverter.ToUInt64(bytesToDecode, 0);
                
                ulong
                    currentHash,
                    scanAddress,
                    currentRange
                ;

                scanAddress = currentRange = hashTableRawLength / 2;

                // check whether or not the chunk can be evenly split; if not, check
                // the odd one out for the expected hash, then exclude it and continue as normal if it isn't a match.
                if (((hashTableRawLength >> 4) & 1) == 1)
                {
                    scanAddress = currentRange -= 8;

                    var checkedHash = BitConverter.ToUInt64(hashTable, (int) hashTableRawLength - 16);
                    if (checkedHash == expectedHash)
                    {
                        scanAddress = hashTableRawLength;
                        goto readString;
                    }
                }

                while (true)
                {
                    // check for uneven split again
                    if (((scanAddress >> 4) & 1) == 1)
                    {
                        scanAddress -= 0x10;

                        var checkedHash = BitConverter.ToUInt64(hashTable, (int) scanAddress);
                        if (checkedHash == expectedHash)
                        {
                            break;
                        }
                    } 
                    if (((currentRange >> 4) & 1) == 1)
                    {
                        currentRange -= 0x10;
                    }


                    #if DEBUG
                    // Make sure we haven't reached the end of the collection (or somehow gone negative),
                    // as well as ensure the offset's still aligned (to the best of my abilities at least- without just clunkily reading it as a string
                    if (addressError(scanAddress))
                    {
                        echo($"\nFuck.\n    {nameof(currentRange)}: 0x{currentRange:X}\n    {nameof(scanAddress)}: 0x{scanAddress:X}\n    {nameof(hashTableRawLength)}: 0x{hashTableRawLength:X}");
                        read();
                        return ret;
                    }
                    #endif
                    

                    
                    currentHash = BitConverter.ToUInt64(hashTable, (int)scanAddress);

                    if (expectedHash < currentHash)
                    {
                        scanAddress -= currentRange / 2;
                        currentRange /= 2;
                    }
                    else if (expectedHash > currentHash)
                    {
                        scanAddress += currentRange / 2;
                    }
                    else
                        break;
                }




                // Read the string pointer
                readString:
                var stringPtr = (int) BitConverter.ToInt64(hashTable, (int)(scanAddress + 8)) - ((uint) hashTableRawLength + 8);

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



        /*// private static string[] Original_DecodeSIDHash(byte[] sidbase, long sidLength, byte[] bytesToDecode)
        /// <summary>
        /// Attempt to decode a provided 64-bit FNV-1a hash via a provided lookup file (sidbase.bin)
        /// </summary>
        /// <param name="sidbase"> The loaded lookup table. </param>
        /// <param name="bytesToDecode"> The hash to decode, as an array of bytes </param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"> Thrown in the event of an invalid string pointer read from the sidbase after the provided hash is located. </exception>
        private static string[] Original_DecodeSIDHash(byte[] sidbase, long sidLength, byte[] bytesToDecode)
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
        */
    }
}
