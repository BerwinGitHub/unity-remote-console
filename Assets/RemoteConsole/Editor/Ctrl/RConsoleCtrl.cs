using System;
using System.Collections.Generic;
using RConsole.Common;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace RConsole.Editor
{
    /// <summary>
    /// 控制台控制器
    /// </summary>
    public class RConsoleCtrl
    {
        private static RConsoleCtrl _instance;
        public static RConsoleCtrl Instance => _instance ??= new RConsoleCtrl();

        private readonly RConsoleServer _server;

        private RConsoleViewModel _model;
        public RConsoleViewModel ViewModel => _model ??= new RConsoleViewModel();


        private Dictionary<string, RConsoleConnection> _connections = new Dictionary<string, RConsoleConnection>();

        private RConsoleCtrl()
        {
            _server = new RConsoleServer();
        }

        public void Connect()
        {
            _server.Start();
        }

        public void Disconnect()
        {
            _server.Stop();
        }

        #region 网络相关

        /// <summary>
        /// 请求查看当前连接的客户端信息
        /// </summary>
        public void FetchLookin()
        {
            var connection = GetSelectConnection();
            connection?.SendEnvelop(new Envelope(EnvelopeKind.S2CLookin, new LookInReqModel("/")));
        }

        public void FetchDirectory(string path = "/")
        {
        }

        private RConsoleConnection GetSelectConnection()
        {
            if (!_server.IsStarted)
            {
                LCLog.LogWarning("服务未启动");
                return null;
            }
            var selectModel = ViewModel.FilterClientModel;
            if (selectModel == null)
            {
                LCLog.LogWarning("未选择客户端");
                return null;
            }

            if (!_connections.TryGetValue(selectModel.connectID, out var connection))
            {
                LCLog.LogWarning("未找到选择的客户端连接");
                return null;
            }

            return connection;
        }

        #endregion

        public void Log(LogType level, string message, string tag = "RCLog")
        {
            var model = new LogModel
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                level = (LogType)(int)level,
                tag = tag,
                message = message,
                stackTrace = Environment.StackTrace,
                threadId = System.Threading.Thread.CurrentThread.ManagedThreadId
            };
            Log(model);
        }

        public void Log(LogModel log)
        {
            ViewModel.Add(log);
        }

        public void AddConnectedClient(RConsoleConnection connection)
        {
            _connections[connection.ClientModel.connectID] = connection;
            _model.AddConnectedClient(connection.ClientModel);
        }

        public void RemoveConnectedClient(RConsoleConnection connection)
        {
            _connections.Remove(connection.ClientModel.connectID);
            _model.RemoveConnectedClient(connection.ClientModel);
        }

        public void SetServerStarted(bool started)
        {
            _model.SetServerStarted(started);
        }

        public void ServerDisconnected()
        {
            _model.ServerDisconnected();
        }

        public void SetFilterClientInfoModel(ClientModel client)
        {
            _model.SetFilterClientInfoModel(client);
        }

        public void ClearLog()
        {
            _model.Clear();
        }

        #region Lookin相关

        /// <summary>
        /// .bring-lookin-to-editor
        /// </summary>
        public void BringLookInToEditor(LookInRespModel lookInRespModel)
        {
            if (lookInRespModel == null) return;
            var scene = SceneManager.GetActiveScene();
            GameObject lookinRoot = null;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "LookIn")
                {
                    lookinRoot = roots[i];
                    break;
                }
            }

            if (lookinRoot != null)
            {
                Undo.DestroyObjectImmediate(lookinRoot);
            }

            var connection = GetSelectConnection();
            lookinRoot = new GameObject($"LookIn({connection.ClientModel.deviceName})");
            Undo.RegisterCreatedObjectUndo(lookinRoot, "Create LookIn Root");

            BuildEditorNodes(lookinRoot.transform, lookInRespModel);
            EditorSceneManager.MarkSceneDirty(scene);
            LCLog.Log($"当前设备: {connection.ClientModel.deviceName}，已将 Lookin 视图添加到场景中");
        }

        private void BuildEditorNodes(Transform parent, LookInRespModel model)
        {
            var go = new GameObject(string.IsNullOrEmpty(model.Name) ? "Node" : model.Name);
            go.SetActive(model.IsActive);
            Undo.RegisterCreatedObjectUndo(go, "Create LookIn Node");
            var t = go.transform;
            t.SetParent(parent, false);
            var children = model.Children;
            if (children != null)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    BuildEditorNodes(t, children[i]);
                }
            }
        }

        #endregion
    }
}