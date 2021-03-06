﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace R2RDump
{
    class R2RHeader
    {
        public enum ReadyToRunFlag
        {
            READYTORUN_FLAG_PLATFORM_NEUTRAL_SOURCE = 0x00000001,
            READYTORUN_FLAG_SKIP_TYPE_VALIDATION = 0x00000002
        }

        /// <summary>
        /// The expected signature of a ReadyToRun header
        /// </summary>
        public const uint READYTORUN_SIGNATURE = 0x00525452; // 'RTR'

        /// <summary>
        /// RVA to the begining of the ReadyToRun header
        /// </summary>
        public int RelativeVirtualAddress { get; }

        /// <summary>
        /// Size of the ReadyToRun header
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Signature of the header in string and hex formats
        /// </summary>
        public string SignatureString { get; }
        public uint Signature { get; }

        /// <summary>
        /// The ReadyToRun version
        /// </summary>
        public ushort MajorVersion { get; }
        public ushort MinorVersion { get; }

        /// <summary>
        /// Flags in the header
        /// eg. PLATFORM_NEUTRAL_SOURCE, SKIP_TYPE_VALIDATION
        /// </summary>
        public uint Flags { get; }

        /// <summary>
        /// The ReadyToRun section RVAs and sizes
        /// </summary>
        public Dictionary<R2RSection.SectionType, R2RSection> Sections { get; }

        /// <summary>
        /// Initializes the fields of the R2RHeader
        /// </summary>
        /// <param name="image">PE image</param>
        /// <param name="rva">Relative virtual address of the ReadyToRun header</param>
        /// <param name="curOffset">Index in the image byte array to the start of the ReadyToRun header</param>
        /// <exception cref="BadImageFormatException">The signature must be 0x00525452</exception>
        public R2RHeader(byte[] image, int rva, int curOffset)
        {
            RelativeVirtualAddress = rva;
            int startOffset = curOffset;

            byte[] signature = new byte[sizeof(uint)];
            Array.Copy(image, curOffset, signature, 0, sizeof(uint));
            SignatureString = System.Text.Encoding.UTF8.GetString(signature);
            Signature = R2RReader.ReadUInt32(image, ref curOffset);
            if (Signature != READYTORUN_SIGNATURE)
            {
                throw new System.BadImageFormatException("Incorrect R2R header signature");
            }

            MajorVersion = R2RReader.ReadUInt16(image, ref curOffset);
            MinorVersion = R2RReader.ReadUInt16(image, ref curOffset);
            Flags = R2RReader.ReadUInt32(image, ref curOffset);
            int nSections = R2RReader.ReadInt32(image, ref curOffset);
            Sections = new Dictionary<R2RSection.SectionType, R2RSection>();

            for (int i = 0; i < nSections; i++)
            {
                int type = R2RReader.ReadInt32(image, ref curOffset);
                var sectionType = (R2RSection.SectionType)type;
                if (!Enum.IsDefined(typeof(R2RSection.SectionType), type))
                {
                    R2RDump.OutputWarning("Invalid ReadyToRun section type");
                }
                Sections[sectionType] = new R2RSection(sectionType,
                    R2RReader.ReadInt32(image, ref curOffset),
                    R2RReader.ReadInt32(image, ref curOffset));
            }

            Size = curOffset - startOffset;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat($"Signature: 0x{Signature:X8} ({SignatureString})\n");
            sb.AppendFormat($"RelativeVirtualAddress: 0x{RelativeVirtualAddress:X8}\n");
            if (Signature == READYTORUN_SIGNATURE)
            {
                sb.AppendFormat($"Size: {Size} bytes\n");
                sb.AppendFormat($"MajorVersion: 0x{MajorVersion:X4}\n");
                sb.AppendFormat($"MinorVersion: 0x{MinorVersion:X4}\n");
                sb.AppendFormat($"Flags: 0x{Flags:X8}\n");
                foreach (ReadyToRunFlag flag in Enum.GetValues(typeof(ReadyToRunFlag)))
                {
                    if ((Flags & (uint)flag) != 0)
                    {
                        sb.AppendFormat($"  - {Enum.GetName(typeof(ReadyToRunFlag), flag)}\n");
                    }
                }
            }
            return sb.ToString();
        }
    }
}
