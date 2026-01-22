using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace RConsole.Common
{

    // 与 proto 对应的简单模型（在未启用 protobuf 运行时的情况下用于占位）
    [Serializable]
    public class LogModel : IBinaryModelBase
    {
        public long timestamp;
        public LogType level; // 0:Log,1:Warning,2:Error,3:Exception,4:Assert
        public string tag;
        public string message;
        public string stackTrace;
        public int threadId;

        [FormerlySerializedAs("clientInfoModel")] [NonSerialized] public ClientModel clientModel;

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(timestamp);
                bw.Write((int)level);
                WriteString(bw, tag);
                WriteString(bw, message);
                WriteString(bw, stackTrace);
                bw.Write(threadId);
                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            timestamp = br.ReadInt64();
            level = (LogType)br.ReadInt32();
            tag = ReadString(br);
            message = ReadString(br);
            stackTrace = ReadString(br);
            threadId = br.ReadInt32();
        }
    }
}