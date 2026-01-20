using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RConsole.Common
{
    /// <summary>
    /// 响应查看界面数据
    /// </summary>
    public class LookInViewModel : IBinaryModelBase
    {
        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// 节点矩形区域
        /// </summary>
        public Rect Rect { get; set; } = new Rect();

        /// <summary>
        /// 组件列表
        /// </summary>
        public List<ComponentModel> Components { get; set; } = new List<ComponentModel>();

        /// <summary>
        /// 子节点列表
        /// </summary>
        public List<LookInViewModel> Children { get; set; } = new List<LookInViewModel>();

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Name);
                bw.Write(Path);
                bw.Write(IsActive);
                bw.Write(Rect.x);
                bw.Write(Rect.y);
                bw.Write(Rect.width);
                bw.Write(Rect.height);
                
                // Write Components
                bw.Write(Components.Count);
                foreach (var comp in Components)
                {
                    bw.Write(comp.ToBinary());
                }

                // Write Children
                bw.Write(Children.Count);
                foreach (var child in Children)
                {
                    bw.Write(child.ToBinary());
                }

                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            Name = br.ReadString();
            Path = br.ReadString();
            IsActive = br.ReadBoolean();
            Rect = new Rect(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            
            // Read Components
            var compCount = br.ReadInt32();
            Components.Clear();
            for (int i = 0; i < compCount; i++)
            {
                var comp = new ComponentModel();
                comp.FromBinary(br);
                Components.Add(comp);
            }

            // Read Children
            var childCount = br.ReadInt32();
            Children.Clear();
            for (int i = 0; i < childCount; i++)
            {
                var child = new LookInViewModel();
                child.FromBinary(br);
                Children.Add(child);
            }
        }
    }

    public class ComponentModel : IBinaryModelBase
    {
        public string TypeName { get; set; } = string.Empty;
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public byte[] ExtraData { get; set; } // For example, image png data

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(TypeName);
                bw.Write(Properties.Count);
                foreach (var kv in Properties)
                {
                    bw.Write(kv.Key);
                    bw.Write(kv.Value);
                }
                
                if (ExtraData != null)
                {
                    bw.Write(true);
                    bw.Write(ExtraData.Length);
                    bw.Write(ExtraData);
                }
                else
                {
                    bw.Write(false);
                }

                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            TypeName = br.ReadString();
            var count = br.ReadInt32();
            Properties.Clear();
            for (int i = 0; i < count; i++)
            {
                var key = br.ReadString();
                var value = br.ReadString();
                Properties[key] = value;
            }

            var hasExtra = br.ReadBoolean();
            if (hasExtra)
            {
                var len = br.ReadInt32();
                ExtraData = br.ReadBytes(len);
            }
            else
            {
                ExtraData = null;
            }
        }
    }
}