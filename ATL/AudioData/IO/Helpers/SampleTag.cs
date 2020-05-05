﻿using Commons;
using System.Collections.Generic;
using System.IO;
using ATL.Logging;
using static ATL.AudioData.IO.MetaDataIO;
using System;
using System.Data;

namespace ATL.AudioData.IO
{
    public static class SampleTag
    {
        public const string CHUNK_SAMPLE = "smpl";

        public static void FromStream(Stream source, MetaDataIO meta, ReadTagParams readTagParams)
        {
            string str;
            byte[] data = new byte[256];

            // Manufacturer
            source.Read(data, 0, 4);
            int intData = StreamUtils.DecodeInt32(data);
            meta.SetMetaField("sample.manufacturer", intData.ToString(), readTagParams.ReadAllMetaFrames);

            // Product
            source.Read(data, 0, 4);
            intData = StreamUtils.DecodeInt32(data);
            meta.SetMetaField("sample.product", intData.ToString(), readTagParams.ReadAllMetaFrames);

            // Period
            source.Read(data, 0, 4);
            intData = StreamUtils.DecodeInt32(data);
            meta.SetMetaField("sample.period", intData.ToString(), readTagParams.ReadAllMetaFrames);

            // MIDI unity note
            source.Read(data, 0, 4);
            intData = StreamUtils.DecodeInt32(data);
            meta.SetMetaField("sample.MIDIUnityNote", intData.ToString(), readTagParams.ReadAllMetaFrames);

            // MIDI pitch fraction
            source.Read(data, 0, 4);
            intData = StreamUtils.DecodeInt32(data);
            meta.SetMetaField("sample.MIDIPitchFraction", intData.ToString(), readTagParams.ReadAllMetaFrames);

            // SMPTE format
            source.Read(data, 0, 4);
            intData = StreamUtils.DecodeInt32(data);
            meta.SetMetaField("sample.SMPTEFormat", intData.ToString(), readTagParams.ReadAllMetaFrames);

            // SMPTE offsets
            source.Read(data, 0, 1);
            sbyte sByteData = StreamUtils.DecodeSignedByte(data);
            meta.SetMetaField("sample.SMPTEOffset.Hours", sByteData.ToString(), readTagParams.ReadAllMetaFrames);
            source.Read(data, 0, 1);
            byte byteData = StreamUtils.DecodeUByte(data);
            meta.SetMetaField("sample.SMPTEOffset.Minutes", byteData.ToString(), readTagParams.ReadAllMetaFrames);
            source.Read(data, 0, 1);
            byteData = StreamUtils.DecodeUByte(data);
            meta.SetMetaField("sample.SMPTEOffset.Seconds", byteData.ToString(), readTagParams.ReadAllMetaFrames);
            source.Read(data, 0, 1);
            byteData = StreamUtils.DecodeUByte(data);
            meta.SetMetaField("sample.SMPTEOffset.Frames", byteData.ToString(), readTagParams.ReadAllMetaFrames);

            // Num sample loops
            source.Read(data, 0, 4);
            int numSampleLoops = StreamUtils.DecodeInt32(data);
            meta.SetMetaField("sample.NumSampleLoops", intData.ToString(), readTagParams.ReadAllMetaFrames);

            // Sample loops size (not useful here)
            source.Seek(4, SeekOrigin.Current);

            for (int i = 0; i < numSampleLoops; i++)
            {
                // Cue point ID
                source.Read(data, 0, 4);
                intData = StreamUtils.DecodeInt32(data);
                meta.SetMetaField("sample.SampleLoop[" + i + "].CuePointId", intData.ToString(), readTagParams.ReadAllMetaFrames);

                // Type
                source.Read(data, 0, 4);
                intData = StreamUtils.DecodeInt32(data);
                meta.SetMetaField("sample.SampleLoop[" + i + "].Type", intData.ToString(), readTagParams.ReadAllMetaFrames);

                // Start
                source.Read(data, 0, 4);
                intData = StreamUtils.DecodeInt32(data);
                meta.SetMetaField("sample.SampleLoop[" + i + "].Start", intData.ToString(), readTagParams.ReadAllMetaFrames);

                // End
                source.Read(data, 0, 4);
                intData = StreamUtils.DecodeInt32(data);
                meta.SetMetaField("sample.SampleLoop[" + i + "].End", intData.ToString(), readTagParams.ReadAllMetaFrames);

                // Fraction
                source.Read(data, 0, 4);
                intData = StreamUtils.DecodeInt32(data);
                meta.SetMetaField("sample.SampleLoop[" + i + "].Fraction", intData.ToString(), readTagParams.ReadAllMetaFrames);

                // Play count
                source.Read(data, 0, 4);
                intData = StreamUtils.DecodeInt32(data);
                meta.SetMetaField("sample.SampleLoop[" + i + "].PlayCount", intData.ToString(), readTagParams.ReadAllMetaFrames);
            }
        }

        public static bool IsDataEligible(MetaDataIO meta)
        {
            foreach (string key in meta.AdditionalFields.Keys)
            {
                if (key.StartsWith("sample.")) return true;
            }

            return false;
        }

        public static int ToStream(BinaryWriter w, bool isLittleEndian, MetaDataIO meta)
        {
            IDictionary<string, string> additionalFields = meta.AdditionalFields;
            w.Write(Utils.Latin1Encoding.GetBytes(CHUNK_SAMPLE));

            long sizePos = w.BaseStream.Position;
            w.Write(0); // Placeholder for chunk size that will be rewritten at the end of the method

            // Int values
            writeFieldIntValue("sample.manufacturer", additionalFields, w, 0);
            writeFieldIntValue("sample.product", additionalFields, w, 0);
            writeFieldIntValue("sample.period", additionalFields, w, 1);
            writeFieldIntValue("sample.MIDIUnityNote", additionalFields, w, 0);
            writeFieldIntValue("sample.MIDIPitchFraction", additionalFields, w, 0);
            writeFieldIntValue("sample.SMPTEFormat", additionalFields, w, 0);

            // SMPTE offset
            writeFieldIntValue("sample.SMPTEOffset.Hours", additionalFields, w, (sbyte)0);
            writeFieldIntValue("sample.SMPTEOffset.Minutes", additionalFields, w, (byte)0);
            writeFieldIntValue("sample.SMPTEOffset.Seconds", additionalFields, w, (byte)0);
            writeFieldIntValue("sample.SMPTEOffset.Frames", additionalFields, w, (byte)0);

            // == Sample loops

            // How many of them do we have ? -> count distinct indexes
            IList<string> keys = new List<string>();
            foreach(string s in additionalFields.Keys)
            {
                if (s.StartsWith("sample.SampleLoop")) {
                    string key = s.Substring(0, s.IndexOf("]") + 1);
                    if (!keys.Contains(key)) keys.Add(key);
                }
            }
            w.Write(keys.Count);

            // Sample loops data size
            long sampleLoopsPos = w.BaseStream.Position;
            w.Write(0); // Placeholder for data size that will be rewritten at the end of the method

            // Sample loops data
            foreach (string key in keys)
            {
                writeFieldIntValue(key+ ".CuePointId", additionalFields, w, 0);
                writeFieldIntValue(key + ".Type", additionalFields, w, 0);
                writeFieldIntValue(key + ".Start", additionalFields, w, 0);
                writeFieldIntValue(key + ".End", additionalFields, w, 0);
                writeFieldIntValue(key + ".Fraction", additionalFields, w, 0);
                writeFieldIntValue(key + ".PlayCount", additionalFields, w, 0);
            }

            // Write actual sample loops data size
            long finalPos = w.BaseStream.Position;
            w.BaseStream.Seek(sampleLoopsPos, SeekOrigin.Begin);
            w.Write((int)(finalPos - sampleLoopsPos - 4));

            // Write actual tag size
            w.BaseStream.Seek(sizePos, SeekOrigin.Begin);
            if (isLittleEndian)
            {
                w.Write((int)(finalPos - sizePos - 4));
            }
            else
            {
                w.Write(StreamUtils.EncodeBEInt32((int)(finalPos - sizePos - 4)));
            }

            return 10;
        }

        private static void writeFieldIntValue(string field, IDictionary<string, string> additionalFields, BinaryWriter w, object defaultValue)
        {
            if (additionalFields.Keys.Contains(field))
            {
                if (Utils.IsNumeric(additionalFields[field], true))
                {
                    if (defaultValue is int) w.Write(int.Parse(additionalFields[field]));
                    else if (defaultValue is byte) w.Write(byte.Parse(additionalFields[field]));
                    else if (defaultValue is sbyte) w.Write(sbyte.Parse(additionalFields[field]));
                    return;
                }
                else
                {
                    LogDelegator.GetLogDelegate()(Log.LV_WARNING, "'" + field + "' : error writing field - integer required; " + additionalFields[field] + " found");
                }
            }

            if (defaultValue is int) w.Write((int)defaultValue);
            else if (defaultValue is byte) w.Write((byte)defaultValue);
            else if (defaultValue is sbyte) w.Write((sbyte)defaultValue);
        }
    }
}
