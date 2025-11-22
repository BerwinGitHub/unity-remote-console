

using System;

namespace RConsole.Common
{
    public class EnvelopeFactory
    {
        public static IBinaryModelBase Create(EnvelopeKind kind)
        {
            return kind switch
            {
                EnvelopeKind.C2SHandshake => new ClientModel(),
                EnvelopeKind.C2SLogRecord => new LogModel(),
                EnvelopeKind.S2CLookin => new LookInReqModel(),
                EnvelopeKind.C2SLookin => new LookInRespModel(),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
}