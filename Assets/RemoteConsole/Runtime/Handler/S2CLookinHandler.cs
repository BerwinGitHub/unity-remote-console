using RConsole.Common;
using UnityEngine;

namespace RConsole.Runtime
{
    public class S2CLookinHandler : IHandler
    {
        public override Envelope Handle(IBinaryModelBase modelBase)
        {
            var resp = (LookInReqModel)modelBase;
            Debug.Log($"[服务器] S2CLookin 收到请求: {resp.Path}");
            return null;
        }
    }
}