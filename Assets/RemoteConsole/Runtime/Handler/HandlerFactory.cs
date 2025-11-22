using System;
using RConsole.Common;

namespace RConsole.Runtime
{
    public static class HandlerFactory
    {
        public static IHandler CreateHandler(EnvelopeKind kind)
        {
            switch (kind)
            {
                case EnvelopeKind.S2CLookin:
                    return new S2CLookinHandler();
                default:
                    return null;
            }
        }
    }
}