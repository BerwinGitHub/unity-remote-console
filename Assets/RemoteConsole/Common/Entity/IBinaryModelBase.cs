using System.IO;
using System.Text;

namespace RConsole.Common
{
    // 二进制解析基类：提供二进制读取工具
    public abstract class IBinaryModelBase
    {
        public abstract byte[] ToBinary();

        public abstract void FromBinary(BinaryReader br);

        public void FromBinary(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                FromBinary(br);
            }
        }

        protected string ReadString(BinaryReader br)
        {
            var len = br.ReadInt32();
            if (len < 0) return null;
            if (len == 0) return string.Empty;
            var bytes = br.ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }


        protected void WriteString(BinaryWriter bw, string s)
        {
            if (s == null)
            {
                bw.Write(-1);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(s);
            bw.Write(bytes.Length);
            if (bytes.Length > 0)
                bw.Write(bytes);
        }
    }

    #region 基本类型类

    /// <summary>
    /// 整数模型
    /// </summary>
    public class IntModel : IBinaryModelBase
    {
        public int Value;
        
        public IntModel(int value)
        {
            Value = value;
        }

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Value);
                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            Value = br.ReadInt32();
        }
    }

    /// <summary>
    /// 字符串模型
    /// </summary>
    public class StringModel : IBinaryModelBase
    {
        public string Value;

        public StringModel()
        {
        }

        public StringModel(string value)
        {
            Value = value;
        }

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                WriteString(bw, Value);
                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            Value = ReadString(br);
        }
    }

    /// <summary>
    /// 布尔模型
    /// </summary>
    public class BoolModel : IBinaryModelBase
    {
        public bool Value;

        public BoolModel(bool value)
        {
            Value = value;
        }

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Value);
                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            Value = br.ReadBoolean();
        }
    }

    /// <summary>
    /// 单精度浮点数模型
    /// </summary>
    public class FloatModel : IBinaryModelBase
    {
        public float Value;

        public FloatModel(float value)
        {
            Value = value;
        }

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Value);
                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            Value = br.ReadSingle();
        }
    }

    /// <summary>
    /// 长整数模型
    /// </summary>
    public class LongModel : IBinaryModelBase
    {
        public long Value;

        public LongModel(long value)
        {
            Value = value;
        }

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Value);
                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            Value = br.ReadInt64();
        }
    }

    #endregion
}