using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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
            #if DEBUG
            var firstRun = true;
            #endif

            var sidbasePath = Directory.GetCurrentDirectory();
            
            byte[]
                wholeSidbase,
                sidbaseRawHashTable,
                sidbaseRawStringTable
            ;
            ulong hashTableRawLength;
            

            string inputString;
            var hashLines = new List<string[]>();
            var lookupLines = new List<string[]>();


            /*
            File.WriteAllLines(@"C:\Users\blob\Dev\repos\SIDHelper\bin\re-encoded.txt", File.ReadAllLines(@"C:\Users\blob\Dev\repos\SIDHelper\bin\encoded.txt").Distinct().Where(item =>
            {
                if (EncodeString(item.Substring(item.IndexOf(':') + 1))[1] != item.Remove(item.IndexOf(':')).PadLeft(16, '0'))
                {
                    //echo($"Entry contains invalid hash for string \"{item.Substring(item.IndexOf(':') + 1)}\".");
                    //echo($"String encodes as {EncodeString(item.Substring(item.IndexOf(':') + 1))[1]}, but was listed with {item.Remove(item.IndexOf(':')).PadLeft(16, '0')}\n");
                    return false;
                }
                else return true;
            
            }).ToArray());
            exit();
            */


            #if false
            {
                var testSplit = File.ReadAllLines(@"C:\Users\blob\Dev\repos\SIDHelper\bin\encoded.txt");
                var testSplit_hashes = new string[testSplit.Length];
                var testSplit_strings = new string[testSplit.Length];

                for (int i = 0; i < testSplit_hashes.Length; i++)
                {
                    testSplit_hashes[i] = testSplit[i].Remove(testSplit[i].IndexOf(':'));
                    testSplit_strings[i] = testSplit[i].Substring(testSplit[i].IndexOf(':') + 1);
                }

                File.WriteAllLines($@"{Directory.GetCurrentDirectory()}\..\hashes.txt", testSplit_hashes);
                File.WriteAllLines($@"{Directory.GetCurrentDirectory()}\..\strings.txt", testSplit_strings);
                exit();
            }
            #endif


            #if !DEBUG
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

                if (sidbasePath.ToLower().Remove(2) == "no") // switch to encoder-only mode (why did I implement it this way??)
                {
                    sidbasePath = "N/A";
                }
                // Repeatedly ask for a valid path until one's provided
                while (!File.Exists(sidbasePath = read().Replace("\"", string.Empty)))
                {
                    Console.Clear();
                    echo ("Invalid path provided, try again. (or type \"no\" to run in encoder-only mode)");
                    sidbasePath = read().Replace("\"", string.Empty);
                }
            }



            // Load the string id base in to memory for scanning
            Console.Clear();

            if (sidbasePath == "N/A")
            {
                mode = "Encode"; // I keep changing which is default, so may as well do this here jic.
            }
            #endif


            echo ("Loading sidbase...");


            #if !DEBUG
        retry:
            wholeSidbase = File.ReadAllBytes(sidbasePath);
            if (wholeSidbase.Length < 24) // not that it would make sense for anyone to use an sidbase with a single sid... or be using this random exe off of my pc.
            {
                echo($"Invalid sidbase provided ({nameof(wholeSidbase)}.Length < 24); Please provide an alternate path.");

                sidbasePath = read().Replace("\"", string.Empty);
                    
                Console.Clear();
                goto retry;
            }
            #else
            restart:
            //if (firstRun)
            //{
            //    wholeSidbase = File.ReadAllBytes($"{Directory.GetCurrentDirectory()}\\..\\even_sidbase.bin");
            //}
            //else
            //    wholeSidbase = File.ReadAllBytes($"{Directory.GetCurrentDirectory()}\\..\\odd_sidbase.bin");

            wholeSidbase = File.ReadAllBytes($"{Directory.GetCurrentDirectory()}\\..\\sidbase.bin"); //!
            #endif


            // Read the table length to get the expected size of the hash table (don't really need it anymore)
            hashTableRawLength = BitConverter.ToUInt64(wholeSidbase, 0) * 16;

            // Initialize the hash/string tables, and read them from the active sidbase
            sidbaseRawHashTable   = ReadBytes(wholeSidbase, 8, (int)hashTableRawLength);
            sidbaseRawStringTable = ReadBytes(wholeSidbase, sidbaseRawHashTable.Length + 8, wholeSidbase.Length - (int) (hashTableRawLength + 8));

            #if false
            File.WriteAllBytes($@"{Directory.GetCurrentDirectory()}\..\{nameof(sidbaseRawHashTable)}.bin", sidbaseRawHashTable);
            File.WriteAllBytes($@"{Directory.GetCurrentDirectory()}\..\{nameof(sidbaseRawStringTable)}.bin", sidbaseRawStringTable);
            exit();
            #endif
            
            

            
            //#
            //## TEMPORARY TESTING
            //#

            //goto skipToMain;

            string[] lines;

            
            echo("Loading hashes & strings... ");
            lines = File.ReadAllLines($"{Directory.GetCurrentDirectory()}\\..\\re-encoded.txt");
            var hashes = new byte [lines.Length][];
            var strings = new string [lines.Length];
            for (var i = 0; i < lines.Length; ++i)
            {
                hashes[i] = BitConverter.GetBytes(ulong.Parse(lines[i].Remove(lines[i].IndexOf(':')), System.Globalization.NumberStyles.HexNumber));
                strings[i] = lines[i].Substring(lines[i].IndexOf(':') + 1);
            }



            echo("\nStarting tests.");
            var tmp = new [] { new byte[] { 0x5C, 0x12, 0x87, 0xD1, 0xD4, 0x01, 0x00, 0x00 }, new byte[] { 0x3E, 0x38, 0xB9, 0xAA, 0x86, 0x04, 0x00, 0x00 }, new byte[] { 0x60, 0x34, 0xA6, 0x8A, 0xDF, 0x08, 0x00, 0x00 }, new byte[] { 0x60, 0x34, 0xA6, 0x8A, 0xDF, 0x08, 0x00, 0x00 }, new byte[] { 0xC9, 0x4E, 0x4D, 0x9F, 0xF4, 0x08, 0x00, 0x00 } };

            echo($"Running Test on binary search method with {sidbasePath.Substring(sidbasePath.LastIndexOf('\\') + 1)}. (Start Time: {DateTime.Now})|({hashes.Length})");
            for (var i = 0; i < hashes.Length; i++)
            {
                // fuck it
                if (tmp.Any(item => item.SequenceEqual(hashes[i])))
                {
                    continue;
                }

                var expected = BitConverter.GetBytes(ulong.Parse(EncodeString(strings[i])[1], System.Globalization.NumberStyles.HexNumber));
                if (!expected.SequenceEqual(hashes[i]))
                {
                    echo($"{strings[i]} => {BitConverter.ToString(expected).Replace("-", string.Empty)}, not {BitConverter.ToString(hashes[i]).Replace("-", string.Empty)}");
                    //continue;
                }

                var decodedHash = New_DecodeSIDHash(sidbaseRawHashTable, sidbaseRawStringTable, hashTableRawLength, hashes[i])[1];
                if (decodedHash != strings[i])
                {
                    echo ($"fuck (#{i + 1}: {BitConverter.ToString(hashes[i]).Replace("-", string.Empty)} (0x{BitConverter.ToUInt64(hashes[i], 0).ToString("X").PadLeft(16, '0')}ul / {BitConverter.ToUInt64(hashes[i], 0)})");

                    echo($"Received: {decodedHash}");
                    echo($"Expected: {strings[i]}\n");
                }
                //else
                //    echo($"#{i:X} good");
            }
            echo($"End Time: {DateTime.Now}.\n");


            #if DEBUG
            if (!firstRun)
            {
                read();
            }
            else
            {
                echo("Loading odd sidbase...");
                firstRun = false;
                goto restart;
            }
            #endif


            //=====================================\\
            //--|   Main Input & Display Loop   |--\\
            //=====================================\\
            #region [Main Input & Display Loop]
            skipToMain:
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
                            
                        
                            lookupLines.Add(New_DecodeSIDHash(sidbaseRawHashTable, sidbaseRawStringTable, hashTableRawLength, hash));
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
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"> Thrown in the event of an invalid string pointer read from the sidbase after the provided hash is located. </exception>
        private static string[] New_DecodeSIDHash(byte[] hashTable, byte[] stringTable, ulong fullHashTableLength, byte[] bytesToDecode)
        {
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
                    previousAddress = 0xBADDEADBEEF,
                    currentRange
                ;

                scanAddress = currentRange = fullHashTableLength / 2;

                // check whether or not the chunk can be evenly split; if not, check
                // the odd one out for the expected hash, then exclude it and continue as normal if it isn't a match.
                if (((fullHashTableLength >> 4) & 1) == 1)
                {
                    var checkedHash = BitConverter.ToUInt64(hashTable, (int) fullHashTableLength - 0x10);
                    
                    if (checkedHash == expectedHash)
                    {
                        scanAddress = fullHashTableLength - 0x10;
                        echo("Skipping check, found hash at the end of the collection.");
                        goto readString;
                    }
                    
                    scanAddress = currentRange -= 8;
                }
                
                


                while (true)
                {
                    // check for uneven split again
                    if (scanAddress.ToString("X").Last() == '8')
                    {
                        //echo($"Adjusting scan address ({scanAddress:X} => {scanAddress - 8:X})");
                        scanAddress -= 0x8;
                    } 
                    if (currentRange.ToString("X").Last() == '8')
                    {
                        //echo($"Adjusting current range ({currentRange:X} => {currentRange - 8:X})");
                        currentRange -= 0x8;
                    }


                    currentHash = BitConverter.ToUInt64(hashTable, (int) scanAddress);

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
                    {
                        //Debug.WriteLine("0x" + scanAddress.ToString("X"));
                        break;
                    }
                    


                    // Handle missing sid's. How did I forget about that?
                    if (scanAddress == previousAddress)
                    {
                        Debug.WriteLine("(gave up) 0x" + scanAddress.ToString("X"));
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
                    stringBuffer += Encoding.ASCII.GetString(stringTable, (int)stringPtr++, 1);
                }

                
                ret[1] = stringBuffer;
            }
            else {
                echo($"Invalid SID provided; unexpected length of \"{bytesToDecode?.Length ?? 0}\". Must be 8 bytes.");
                ret[1] = "INVALID_SID_64";
            }

            return ret;
        }



        // private static string[] Original_DecodeSIDHash(byte[] sidbase, long sidLength, byte[] bytesToDecode)
        /// <summary>
        /// Attempt to decode a provided 64-bit FNV-1a hash via a provided lookup file (sidbase.bin)
        /// </summary>
        /// <param name="sidbase"> The loaded lookup table. </param>
        /// <param name="bytesToDecode"> The hash to decode, as an array of bytes </param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"> Thrown in the event of an invalid string pointer read from the sidbase after the provided hash is located. </exception>
        private static string[] Original_DecodeSIDHash(byte[] sidbase, ulong sidLength, byte[] bytesToDecode)
        {
            var ret = new string[] { BitConverter.ToString(bytesToDecode).Replace("-", string.Empty), "UNKNOWN_SID_64"};
            
            if (bytesToDecode.Length == 8)
            {
                for (ulong mainArrayIndex = 0, subArrayIndex = 0; mainArrayIndex < sidLength; subArrayIndex = 0, mainArrayIndex+=8)
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
