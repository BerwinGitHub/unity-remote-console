
using RConsole.Common;

namespace RConsole.Editor
{
    /// <summary>
    /// 握手处理
    /// </summary>
    public class LogHandler : IHandler
    {
        public override Envelope Handle(ClientModel clientModel, IBinaryModelBase modelBase)
        {
            var log = (LogModel)modelBase;
            if (clientModel != null) log.clientModel = clientModel;
            LCLog.ViewModel.Add(log);
            return null;
        }
    }
}
