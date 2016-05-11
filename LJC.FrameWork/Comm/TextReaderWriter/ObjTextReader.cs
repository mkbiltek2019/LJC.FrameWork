﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LJC.FrameWork.Comm
{
    public class ObjTextReader : ObjTextReaderWriterBase, IDisposable
    {
        private StreamReader _sr;

        private ObjTextReader(string textfile)
        {
            this.readwritePath = textfile;
            var fs = File.Open(textfile, FileMode.Open, FileAccess.Read);
            _sr = new StreamReader(fs, Encoding.UTF8);
            int firstchar = _sr.BaseStream.ReadByte();
            if (firstchar == -1)
            {
                throw new Exception("未知编码方式。");
            }
            else
            {
                this._encodeType = (ObjTextReaderWriterEncodeType)firstchar;
            }

            _canReadFromBack = CanReadFormBack;
        }


        public static ObjTextReader CreateReader(string textfile)
        {
            return new ObjTextReader(textfile);
        }

        public T ReadObjectFromBack<T>(bool firstRead) where T : class
        {
            if (!_canReadFromBack)
                throw new Exception("不支持从后向前读");

            if (_sr.BaseStream.Position <= 1)
            {
                if (!firstRead)
                    return default(T);
                else
                {
                    int offset = 4;
                    if (CheckHasEndSpan(_sr.BaseStream))
                    {
                        offset = 7;
                    }

                    if (_sr.BaseStream.Length < offset)
                        return default(T);

                    _sr.BaseStream.Position = _sr.BaseStream.Length - offset;
                }
            }
            var oldpostion = _sr.BaseStream.Position;
            byte[] bylen = new byte[4];
            _sr.BaseStream.Read(bylen, 0, 4);
            var len = BitConverter.ToInt32(bylen, 0);

            _sr.BaseStream.Position = oldpostion - len;
            oldpostion = _sr.BaseStream.Position;
            var contentbyte = new Byte[len];
            _sr.BaseStream.Read(contentbyte, 0, len);
            if (oldpostion > 8)
                _sr.BaseStream.Position = oldpostion - 8;
            else
                _sr.BaseStream.Position = oldpostion - 4;

            using (MemoryStream ms = new MemoryStream(contentbyte))
            {
                return ProtoBuf.Serializer.Deserialize<T>(ms);
            }
        }

        public T ReadObject<T>() where T : class
        {
            if (_encodeType == ObjTextReaderWriterEncodeType.protobuf
                || _encodeType == ObjTextReaderWriterEncodeType.protobufex)
            {
                byte[] bylen = new byte[4];
                _sr.BaseStream.Read(bylen, 0, 4);
                var len = BitConverter.ToInt32(bylen, 0);
                //239 187 191
                if (len == 0 || len == 12565487)
                    return default(T);
                var contentbyte = new Byte[len];
                _sr.BaseStream.Read(contentbyte, 0, len);

                if (_canReadFromBack)
                {
                    _sr.BaseStream.Position += 4;
                }

                using (MemoryStream ms = new MemoryStream(contentbyte))
                {
                    return ProtoBuf.Serializer.Deserialize<T>(ms);
                }
            }
            else if (_encodeType == ObjTextReaderWriterEncodeType.jsongzip)
            {
                byte[] bylen = new byte[4];
                _sr.BaseStream.Read(bylen, 0, 4);
                var len = BitConverter.ToInt32(bylen, 0);
                if (len == 0 || len == 12565487)
                    return default(T);
                var contentbyte = new Byte[len];
                _sr.BaseStream.Read(contentbyte, 0, len);

                var decomparssbytes = GZip.Decompress(contentbyte);
                var jsonstr = Encoding.UTF8.GetString(decomparssbytes);
                return JsonUtil<T>.Deserialize(jsonstr);
            }
            else
            {
                //string s = _sr.ReadLine().Trim((char)65279, (char)1); //过滤掉第一行
                string s = _sr.ReadLine();

                if (s == null)
                    return default(T);

                s = s.Trim((char)65279, (char)1);

                while ((string.IsNullOrEmpty(s) || !s.Last().Equals(splitChar))
                    && !_sr.EndOfStream)
                {
                    s += _sr.ReadLine().Trim((char)65279, (char)1);
                }

                if (!string.IsNullOrEmpty(s) && s.Last().Equals(splitChar))
                {
                    s = s.Remove(s.Length - 1, 1);
                    return JsonUtil<T>.Deserialize(s);
                }

                return default(T);
            }
        }

        public void Dispose()
        {
            if (_sr != null)
            {
                _sr.Close();
                _sr = null;
            }
        }
    }
}
