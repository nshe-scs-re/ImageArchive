using System;
using System.IO;

string filePath = @"C:\Users\whaley\source\Images\Rockland\Camera\Images\17269\149206322383.jpg";

if(Path.IsPathFullyQualified(filePath) && File.Exists(filePath))
{
    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
    using (BinaryReader binaryReader = new BinaryReader(fileStream))
    {
        if(fileStream.Length < 2)
        {
            Console.WriteLine("[ERROR] [Program.cs] [Main]: File is too small to be a valid JPEG file. Exiting...");
            return;
        }

        fileStream.Position = fileStream.Length - 2;

        ushort segmentMarker = ConvertToBigEndian16(binaryReader.ReadUInt16());

        if(segmentMarker != 0xFFD9) // JPEG EOI marker (0xFFD9)
        {
            Console.WriteLine($"[ERROR] [Program.cs] [Main]: Unexpected EOI marker: 0x{segmentMarker:X4}");
            Console.WriteLine("[ERROR] [Program.cs] [Main]: Not a valid JPEG file. Exiting...");
            return;
        }

        fileStream.Position = 0;

        ushort firstHeader = ConvertToBigEndian16(binaryReader.ReadUInt16());

        if (firstHeader != 0xFFD8) // JPEG SOI marker (0xFFD8)
        {
            Console.WriteLine($"[ERROR] [Program.cs] [Main]: Unexpected SOI marker: 0x{firstHeader:X4}");
            Console.WriteLine("[ERROR] [Program.cs] [Main]: Not a valid JPEG file. Exiting...");
            return;
        }

        while (fileStream.Position < fileStream.Length)
        {
            segmentMarker = ConvertToBigEndian16(binaryReader.ReadUInt16());

            ushort segmentLength = ConvertToBigEndian16(binaryReader.ReadUInt16());

            if(segmentLength < 2 || (fileStream.Position + (segmentLength - 2)) > fileStream.Length)
            {
                Console.WriteLine("[ERROR] [Program.cs] [Main]: Invalid segment length. Exiting...");
                return;
            }

            if (segmentMarker == 0xFFE0) // APP0 marker (0xFFE0)
            {
                byte[] app0_header = binaryReader.ReadBytes(segmentLength - 2);

                Console.WriteLine("[INFO] [Program.cs] [Main]: Found APP0 header. Relevant bytes:");

                Console.WriteLine("----------------------------------");
                Console.WriteLine("{0, -3} | {1, -10} | {2, -15}", "Byte", "Hex Value", "Decimal Value");
                Console.WriteLine("----------------------------------");

                //for(int i = 0; i < app0_header.Length; i++)
                //{
                //    Console.WriteLine("{0,-4} | 0x{1,-8:X2} | {1, -15}", i + 1, app0_header[i]);
                //}

                byte byte_27 = app0_header[26];
                byte byte_29 = app0_header[28];

                Console.WriteLine("{0, -4} | 0x{1,-8:X2} | {1,-15}", 27, byte_27);
                Console.WriteLine("{0, -4} | 0x{1,-8:X2} | {1,-15}", 29, byte_29);

                break;
            }
            else
            {
                Console.WriteLine("[INFO] [Program.cs] [Main]: Found segment other than APP0, skipping segment.");
                fileStream.Position += segmentLength - 2;
            }
        }

    }
}
else
{
    Console.WriteLine($"[ERROR] [Program.CS] [Main]: '{filePath}' is an invalid file path.");
}

ushort ConvertToBigEndian16(ushort value)
{
    ushort originalMsb = (ushort)((value >> 8) & 0xFF);
    ushort originalLsb = (ushort)(value & 0xFF);

    ushort newMsb = (ushort)(originalLsb << 8);
    ushort newLsb = originalMsb;

    return (ushort)(newMsb | newLsb);
}
