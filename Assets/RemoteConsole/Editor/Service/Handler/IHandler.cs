using RConsole.Common;

namespace RConsole.Editor
{
    /// <summary>
    /// 处理接口
    /// </summary>
    public abstract class IHandler
    {
        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="clientInfo">客户端信息</param>
        /// <param name="envelope">信封</param>
        /// <returns>处理后需要绘制的信封</returns>
        public abstract Envelope Handle(ClientModel clientInfo, IBinaryModelBase modelBase);
    }
}