using System;
using System.IO;
using System.Text;

namespace RConsole.Common
{

    [Serializable]
    public class ClientModel : IBinaryModelBase
    {
        public string deviceName;
        public string deviceModel;
        public string deviceId;
        public string platform;
        public string appName;
        public string appVersion;
        public string sessionId;

        [NonSerialized] public string connectID;
        [NonSerialized] public string address;
        [NonSerialized] public DateTime connectedAt;
        [NonSerialized] public bool isFiltered;

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteString(bw, deviceName);
                WriteString(bw, deviceModel);
                WriteString(bw, deviceId);
                WriteString(bw, platform);
                WriteString(bw, appName);
                WriteString(bw, appVersion);
                WriteString(bw, sessionId);
                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            deviceName = ReadString(br);
            deviceModel = ReadString(br);
            deviceId = ReadString(br);
            platform = ReadString(br);
            appName = ReadString(br);
            appVersion = ReadString(br);
            sessionId = ReadString(br);
        }
    }
}