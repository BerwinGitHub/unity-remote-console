using System;
using System.Text;
using System.Text.RegularExpressions;
using RConsole.Common;
using UnityEngine;

namespace RConsole.Editor
{

    // 简易日志入口：将日志写入 RConsoleMainWindow 所用的存储，并触发刷新显示
    public class LCLog
    {

        public static void Log(string message)
        {
            // Write(LogType.Log, message);
            Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            // Write(LogType.Warning, message);
            Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            // Write(LogType.Error, message);
            Debug.LogError(message);
        }

        public static void LogAssertion(string message)
        {
            // Write(LogType.Assert, message);
            Debug.LogAssertion(message);
        }

        public static void LogException(Exception exception)
        {
            // Write(LogType.Exception, exception.ToString());
            Debug.LogException(exception);
        }


        /// <summary>
        /// 从网络来的日志数据
        /// </summary>
        /// <param name="logModel"></param>
        public static void LogFromModel(LogModel logModel)
        {
            if (logModel == null) return;
            var deviceName = logModel.clientModel.deviceName ?? "Unknown";
            var deviceModel = logModel.clientModel.deviceModel ?? "-";
            var message = logModel.message;
            var stackTrace = logModel.stackTrace;
            // 使用 LogFormat 可以避免输出当前代码的堆栈，但 Unity Console 默认会自动收集堆栈
            // 只要不是 LogType.Exception，LogFormat 的表现基本符合预期
            // 为了完全替换堆栈，可能需要反射或自定义 Console 窗口，但这里我们尽量利用原生 Console
            var format = RConsoleConstants.LogPrefix + "{0}({1})>]: {2}\n\n{3}";
            // 处理堆栈信息，将文件路径转换为 Unity Console 可点击的超链接
            stackTrace = HyperlinkStackTrace(stackTrace);
            Debug.LogFormat(logModel.level, LogOption.NoStacktrace, null, format, deviceName, deviceModel, message, stackTrace);
        }



        private static string HyperlinkStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return string.Empty;

            // 匹配标准 Unity 堆栈格式: (at Path/To/File.cs:123)
            // 支持 Assets 目录以及 PackageCache 等其他目录
            // 捕获组 1: 文件路径
            // 捕获组 2: 行号
            string pattern = @"\(at\s+(.+?):(\d+)\)";

            return Regex.Replace(stackTrace, pattern, (match) =>
            {
                string path = match.Groups[1].Value;
                string line = match.Groups[2].Value;
                // 保持 (at ...) 格式，内部添加超链接
                return $"(at <a href=\"{path}\" line=\"{line}\">{path}:{line}</a>)";
            });
        }

        private static void Write(LogType level, string message, string tag = "RCLog")
        {
            RConsoleCtrl.Instance.Log(level, message, tag);
        }
    }
}