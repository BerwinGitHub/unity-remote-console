
using RConsole.Common;

namespace RConsole.Editor
{
    /// <summary>
    /// 握手处理
    /// </summary>
    public class LookInHandler : IHandler
    {
        public override Envelope Handle(RConsoleConnection connection, IBinaryModelBase modelBase)
        {
            var lookInRespModel = modelBase as LookInRespModel;
            if (lookInRespModel == null) return null;
            LCLog.Log("客户端请求 Lookin");
            RConsoleCtrl.Instance.BringLookInToEditor(lookInRespModel);
            return null;
        }
    }
}
