using System;
using System.Collections.Generic;
using RConsole.Common;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using RConsole.Runtime;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Globalization;

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


        private Dictionary<string, ClientModel> _connections = new Dictionary<string, ClientModel>();

        private RConsoleCtrl()
        {
            _server = new RConsoleServer();
        }

        public void OnEnable()
        {
            RConsoleServer.On(EnvelopeKind.C2SLog, (byte)SubLog.Log, OnLogReceived);
            RConsoleServer.On(EnvelopeKind.C2SHandshake, (byte)SubHandshake.Handshake, OnHandshakeReceived);
            
            RemoteNodeSync.OnSyncRequest += OnNodeSyncRequest;
        }

        public void OnDisable()
        {
            RConsoleServer.Off(EnvelopeKind.C2SLog, (byte)SubLog.Log, OnLogReceived);
            RConsoleServer.Off(EnvelopeKind.C2SHandshake, (byte)SubHandshake.Handshake, OnHandshakeReceived);
            
            RemoteNodeSync.OnSyncRequest -= OnNodeSyncRequest;
        }

        private void OnNodeSyncRequest(int instanceID, string type, string value)
        {
            var connection = GetSelectConnection();
            if (connection == null) return;

            var body = new StringModel($"{instanceID}|{type}|{value}");
            Debug.Log($"OnNodeSyncRequest cmd: {body.Value}");
            connection.Reqeust(EnvelopeKind.S2CLookIn, (byte)SubLookIn.SyncNode, body, null);
        }

        public void Connect()
        {
            _server.Start();
        }

        public void Disconnect()
        {
            _server.Stop();
        }

        #region 网络相关 - 主动请求相关数据

        /// <summary>
        /// 请求查看当前连接的客户端信息
        /// </summary>
        public void FetchLookin()
        {
            var connection = GetSelectConnection();
            var body = new StringModel("/");
            connection?.Reqeust(EnvelopeKind.S2CLookIn, (byte)SubLookIn.LookIn, body, model =>
            {
                var lookInRespModel = model as LookInViewModel;
                if (lookInRespModel == null) return;
                LCLog.Log("服务端请求 Lookin 数据成功返回");
                BringLookInToEditor(lookInRespModel);
            });
        }

        public void FetchDirectory(FileModel body)
        {
            var connection = GetSelectConnection();
            connection?.Reqeust(EnvelopeKind.S2CFile, (byte)SubFile.FetchDirectory, body, model =>
            {
                var resp = model as FileModel;
                if (resp == null) return;
                UpdateFileBrowser(resp);
            });
        }


        public void RequestFileMD5(FileModel body)
        {
            var connection = GetSelectConnection();
            connection?.Reqeust(EnvelopeKind.S2CFile, (byte)SubFile.MD5, body, model =>
            {
                var resp = model as FileModel;
                if (resp == null) return;
                OnFileMD5Changed?.Invoke(resp);
            });
        }

        public void DownloadFile(FileModel body)
        {
            var connection = GetSelectConnection();
            if (connection == null) return;

            string fileName = System.IO.Path.GetFileName(body.Path);
            string extension = System.IO.Path.GetExtension(fileName).TrimStart('.');
            string savePath = EditorUtility.SaveFilePanel("保存文件", "", fileName, extension);

            if (string.IsNullOrEmpty(savePath)) return;

            LCLog.Log($"开始下载文件: {body.Path} ...");

            connection.Reqeust(EnvelopeKind.S2CFile, (byte)SubFile.Download, body, model =>
            {
                var resp = model as FileModel;
                if (resp == null || resp.Data == null)
                {
                    LCLog.LogError("下载文件失败或文件为空");
                    return;
                }

                try
                {
                    System.IO.File.WriteAllBytes(savePath, resp.Data);
                    LCLog.Log($"文件已保存到: {savePath}");
                    EditorUtility.RevealInFinder(savePath);
                }
                catch (Exception ex)
                {
                    LCLog.LogError($"保存文件失败: {ex.Message}");
                }
            });
        }

        public RConsoleConnection GetSelectConnection()
        {
            if (!_server.IsStarted)
            {
                // LCLog.LogWarning("服务未启动");
                return null;
            }

            var selectModel = ViewModel.FilterClientModel;
            if (selectModel == null)
            {
                // LCLog.LogWarning("未选择客户端");
                return null;
            }

            if (!RConsoleServer.Connections.TryGetValue(selectModel.connectID, out var connection))
            {
                LCLog.LogWarning("未找到选择的客户端连接");
                return null;
            }

            return connection;
        }

        #endregion

        #region 网络相关 - 被动监听回调

        public Envelope OnLogReceived(RConsoleConnection connection, IBinaryModelBase model)
        {
            var logModel = model as LogModel;
            if (logModel == null) return null;
            var clientModel = connection.ClientModel;
            if (clientModel != null) logModel.clientModel = clientModel;
            LCLog.LogFromModel(logModel);
            return null;
        }

        public Envelope OnHandshakeReceived(RConsoleConnection connection, IBinaryModelBase model)
        {
            var handshakeModel = model as ClientModel;
            AddConnectedClient(handshakeModel);
            return null;
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
            // ViewModel.Add(log);
        }

        public void AddConnectedClient(ClientModel model)
        {
            _connections[model.connectID] = model;
            _model.AddConnectedClient(model);
        }

        public void RemoveConnectedClient(ClientModel model)
        {
            _connections.Remove(model.connectID);
            _model.RemoveConnectedClient(model);
        }

        public void SetServerStarted(bool started)
        {
            _model.SetServerStarted(started);
            if (started)
            {
                OnEnable();
            }
            else
            {
                OnDisable();
            }
        }

        public void ServerDisconnected()
        {
            _model.ServerDisconnected();
        }

        public void SetFilterClientInfoModel(ClientModel client)
        {
            _fileRoot = null;
            OnFileBrowserChanged?.Invoke(_fileRoot);
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
        public void BringLookInToEditor(LookInViewModel lookInViewModel)
        {
            if (lookInViewModel == null) return;
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
            // 添加 Canvas 以便 UI 组件正常显示
            var canvas = lookinRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            lookinRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
            lookinRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            Undo.RegisterCreatedObjectUndo(lookinRoot, "Create LookIn Root");

            BuildEditorNodes(lookinRoot.transform, lookInViewModel);
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
            else
            {
                LCLog.LogWarning("Lookin 视图只能在编辑模式下添加");
            }
            LCLog.Log($"当前设备: {connection.ClientModel.deviceName}，已将 Lookin 视图添加到场景中");
        }

        private bool TryParseUnityColor(string colorStr, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(colorStr)) return false;
            // Unity ToString format: RGBA(r, g, b, a)
            if (colorStr.StartsWith("RGBA("))
            {
                var content = colorStr.Substring(5, colorStr.Length - 6);
                var parts = content.Split(',');
                if (parts.Length == 4)
                {
                    if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var r) &&
                        float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var g) &&
                        float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var b) &&
                        float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
                    {
                        color = new Color(r, g, b, a);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TryParseVector2(string str, out Vector2 v)
        {
            v = Vector2.zero;
            if (string.IsNullOrEmpty(str)) return false;
            str = str.Trim();
            if (str.StartsWith("(") && str.EndsWith(")"))
            {
                str = str.Substring(1, str.Length - 2);
            }
            var parts = str.Split(',');
            if (parts.Length >= 2)
            {
                if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) && 
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                {
                    v = new Vector2(x, y);
                    return true;
                }
            }
            return false;
        }

        private bool TryParseVector3(string str, out Vector3 v)
        {
            v = Vector3.zero;
            if (string.IsNullOrEmpty(str)) return false;
            str = str.Trim();
            if (str.StartsWith("(") && str.EndsWith(")"))
            {
                str = str.Substring(1, str.Length - 2);
            }
            var parts = str.Split(',');
            if (parts.Length >= 3)
            {
                if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) && 
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) && 
                    float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                {
                    v = new Vector3(x, y, z);
                    return true;
                }
            }
            return false;
        }

        private void BuildEditorNodes(Transform parent, LookInViewModel model)
        {
            var go = new GameObject(string.IsNullOrEmpty(model.Name) ? "Node" : model.Name);
            go.SetActive(model.IsActive);
            Undo.RegisterCreatedObjectUndo(go, "Create LookIn Node");
            
            // 必须先添加 RectTransform (它会替换默认的 Transform)，然后再获取 transform 引用
            var rt = go.AddComponent<RectTransform>();
            // 默认值，防止没找到数据时缩成一点
            rt.sizeDelta = new Vector2(model.Rect.width, model.Rect.height);

            var t = go.transform;
            t.SetParent(parent, false);
            
            // Add Sync Component
            var sync = go.AddComponent<RemoteNodeSync>();
            sync.RuntimeInstanceID = model.InstanceID;

            if (model.Components != null && model.Components.Count > 0)
            {
                var remoteData = go.AddComponent<RemoteNodeData>();
                remoteData.Components = model.Components;

                foreach (var comp in model.Components)
                {
                    Component target = null;
                    
                    // 1. 获取或创建组件
                    if (comp.TypeName == "Transform" || comp.TypeName == "RectTransform")
                    {
                        target = t;
                    }
                    else
                    {
                        // 尝试通过全名加载类型
                        Type type = null;
                        if (!string.IsNullOrEmpty(comp.FullTypeName))
                        {
                            type = GetTypeByFullName(comp.FullTypeName);
                        }
                        
                        if (type != null)
                        {
                            // 避免重复添加 (有些组件可能互斥或已存在)
                            target = go.GetComponent(type);
                            if (target == null)
                            {
                                try { target = go.AddComponent(type); } catch { }
                            }
                        }
                        
                        // 降级策略：如果是已知 UI 组件但没找到类型（不太可能），尝试硬编码
                        if (target == null)
                        {
                            if (comp.TypeName == "Image") target = go.AddComponent<UnityEngine.UI.Image>();
                            else if (comp.TypeName == "Text") target = go.AddComponent<UnityEngine.UI.Text>();
                            else if (comp.TypeName == "RawImage") target = go.AddComponent<UnityEngine.UI.RawImage>();
                        }
                    }

                    // 2. 特殊处理：Image Sprite 还原
                    if (comp.TypeName == "Image" && target is UnityEngine.UI.Image img && comp.ExtraData != null && comp.ExtraData.Length > 0)
                    {
                        var tex = new Texture2D(2, 2);
                        if (tex.LoadImage(comp.ExtraData))
                        {
                            Vector4 border = Vector4.zero;
                            if (comp.Properties.TryGetValue("SpriteBorder", out var borderStr))
                            {
                                var val = ParseValue(typeof(Vector4), borderStr);
                                if (val != null) border = (Vector4)val;
                            }

                            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, border);
                            sprite.name = comp.Properties.TryGetValue("Sprite", out var spriteName) ? spriteName : "RemoteSprite";
                            img.sprite = sprite;
                        }
                    }

                    // 3. 通用反射属性赋值
                    if (target != null)
                    {
                        ApplyProperties(target, comp.Properties);
                    }
                }
            }

            // 初始化同步组件状态，避免初始数据不一致导致的误同步
            sync.Initialize();

            var children = model.Children;
            if (children != null)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    BuildEditorNodes(t, children[i]);
                }
            }
        }

        private Type GetTypeByFullName(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }

        private void ApplyProperties(Component c, Dictionary<string, string> props)
        {
            var type = c.GetType();
            foreach (var kv in props)
            {
                try
                {
                    // 尝试属性
                    var p = type.GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance);
                    if (p != null && p.CanWrite)
                    {
                        var val = ParseValue(p.PropertyType, kv.Value);
                        if (val != null) p.SetValue(c, val, null);
                        continue;
                    }
                    
                    // 尝试字段
                    var f = type.GetField(kv.Key, BindingFlags.Public | BindingFlags.Instance);
                    if (f != null)
                    {
                        var val = ParseValue(f.FieldType, kv.Value);
                        if (val != null) f.SetValue(c, val);
                    }
                }
                catch { }
            }
        }

        private object ParseValue(Type type, string valStr)
        {
            try
            {
                if (type == typeof(string)) return valStr;
                if (type == typeof(bool)) return bool.Parse(valStr);
                if (type == typeof(int)) return int.Parse(valStr, CultureInfo.InvariantCulture);
                if (type == typeof(float)) return float.Parse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture);
                if (type.IsEnum) return Enum.Parse(type, valStr);
                if (type == typeof(Vector2)) { return TryParseVector2(valStr, out var v) ? (object)v : null; }
                if (type == typeof(Vector3)) { return TryParseVector3(valStr, out var v) ? (object)v : null; }
                if (type == typeof(Color)) { return TryParseUnityColor(valStr, out var v) ? (object)v : null; }
                if (type == typeof(Vector4)) 
                {
                    // 简单解析 Vector4 (x,y,z,w)
                    var clean = valStr.Replace("(", "").Replace(")", "").Trim();
                    var parts = clean.Split(',');
                    if (parts.Length == 4) return new Vector4(
                        float.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture), 
                        float.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture), 
                        float.Parse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture), 
                        float.Parse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture));
                }
                if (type == typeof(Rect))
                {
                    // Rect (x:0.00, y:0.00, width:0.00, height:0.00) or similar
                    // 简单起见，如果解析失败则忽略，或者尝试正则提取。
                    // Unity 的 Rect.ToString 格式：(x:X, y:Y, width:W, height:H)
                    // 但我们这里可以尝试更简单的处理，或者如果遇到复杂的就不支持
                    // 暂时不支持 Rect 的反序列化，因为格式比较复杂且不统一
                }
            }
            catch { }
            return null;
        }

        #endregion

        private FileModel _fileRoot;
        public FileModel FileRoot => _fileRoot;
        public Action<FileModel> OnFileBrowserChanged;
        public Action<FileModel> OnFileMD5Changed;

        public void UpdateFileBrowser(FileModel resp)
        {
            if (resp == null) return;
            if (_fileRoot == null)
            {
                _fileRoot = resp;
            }
            else
            {
                var isRootResp = string.Equals(resp.Path, _fileRoot.Path, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(resp.Path, _fileRoot.RootPath,
                                     StringComparison.OrdinalIgnoreCase);
                if (isRootResp)
                {
                    _fileRoot = resp;
                }
                else
                {
                    var target = FindNodeByPath(_fileRoot, resp.Path);
                    if (target != null)
                    {
                        target.Name = resp.Name;
                        target.IsDirectory = resp.IsDirectory;
                        target.Length = resp.Length;
                        target.LastWriteTime = resp.LastWriteTime;
                        target.Children = resp.Children ?? new List<FileModel>();
                    }
                }
            }

            OnFileBrowserChanged?.Invoke(_fileRoot);
        }

        private FileModel FindNodeByPath(FileModel node, string path)
        {
            if (node == null) return null;
            if (string.Equals(node.Path, path, StringComparison.OrdinalIgnoreCase)) return node;
            if (node.Children == null) return null;
            for (int i = 0; i < node.Children.Count; i++)
            {
                var found = FindNodeByPath(node.Children[i], path);
                if (found != null) return found;
            }

            return null;
        }
    }
}