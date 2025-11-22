

using System;
using RConsole.Common;

namespace RConsole.Editor
{
    /// <summary>
    /// 处理工厂
    /// </summary>
    public class HandlerFactory
    {
        public static IHandler CreateHandler(EnvelopeKind kind)
        {
            return kind switch
            {
                EnvelopeKind.C2SHandshake => new HandshakeHandler(),
                EnvelopeKind.C2SLogRecord => new LogHandler(),
                EnvelopeKind.C2SLookin => new LookInHandler(),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
}