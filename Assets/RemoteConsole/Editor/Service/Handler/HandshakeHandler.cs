
using RConsole.Common;

namespace RConsole.Editor
{
    /// <summary>
    /// 握手处理
    /// </summary>
    public class HandshakeHandler : IHandler
    {
        public override Envelope Handle(RConsoleConnection connection, IBinaryModelBase modelBase)
        {
            var clientInfo = connection.ClientModel;
            LCLog.Log(
                $"[服务]客户端握手：{clientInfo.deviceId} {clientInfo.platform} {clientInfo.appName} {clientInfo.appVersion}");
            RConsoleCtrl.Instance.AddConnectedClient(connection);
            return null;
        }
    }
}
