namespace RConsole.Common
{
    public static class RConsoleConstants
    {
        // 日志前缀，用于区分远程日志和本地日志，防止递归捕获
        public const string LogPrefix = "[RC<";
    }
}
