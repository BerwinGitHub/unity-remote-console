using System;
using System.IO;
using RConsole.Common;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RConsole.Editor
{
    // 独立的 WebSocket 服务实现，直接在行为类中处理逻辑
    public class RConsoleConnection : WebSocketBehavior
    {
        private readonly object _lock = new object();
        private ClientModel _client = new ClientModel();
        public ClientModel ClientModel => _client;

        protected override void OnOpen()
        {
            var id = ID;
            var remoteStr = UserEndPoint.Address.ToString();
            MainThreadDispatcher.Enqueue(() =>
            {
                lock (_lock)
                {
                    _client = new ClientModel
                    {
                        connectID = id,
                        address = remoteStr,
                        connectedAt = DateTime.UtcNow
                    };
                }

                LCLog.Log($"[服务]客户端连接：{remoteStr} (id={id})");
            });
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var id = ID;
            try
            {
                if (e.IsBinary)
                {
                    var data = e.RawData; // 捕获快照
                    MainThreadDispatcher.Enqueue(() => ProcessEnvelopeBinary(id, data));
                }
                else if (e.IsText)
                {
                    var json = e.Data;
                    MainThreadDispatcher.Enqueue(() => ProcessEnvelopeJson(id, json));
                }
            }
            catch (Exception ex)
            {
                MainThreadDispatcher.Enqueue(() => LCLog.LogWarning($"[服务]客户端消息处理错误：{ex.Message}"));
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            var id = ID;
            MainThreadDispatcher.Enqueue(() =>
            {
                RConsoleCtrl.Instance.RemoveConnectedClient(this);
                _client = null;
                LCLog.Log($"[服务]客户端断开：id={id} ({e.Code})");
            });
        }

        private void ProcessEnvelopeJson(string sessionId, string json)
        {
            try
            {
                var env = JsonUtility.FromJson<Envelope>(json);
                if (env == null) return;
                HandleEnvelope(sessionId, env);
            }
            catch (Exception ex)
            {
                LCLog.LogError($"[服务]客户端 JSON 解析错误：{ex.Message}");
            }
        }

        private void ProcessEnvelopeBinary(string sessionId, byte[] data)
        {
            try
            {
                Envelope envelope = null;

#if RC_USE_GOOGLE_PROTOBUF
                // 使用 Google.Protobuf 生成的类型进行解析（需提供 csharp 生成文件与命名空间）
                // 假设命名空间为 RemoteConsoleProto，类型为 Envelope
                // var pbEnv = RemoteConsoleProto.Envelope.Parser.ParseFrom(data);
                // env = ConvertFromProto(pbEnv);
                R.LogWarning("RC_USE_GOOGLE_PROTOBUF 启用后请提供 ConvertFromProto 与生成的 C# 类型。");
#else
                envelope = new Envelope(data);
#endif

                HandleEnvelope(sessionId, envelope);
            }
            catch (EndOfStreamException)
            {
                LCLog.LogError($"[服务]客户端二进制解析错误：unexpected end of stream");
            }
            catch (Exception ex)
            {
                LCLog.LogError($"[服务]客户端二进制解析错误：{ex.Message}");
            }
        }

        private void HandleEnvelope(string sessionId, Envelope env)
        {
            if (env.Kind == EnvelopeKind.C2SHandshake)
            {
                var clientInfo = (ClientModel)env.Model;
                if (_client != null)
                {
                    _client.deviceName = clientInfo.deviceName;
                    _client.deviceModel = clientInfo.deviceModel;
                    _client.deviceId = clientInfo.deviceId;
                    _client.platform = clientInfo.platform;
                    _client.appName = clientInfo.appName;
                    _client.appVersion = clientInfo.appVersion;
                    _client.sessionId = clientInfo.sessionId;
                }
                LCLog.Log($"[服务]握手成功：{clientInfo.deviceName} ");
            }
            var handler = HandlerFactory.CreateHandler(env.Kind);
            var respEnv = handler?.Handle(this, env.Model);
            if (respEnv == null) return;
            Send(respEnv.ToBinary());
        }

        public void SendEnvelop(Envelope env)
        {
            Send(env.ToBinary());
        }
    }
}