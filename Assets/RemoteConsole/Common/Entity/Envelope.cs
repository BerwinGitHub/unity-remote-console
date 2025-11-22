using System;
using System.IO;
using System.Text;

namespace RConsole.Common
{

    [Serializable]
    public class Envelope : IBinaryModelBase
    {

        public EnvelopeKind Kind;

        public IBinaryModelBase Model = null;

        public Envelope(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                FromBinary(br);
            }
        }

        public Envelope(EnvelopeKind kind, IBinaryModelBase model)
        {
            Kind = kind;
            Model = model;
        }
        
        public override void FromBinary(BinaryReader br)
        {
            Kind = (EnvelopeKind)br.ReadByte();
            Model = EnvelopeFactory.Create(Kind);
            Model.FromBinary(br);
        }

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write((byte)Kind);
                var bytes = Model.ToBinary();
                bw.Write(bytes);
                return ms.ToArray();
            }
        }
    }
}