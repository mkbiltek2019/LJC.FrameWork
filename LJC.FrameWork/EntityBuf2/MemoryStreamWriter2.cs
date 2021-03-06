﻿using LJC.FrameWork.Comm;
using LJC.FrameWork.EntityBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LJC.FrameWork.EntityBuf2
{
    public class MemoryStreamWriter2 : IDisposable
    {
        private MemoryStream _ms;
        private static byte[] bytesNull = BitConverter.GetBytes(-1);
        private static byte[] bytesZero = BitConverter.GetBytes(0);
        private static float maxFloat = 9999999f;

        private static byte[] bytesTrue = BitConverter.GetBytes(true);
        private static byte[] bytesFalse = BitConverter.GetBytes(false);

        private BufferPollManager _bufferPollManager;
        public int Bufferindex = -1;
        private long _bufferoffset = -1;

        private bool _needcheckbuffuer = false;

        public MemoryStreamWriter2(MemoryStream ms)
        {
            _ms = ms;
        }

        public MemoryStreamWriter2(BufferPollManager bufferpollmanger)
        {
            var bufferindex = bufferpollmanger.GetBuffer();
            if (bufferindex == -1)
            {
                _ms = new MemoryStream();
            }
            else
            {
                _bufferPollManager = bufferpollmanger;
                Bufferindex = bufferindex;
                _bufferoffset = bufferpollmanger.GetOffset(bufferindex);
                _ms = new MemoryStream(bufferpollmanger.Buffer, (int)_bufferoffset, bufferpollmanger.BlockSize);
                _needcheckbuffuer = true;
            }
            //_ms.Position = 4;
            _ms.Position = 8;
        }

        private void CheckBufferPoll(int willwritecount)
        {
            if (!_needcheckbuffuer)
            {
                return;
            }

            if (_ms.Position + willwritecount > _bufferPollManager.BlockSize)
            {
                var pos = _ms.Position;
                var newms = new System.IO.MemoryStream();

                newms.Write(_bufferPollManager.Buffer, (int)_bufferoffset, (int)pos);

                _ms.Close();
                _ms.Dispose();
                _bufferPollManager.RealseBuffer(Bufferindex);
                Bufferindex = -1;
                _bufferPollManager = null;

                _ms = newms;

                _needcheckbuffuer = false;
            }
        }

        public byte[] GetBytes()
        {
            if (!_needcheckbuffuer)
            {
                return _ms.ToArray();
            }
            else
            {
                using (var newms = new System.IO.MemoryStream())
                {
                    newms.Write(_bufferPollManager.Buffer, (int)_bufferoffset, (int)_ms.Position);
                    return newms.ToArray();
                }
            }
        }

        internal long GetDataLen()
        {
            return _ms.Position;
        }

        public void WriteBool(bool boo)
        {
            CheckBufferPoll(1);

            if (boo)
            {
                _ms.Write(bytesTrue, 0, bytesTrue.Length);
            }
            else
            {
                _ms.Write(bytesFalse, 0, bytesFalse.Length);
            }

            //byte[] bts = BitConverter.GetBytes(boo);
            //_ms.Write(bts, 0, bts.Length);
        }

        public void WriteBoolArray(bool[] booArray)
        {
            CheckBufferPoll(booArray == null ? 1 : booArray.Length + 5);

            if (booArray == null)
            {
                _ms.WriteByte((byte)ArrayTypeFlag.NULL);
                return;
            }
            else if (booArray.Length == 0)
            {
                _ms.WriteByte((byte)ArrayTypeFlag.Empty);
                return;
            }

            ArrayTypeFlag flag = ArrayTypeFlag.DEFAULT;
            int len = booArray.Length;
            var bytelen = CompressInt32(len);
            if (bytelen.Length == 1)
            {
                flag = ArrayTypeFlag.ByteLen;
            }
            else if (bytelen.Length == 2)
            {
                flag = ArrayTypeFlag.ShortLen;
            }

            _ms.WriteByte((byte)flag);
            _ms.Write(bytelen, 0, bytelen.Length);
            BitArray ba = new BitArray(booArray);

            var btarray = BitHelper.ConvertToByteArray(ba);
            foreach (byte b in btarray)
            {
                _ms.WriteByte(b);
            }
        }

        public void WriteInt16(Int16 num)
        {
            CheckBufferPoll(3);

            if (num == 0)
            {
                _ms.WriteByte((byte)ShortTypeEnum.Zero);
                return;
            }

            ShortTypeEnum flag = ShortTypeEnum.DEFAULT;

            if (num < 0)
            {
                flag |= ShortTypeEnum.Minus;
                num = Math.Abs(num);
            }

            byte[] bytes = BitConverter.GetBytes(num);
            _ms.WriteByte((byte)flag);
            _ms.Write(bytes, 0, bytes.Length);
        }

        public void WriteInt16Array(Int16[] numArray)
        {
            CheckBufferPoll(4);

            if (numArray == null)
            {
                _ms.Write(bytesNull, 0, 4);
                return;
            }

            int len = numArray.Length;
            WriteInt32(len);
            foreach (Int16 i in numArray)
            {
                WriteInt16(i);
            }
        }

        public void WriteUInt16(UInt16 num)
        {
            CheckBufferPoll(3);

            if (num == 0)
            {
                _ms.WriteByte((byte)UShrotTypeEnum.Zero);
                return;
            }

            UShrotTypeEnum flag = UShrotTypeEnum.DEFAULT;

            byte[] bytes = BitConverter.GetBytes(num);
            _ms.WriteByte((byte)flag);
            _ms.Write(bytes, 0, bytes.Length);
        }

        public void WriteUInt16Array(UInt16[] numArray)
        {
            CheckBufferPoll(4);

            if (numArray == null)
            {
                _ms.Write(bytesNull, 0, 4);
                return;
            }

            int len = numArray.Length;
            WriteInt32(len);
            foreach (UInt16 i in numArray)
            {
                WriteUInt16(i);
            }
        }

        public void WriteInt32(Int32 num)
        {
            CheckBufferPoll(5);

            if (num == 0)
            {
                _ms.WriteByte((byte)IntTypeFlag.Zero);
                return;
            }
            IntTypeFlag flag = IntTypeFlag.DEFAULT;
            if (num < 0)
            {
                flag = IntTypeFlag.Minus;
                num = Math.Abs(num);
            }
            byte[] byts = BitConverter.GetBytes(num);
            flag |= IntTypeFlag.INT;

            _ms.WriteByte((byte)flag);
            _ms.Write(byts, 0, byts.Length);
        }

        public void WriteInt32Array(Int32[] numArray)
        {
            CheckBufferPoll(numArray == null ? 1 : 5);

            if (numArray == null)
            {
                _ms.WriteByte((byte)ArrayTypeFlag.NULL);
                return;
            }

            bool isCompress = numArray.Where(p => p <= ushort.MaxValue).Count() > (numArray.Length * 2 / 3);
            ArrayTypeFlag flag = isCompress ? ArrayTypeFlag.Compress : ArrayTypeFlag.DEFAULT;
            byte[] bytelen = CompressInt32(numArray.Length);
            if (bytelen.Length == 1)
            {
                _ms.WriteByte((byte)(ArrayTypeFlag.ByteLen | flag));
            }
            else if (bytelen.Length == 2)
            {
                _ms.WriteByte((byte)(ArrayTypeFlag.ShortLen | flag));
            }
            else
            {
                _ms.WriteByte((byte)(ArrayTypeFlag.IntLen | flag));
            }
            _ms.Write(bytelen, 0, bytelen.Length);
            foreach (int num in numArray)
            {
                if (!isCompress)
                {
                    _ms.Write(BitConverter.GetBytes(num), 0, 4);
                }
                else
                {
                    WriteInt32(num);
                }
            }
        }

        public void WriteChar(char num)
        {
            CheckBufferPoll(2);
            _ms.Write(BitConverter.GetBytes(num), 0, 2);
        }

        public void WriteCharArray(char[] numArray)
        {
            CheckBufferPoll(4);
            if (numArray == null)
            {
                _ms.Write(bytesNull, 0, 4);
                return;
            }

            int len = numArray.Length;
            WriteInt32(len);
            foreach (char i in numArray)
            {
                WriteChar(i);
            }
        }

        public void WriteInt64(Int64 num)
        {
            CheckBufferPoll(9);

            if (num == default(Int64))
            {
                _ms.WriteByte((byte)LongTypeEnum.Zero);
                return;
            }
            LongTypeEnum flag = LongTypeEnum.DEFAULT;
            if (num < 0)
            {
                flag = LongTypeEnum.Minus;
                num = Math.Abs(num);
            }

            byte[] byts = BitConverter.GetBytes(num);
            _ms.WriteByte((byte)flag);
            _ms.Write(byts, 0, byts.Length);
        }

        public void WriteInt64Array(Int64[] intArray)
        {
            CheckBufferPoll(intArray == null ? 1 : 5);

            if (intArray == null)
            {
                _ms.WriteByte((byte)ArrayTypeFlag.NULL);
                return;
            }
            else if (intArray.Length == 0)
            {
                _ms.WriteByte((byte)ArrayTypeFlag.Empty);
                return;
            }
            var flag = ArrayTypeFlag.DEFAULT;
            int len = intArray.Length;
            byte[] byts = CompressInt32(len);
            if (byts.Length == 1)
            {
                flag |= ArrayTypeFlag.ByteLen;
            }
            else if (byts.Length == 2)
            {
                flag |= ArrayTypeFlag.ShortLen;
            }

            _ms.WriteByte((byte)flag);
            _ms.Write(byts, 0, byts.Length);
            foreach (Int64 num in intArray)
            {
                WriteInt64(num);
            }
        }

        public void WriteAsii(string str)
        {
            if (str != null)
            {
                byte[] byts = Encoding.ASCII.GetBytes(str);

                CheckBufferPoll(4 + byts.Length);

                _ms.Write(BitConverter.GetBytes(byts.Length), 0, 4);
                _ms.Write(byts, 0, byts.Length);
            }
            else
            {
                CheckBufferPoll(4);

                _ms.Write(bytesNull, 0, 4);
            }
        }


        public void WriteString(string str)
        {
            CheckBufferPoll(1);

            if (str == null)
            {
                _ms.WriteByte((byte)StringTypeFlag.NULL);
                return;
            }
            else if (string.IsNullOrWhiteSpace(str))
            {
                _ms.WriteByte((byte)StringTypeFlag.Empty);
                return;
            }

            byte[] byts;
            StringTypeFlag flag = StringTypeFlag.DEFAULT;
            //bool isAsscii = StringHelper.IsAscii(str);
            bool isAsscii = false;
            if (isAsscii)
            {
                flag = StringTypeFlag.AssciiEncoding;
                byts = Encoding.ASCII.GetBytes(str);
            }
            else
            {
                flag = StringTypeFlag.UTF8Encoding;
                byts = Encoding.UTF8.GetBytes(str);
            }

            byte[] lenbytes = null;

            flag |= StringTypeFlag.IntLen;
            lenbytes = BitConverter.GetBytes(byts.Length);

            CheckBufferPoll(5 + byts.Length);

            _ms.WriteByte((byte)flag);
            _ms.Write(lenbytes, 0, lenbytes.Length);

            _ms.Write(byts, 0, byts.Length);


        }

        private byte[] CompressInt32(Int32 num)
        {
            byte[] byts = null;
            //if (num >= 0)
            //{
            //    if (num <= byte.MaxValue)
            //    {
            //        byts = new byte[] { (byte)num };
            //    }
            //    else if (num <= UInt16.MaxValue)
            //    {
            //        byts = BitConverter.GetBytes((UInt16)num);
            //    }
            //}
            if (byts == null)
                byts = BitConverter.GetBytes(num);
            return byts;
        }

        public void WriteStringArray(string[] strArray)
        {
            CheckBufferPoll(1);

            if (strArray == null)
            {
                _ms.WriteByte((byte)StringTypeFlag.NULL);
                return;
            }

            byte[] lenbytes = CompressInt32(strArray.Length);
            CheckBufferPoll(lenbytes.Length + 1);
            if (lenbytes.Length == 1)
            {
                _ms.WriteByte((byte)StringTypeFlag.ByteLen);
            }
            else if (lenbytes.Length == 2)
            {
                _ms.WriteByte((byte)StringTypeFlag.ShortLen);
            }
            else
            {
                _ms.WriteByte((byte)StringTypeFlag.IntLen);
            }

            _ms.Write(lenbytes, 0, lenbytes.Length);
            foreach (string s in strArray)
            {
                WriteString(s);
            }
        }

        public void WriteDateTime(DateTime dateTime)
        {
            byte[] byts = BitConverter.GetBytes(dateTime.ToOADate());

            CheckBufferPoll(byts.Length);

            _ms.Write(byts, 0, byts.Length);
        }

        public void WriteDateTimeArray(DateTime[] dateTimes)
        {
            CheckBufferPoll(1);

            if (dateTimes == null)
            {
                _ms.WriteByte((byte)ArrayTypeFlag.NULL);
                return;
            }
            else if (dateTimes.Length == 0)
            {
                _ms.WriteByte((byte)ArrayTypeFlag.Empty);
                return;
            }

            var flag = ArrayTypeFlag.DEFAULT;
            int len = dateTimes.Length;
            var bytslen = CompressInt32(len);
            if (bytslen.Length == 1)
            {
                flag = ArrayTypeFlag.ByteLen;
            }
            else if (bytslen.Length == 2)
            {
                flag = ArrayTypeFlag.ShortLen;
            }

            CheckBufferPoll(bytslen.Length + 1);
            _ms.WriteByte((byte)flag);
            _ms.Write(bytslen, 0, bytslen.Length);
            foreach (DateTime dt in dateTimes)
            {
                var byts = BitConverter.GetBytes(dt.ToOADate());
                CheckBufferPoll(byts.Length);
                _ms.Write(byts, 0, byts.Length);
            }
        }

        public void WriteByteArray(byte[] data)
        {
            if (data == null)
            {
                this.WriteInt32(-1);
            }
            else
            {
                this.WriteInt32(data.Length);
                this.WriteBytes(data);
            }
        }

        public void WriteByte(byte data)
        {
            CheckBufferPoll(1);

            _ms.WriteByte(data);
        }

        public void WriteBytes(byte[] data)
        {
            CheckBufferPoll(data.Length);

            _ms.Write(data, 0, data.Length);
        }

        public void WriteDecimal(decimal data)
        {
            CheckBufferPoll(1);

            if (data == 0m)
            {
                _ms.WriteByte((byte)DecimalTypeFlag.Zero);
                return;
            }
            DecimalTypeFlag flag = DecimalTypeFlag.DEFAULT;
            if (data < 0)
            {
                flag = DecimalTypeFlag.Minus;
                data = Math.Abs(data);
            }

            flag |= DecimalTypeFlag.DoubleVal;
            byte[] byts = BitConverter.GetBytes((double)data);

            CheckBufferPoll(1 + byts.Length);
            _ms.WriteByte((byte)flag);
            _ms.Write(byts, 0, byts.Length);
        }

        public void WriteDeciamlArray(decimal[] data)
        {
            CheckBufferPoll(4);

            if (data == null)
            {
                _ms.Write(bytesNull, 0, 4);
                return;
            }

            _ms.Write(BitConverter.GetBytes(data.Length), 0, 4);
            foreach (decimal d in data)
            {
                WriteDecimal(d);
            }
        }

        public void WriteDoubleArray(double[] data)
        {
            if (data == null || data.Length == 0)
            {
                WriteInt32(-1);
                //WriteInt32(bytesNull, 0, 4);
                return;
            }
            WriteInt32(data.Length);
            foreach (double d in data)
            {
                WriteDouble(d);
            }
        }

        public void WriteDouble(double data)
        {
            var bytes = BitConverter.GetBytes(data);

            CheckBufferPoll(bytes.Length);

            _ms.Write(bytes, 0, bytes.Length);

            //DoubleTypeFlag flag = DoubleTypeFlag.DEFAULT;
            //if (data < 0.0)
            //{
            //    flag = DoubleTypeFlag.Minus;
            //    data = Math.Abs(data);
            //}
            //byte[] buffer = null;
            //int mod = (int)(data % 1);
            //if (mod == 0)
            //{
            //    if (data <= byte.MaxValue)
            //    {
            //        flag |= DoubleTypeFlag.ByteVal;
            //        buffer = new byte[] { (byte)flag };
            //    }
            //    else if (data <= ushort.MaxValue)
            //    {
            //        flag |= DoubleTypeFlag.ShortVal;
            //        buffer = BitConverter.GetBytes((ushort)data);
            //    }
            //    else if (data <= UInt32.MaxValue)
            //    {
            //        flag |= DoubleTypeFlag.IntVal;
            //        buffer = BitConverter.GetBytes((UInt32)data);
            //    }
            //}
            //else if (((data < 10000000) && ((mod * 10) % 1 == 0))
            //    ||((data < 1000000) && ((mod * 100) % 1 == 0))
            //    ||((data < 100000) && ((mod * 1000) % 1 == 0))
            //    ||((data < 10000) && ((mod * 10000) % 1 == 0))
            //    ||((data < 1000) && ((mod * 100000) % 1 == 0))
            //    ||((data < 100) && ((mod * 1000000) % 1 == 0))
            //    ||((data < 10) && ((mod * 10000000) % 1 == 0)))
            //{
            //    flag |= DoubleTypeFlag.FloatVal;
            //    buffer = BitConverter.GetBytes((float)data);
            //}

            //if (buffer == null)
            //{
            //    buffer = BitConverter.GetBytes(data);
            //    flag = DoubleTypeFlag.DEFAULT;
            //}
            //_ms.WriteByte((byte)flag);
            //_ms.Write(buffer, 0, buffer.Length);
        }

        public void WriteFloat(float data)
        {
            byte[] bytes = BitConverter.GetBytes(data);

            _ms.Write(bytes, 0, bytes.Length);
        }

        public void WriteFloatArray(float[] data)
        {
            if (data == null || data.Length == 0)
            {
                WriteInt32(-1);
                // WriteInt32(bytesNull, 0, 4);
                return;
            }
            WriteInt32(data.Length);
            foreach (float d in data)
            {
                WriteFloat(d);
            }
        }

        void IDisposable.Dispose()
        {
            _ms.Close();
        }
    }
}
