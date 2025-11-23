using System;
using System.IO;
using System.Text;
using UnityEngine.Serialization;

namespace RConsole.Common
{
    [Serializable]
    public class Envelope : IBinaryModelBase
    {

        /// <summary>
        /// seq 下表记录，用于响应消息匹配
        /// </summary>
        public static int SeqIncrement = 0;

        /// <summary>
        /// 消息ID
        /// </summary>
        public int SeqId;

        /// <summary>
        /// 是否是响应消息
        /// </summary>
        public bool IsResponse;

        /// <summary>
        /// 消息类型
        /// </summary>
        public EnvelopeKind Kind;

        /// <summary>
        /// 对应模块子命令
        /// </summary>
        public byte SubKind;

        /// <summary>
        /// 消息数据
        /// </summary>
        // public IBinaryModelBase Model = null;

        /// <summary>
        /// 消息数据
        /// </summary>
        public byte[] Data;

        public Envelope(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                FromBinary(br);
            }
        }

        public Envelope(EnvelopeKind kind, byte subKind, byte[] data)
        {
            // 随机生成一个不重复的 id
            SeqId = GetNextSeqId();
            Kind = kind;
            SubKind = subKind;
            Data = data;
            // Model = model;
        }

        public override void FromBinary(BinaryReader br)
        {
            SeqId = br.ReadInt32();
            IsResponse = br.ReadBoolean();
            Kind = (EnvelopeKind)br.ReadByte();
            SubKind = br.ReadByte();
            Data = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
            // Model = EnvelopeFactory.Create(Kind);
            // Model.FromBinary(br);
        }

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(SeqId);
                bw.Write(IsResponse);
                bw.Write((byte)Kind);
                bw.Write(SubKind);
                bw.Write(Data);
                // var bytes = Model.ToBinary();
                // bw.Write(bytes);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 获取下一个 seq id
        /// </summary>
        /// <returns></returns>
        public static int GetNextSeqId()
        {
            // 每次递增 1，越界后从 0 开始
            SeqIncrement = (SeqIncrement + 1) % int.MaxValue;
            return SeqIncrement;
        }
    }
}