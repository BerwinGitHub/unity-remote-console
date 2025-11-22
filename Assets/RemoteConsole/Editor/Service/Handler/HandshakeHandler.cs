
using RConsole.Common;

namespace RConsole.Editor
{
    /// <summary>
    /// 握手处理
    /// </summary>
    public class HandshakeHandler : IHandler
    {
        public override Envelope Handle(ClientModel clientInfo, IBinaryModelBase modelBase)
        {
            LCLog.Log(
                $"[服务]客户端握手：{clientInfo.deviceId} {clientInfo.platform} {clientInfo.appName} {clientInfo.appVersion}");
            LCLog.ViewModel.AddConnectedClient(clientInfo);
            return null;
        }
    }
}
