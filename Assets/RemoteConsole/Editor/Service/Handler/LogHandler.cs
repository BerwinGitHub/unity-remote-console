
using RConsole.Common;

namespace RConsole.Editor
{
    /// <summary>
    /// 握手处理
    /// </summary>
    public class LogHandler : IHandler
    {
        public override Envelope Handle(RConsoleConnection connection, IBinaryModelBase modelBase)
        {
            var clientModel = connection.ClientModel;
            var log = (LogModel)modelBase;
            if (clientModel != null) log.clientModel = clientModel;
            RConsoleCtrl.Instance.Log(log);
            return null;
        }
    }
}
