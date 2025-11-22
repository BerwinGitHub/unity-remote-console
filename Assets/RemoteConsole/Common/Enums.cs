namespace RConsole.Common
{

    // Envelope 的二进制类型标识
    public enum EnvelopeKind : byte
    {
        C2SHandshake = 1,
        C2SLogRecord = 2,
        C2SLookin = 3,
        S2CLookin = 4,
    }
}