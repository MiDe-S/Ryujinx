﻿/*
 * Copyright (C) 2016 Benjamin Krämer
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using LibAmiibo.Data.Settings.AppData.TitleID;
using LibAmiibo.Helper;

namespace LibAmiibo.Data.Settings.AppData
{
    // TODO: Use http://3dsdb.com/, http://wiiubrew.org/wiki/Title_database and http://switchbrew.org/index.php?title=Title_list to resolve the titles
    public class Title
    {
        private IDBEContext context = null;
        public ArraySegment<byte> Data { get; private set; }
        private IList<byte> DataList { get { return (IList<byte>) Data; } }

        public IDBEContext Context
        {
            get
            {
                if (context == null)
                {
                    context = CDNUtils.DownloadTitleData(this);
                }
                return context;
            }
        }

        public Platform Platform
        {
            get { return (Platform)(NtagHelpers.UInt16FromTag(Data, 0x00)); }
        }
        // TODO: This is only correct for N3DS and WiiU
        public Category Category
        {
            get { return (Category)(NtagHelpers.UInt16FromTag(Data, 0x02)); }
        }
        public ulong TitleID
        {
            get { return NtagHelpers.UInt64FromTag(Data, 0x00); }
        }
        // TODO: This is only correct for N3DS and WiiU
        public uint UniqueID
        {
            get
            {
                var data = new byte[0x04];
                Array.Copy(Data.Array, Data.Offset + 0x04, data, 0x01, 0x03); // Offset by 0x02
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(data);
                return BitConverter.ToUInt32(data, 0);
            }
        }
        // TODO: This is only correct for N3DS and WiiU
        public IdRestriction.TitleType UniqueIDType
        {
            get { return IdRestriction.GetType(UniqueID); }
        }
        // TODO: This is only correct for N3DS and WiiU
        public Variation Variation
        {
            get { return (Variation)(DataList[0x07]); }
        }

        private Title(ArraySegment<byte> data)
        {
            this.Data = data;
        }

        public static Title FromTitleID(long data)
        {
            var formatted = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(formatted);
            return Title.FromTitleID(formatted);
        }

        public static Title FromTitleID(byte[] data)
        {
            return new Title(new ArraySegment<byte>(data));
        }

        public static Title FromTitleID(string data)
        {
            return Title.FromTitleID(NtagHelpers.StringToByteArrayFastest(data));
        }

        public static Title FromTitleID(ArraySegment<byte> data)
        {
            return new Title(data);
        }
    }
}
