using RConsole.Common;

namespace RConsole.Runtime
{
    public abstract class IHandler
    {
        public abstract Envelope Handle(IBinaryModelBase modelBase);
    }
}