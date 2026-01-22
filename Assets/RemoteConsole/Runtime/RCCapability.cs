using System;
using RConsole.Common;
using UnityEngine;

namespace RConsole.Runtime
{
    // 单例日志管理器：负责连接远端控制台与转发 Unity 日志
    public class RCCapability
    {
        private static RCCapability _instance;
        public static RCCapability Instance => _instance ??= new RCCapability();

        private RConsoleClient _client;
        public RConsoleClient WebSocket => _client;
        private bool _capturingLogs;
        public bool IsCapturingLogs => _capturingLogs;

        public string PathRoot { get; private set; } = "/";

        private RCCapability()
        {
            // 允许默认配置，必要时由 Init 指定
        }

        public void Initialize(string pathRoot)
        {
            PathRoot = pathRoot;
        }
        

        /// <summary>
        /// 初始化并连接到服务器。
        /// </summary>
        public async void Connect(string ip, int port = 13337, string path = "/remote-console")
        {
            // 释放旧连接
            _client?.Disconnect();

            _client = new RConsoleClient
            {
                Host = ip,
                Port = port,
                Path = path
            };
            Debug.Log($"RCLogManager Connect to {ip}:{port}{path}");
            // 由 RCLogManager 自行决定是否捕获 Unity 日志，因此此处不启用捕获
            await _client.Connect();
            _client.On(EnvelopeKind.C2SHandshake, (byte)SubHandshake.Handshake, model =>
            {
                var s = model as BoolModel;
                if (s != null)
                {
                    if (s.Value)
                    {
                        CaptureLog();
                    } else {
                        EscapeLog();
                    }
                }
                return null;
            });
        }

        public async void Disconnect()
        {
            try
            {
                _client?.Disconnect();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"RCLogManager 断开连接时发生异常: {ex.Message}");
            }
            finally
            {
                _client = null;
            }
        }

        /// <summary>
        /// 是否已连接到服务器。
        /// </summary>
        public bool IsConnected => _client != null && _client.IsConnected;

        /// <summary>
        /// 开启 Unity 日志转发（通过 RCLogManager 捕获并发送）。
        /// </summary>
        public void CaptureLog()
        {
            if (_capturingLogs) return;
            Application.logMessageReceivedThreaded += OnUnityLogMessage;
            _capturingLogs = true;
        }

        /// <summary>
        /// 关闭 Unity 日志转发。
        /// </summary>
        public void EscapeLog()
        {
            if (!_capturingLogs) return;
            Application.logMessageReceivedThreaded -= OnUnityLogMessage;
            _capturingLogs = false;
        }

        /// <summary>
        /// 主动停止并释放连接。
        /// </summary>
        public void Stop()
        {
            try
            {
                EscapeLog();
                _client?.Disconnect();
            }
            catch
            {
            }
            finally
            {
                _client = null;
            }
        }

        public void Update()
        {
            _client?.Update();
        }

        /// <summary>
        /// 发送业务日志到服务器。
        /// </summary>
        public void SendLog(LogModel log)
        {
            if (log == null) return;
            try
            {
                // _client?.Send(new Envelope(EnvelopeKind.C2SLog, (byte)SubLog.Log, log));
                _client?.Reqeust(EnvelopeKind.C2SLog, (byte)SubLog.Log, log);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"RCLogManager send failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理 Unity 日志（线程回调）。
        /// </summary>
        private void OnUnityLogMessage(string logString, string stackTrace, LogType type)
        {
            // 防止递归：忽略由 RemoteConsole 自身在编辑器中输出的日志
            if (!string.IsNullOrEmpty(logString) && logString.StartsWith(RConsoleConstants.LogPrefix)) return;

            var lr = new LogModel
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                level = type,
                tag = "Unity",
                message = logString,
                stackTrace = stackTrace ?? string.Empty,
                threadId = System.Threading.Thread.CurrentThread.ManagedThreadId
            };
            SendLog(lr);
        }
    }
}
