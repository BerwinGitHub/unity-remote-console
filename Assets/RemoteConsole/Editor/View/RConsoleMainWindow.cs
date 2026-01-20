using System;
using System.Collections.Generic;
using System.Linq;
using RConsole.Common;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RConsole.Editor
{
    public class RConsoleMainWindow : EditorWindow
    {
        // Tabs 定义
        private enum TabType
        {
            LookIn,
            FileBrowser
        }
        private TabType _currentTab = TabType.LookIn;
        private readonly string[] _tabNames = { "LookIn", "File Browser" };

        private static RConsoleMainWindow _selfWindows;

        // File Browser Fields
        private RConsoleTreeView _tree;
        private TreeViewState _treeState;
        private FileModel _root;
        private RConsoleTreeViewItem _selecct;
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private float _splitLeft = 260f;
        private bool _resizing;

        [MenuItem("Window/Remote Console")]
        public static void ShowWindow()
        {
            var win = GetWindow<RConsoleMainWindow>(title: "Remote Console");
            win.Init();
            win.Show();
        }

        private void Awake()
        {
        }

        private void OnEnable()
        {
            RConsoleCtrl.Instance.ViewModel.OnModelChanged += OnModelChanged;
            var wins = Resources.FindObjectsOfTypeAll<RConsoleMainWindow>();
            if (wins != null && wins.Length > 0)
            {
                _selfWindows = wins[0];
            }
            
            // File Browser Init
            _treeState ??= new TreeViewState();
            _tree = new RConsoleTreeView(_treeState)
            {
                OnItemClicked = OnItemClicked
            };
            RConsoleCtrl.Instance.OnFileBrowserChanged += OnFileBrowserChanged;
            RConsoleCtrl.Instance.OnFileMD5Changed += OnFileMD5Changed;
            RConsoleCtrl.Instance.FetchDirectory(new FileModel("/"));
        }

        private void OnDisable()
        {
            RConsoleCtrl.Instance.ViewModel.OnModelChanged -= OnModelChanged;
            _selfWindows = null;

            // File Browser Cleanup
            RConsoleCtrl.Instance.OnFileBrowserChanged -= OnFileBrowserChanged;
            RConsoleCtrl.Instance.OnFileMD5Changed -= OnFileMD5Changed;
        }

        private void OnModelChanged(RConsoleViewModel model)
        {
            RefreshIfOpen();
        }

        // 在收到新日志时，如果窗口已打开则请求重绘（不抢占焦点）
        public static void RefreshIfOpen()
        {
            if (_selfWindows == null) return;
            _selfWindows.Repaint();
        }

        private void Init()
        {
            // 初始化图标缓存
            // _iconLog = EditorGUIUtility.IconContent("console.infoicon");
            // _iconWarn = EditorGUIUtility.IconContent("console.warnicon");
            // _iconErr = EditorGUIUtility.IconContent("console.erroricon");

            // // 初始化样式缓存（更紧凑，不换行）
            // _styleLog = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            // _styleLog.normal.textColor = Color.white;

            // _styleWarn = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            // _styleWarn.normal.textColor = new Color(0.8f, 0.6f, 0.0f);

            // _styleError = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            // _styleError.normal.textColor = Color.red;

            // _styleStack = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            // _styleStack.normal.textColor = Color.white;

            // _styleTime = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            // _styleTime.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            // _styleIp = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            // _styleIp.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            // // 可点击超链接样式（蓝色，支持换行）
            // _styleLink = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            // _styleLink.normal.textColor = new Color(0.2f, 0.5f, 1f);

            // // 初始化列表项渲染器
            // _listItemView = new RConsoleListItemView(
            //     _iconLog,
            //     _iconWarn,
            //     _iconErr,
            //     _styleTime,
            //     _styleIp,
            //     _styleLog,
            //     _styleWarn,
            //     _styleError
            // );

            // // 初始化详情视图渲染器
            // _detailView = new RConsoleDetailView(
            //     _iconLog,
            //     _iconWarn,
            //     _iconErr,
            //     _styleStack,
            //     _styleLink
            // );
        }


        private void OnGUI()
        {
            Init();
            Toolbar();
            DrawContent();
        }

        private void Toolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Play 按钮
            var isServerStarted = RConsoleCtrl.Instance.ViewModel.IsServerStarted;
            var playText = isServerStarted ? "关闭服务" : "启动服务";
            var ips = NETUtils.GetIPv4Addresses();
            var tips = $"服务地址: {string.Join(", ", ips)}";
            var icon = EditorGUIUtility.IconContent(isServerStarted ? "PauseButton" : "d_PlayButton");
            if (GUILayout.Button(new GUIContent(playText, icon.image, tips), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
            {
                // 播放按钮点击事件
                if (isServerStarted)
                {
                    RConsoleCtrl.Instance.Disconnect();
                }
                else
                {
                    RConsoleCtrl.Instance.Connect();
                }
            }

            // Server 按钮紧随其后，宽度不扩展
            var clients = RConsoleCtrl.Instance.ViewModel.ConnectedClients;
            var text = $"连接设备({clients.Count})";
            var filterClient = RConsoleCtrl.Instance.ViewModel.FilterClientModel;
            if (filterClient != null)
            {
                text += $"[{filterClient.deviceName}]";
            }
            var serverContent = new GUIContent(text);
            if (GUILayout.Button(serverContent, EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(false)))
            {
                var serverBtnRect = GUILayoutUtility.GetLastRect();
                RConsoleClientPop.Open(serverBtnRect, this);
            }

            GUILayout.FlexibleSpace();

            // Tabs 切换
            _currentTab = (TabType)GUILayout.Toolbar((int)_currentTab, _tabNames, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawContent()
        {
            switch (_currentTab)
            {
                case TabType.LookIn:
                    DrawLookIn();
                    break;
                case TabType.FileBrowser:
                    DrawFileBrowser();
                    break;
            }
        }

        private void DrawLookIn()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox("LookIn 功能可以查看连接设备的实时界面层级结构。", MessageType.Info);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Texture2D eyeOpen = EditorGUIUtility.FindTexture("animationvisibilitytoggleon");
            if (GUILayout.Button(new GUIContent(" 获取 LookIn 数据", eyeOpen, "点击获取远程设备界面层级"), GUILayout.Height(30), GUILayout.Width(200)))
            {
                RConsoleCtrl.Instance.FetchLookin();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawFileBrowser()
        {
            var connection = RConsoleCtrl.Instance.GetSelectConnection();
            if (connection == null)
            {
                EditorGUILayout.HelpBox("请先选择一个连接的设备", MessageType.Warning);
                return;
            }

            // 使用水平布局容器，避免手动计算 Rect 导致的覆盖问题
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // 左侧：文件树
            // 限制最小宽度，防止拖拽过小
            float minLeft = 160f;
            if (_splitLeft < minLeft) _splitLeft = minLeft;
            
            // 直接使用垂直布局，指定宽度
            EditorGUILayout.BeginVertical(GUILayout.Width(_splitLeft), GUILayout.ExpandHeight(true));
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var refreshIcon = EditorGUIUtility.IconContent("d_TreeEditor.Refresh");
            if (GUILayout.Button(new GUIContent(refreshIcon.image, "刷新文件列表"), EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                RConsoleCtrl.Instance.FetchDirectory(new FileModel("/"));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            Rect treeRect = GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue, GUILayout.ExpandHeight(true));
            _tree?.OnGUI(treeRect);

            EditorGUILayout.EndVertical();

            // 分割线
            Rect splitterRect = GUILayoutUtility.GetRect(6f, 6f, 0f, float.MaxValue, GUILayout.ExpandHeight(true), GUILayout.Width(6f));
            if (Event.current.type == EventType.Repaint)
            {
                Rect drawRect = splitterRect;
                drawRect.width = 1f;
                drawRect.x += 2f;
                EditorGUI.DrawRect(drawRect, new Color(0, 0, 0, 0.3f));
            }
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            // 右侧：详情页
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _rightScroll = GUILayout.BeginScrollView(_rightScroll);
            DrawDetails();
            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // 处理分割线拖拽事件
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                _resizing = true;
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseDrag && _resizing)
            {
                _splitLeft += Event.current.delta.x;
                // 限制最大宽度，防止把右边挤没了
                float maxLeft = position.width - 240f; 
                _splitLeft = Mathf.Clamp(_splitLeft, minLeft, maxLeft);
                
                Repaint();
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseUp && _resizing)
            {
                _resizing = false;
                Event.current.Use();
            }
        }

        private void OnFileBrowserChanged(FileModel resp)
        {
            _root = resp;
            _tree.SetData(resp);
            Repaint();
        }

        private void OnFileMD5Changed(FileModel resp)
        {
            if (_selecct == null || resp == null) return;
            var currentPath = _selecct.FileModel?.Path;
            if (string.Equals(currentPath, resp.Path, StringComparison.OrdinalIgnoreCase))
            {
                if (_selecct.FileModel != null) _selecct.FileModel.MD5 = resp.MD5;
                Repaint();
            }
        }

        private void OnItemClicked(RConsoleTreeViewItem item)
        {
            _selecct = item;
            Repaint();
        }

        private void DrawDetails()
        {
            if (_root == null || _selecct == null) return;

            var m = _selecct.FileModel;
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(m.LastWriteTime).ToLocalTime();
            DrawRow("名称", m.Name);
            DrawRow("路径", m.Path.TrimStart(m.RootPath.ToCharArray()));
            DrawRow("类型", m.IsDirectory ? "目录" : "文件");
            DrawRow("大小", m.IsDirectory ? "-" : FormatSize(m.Length));
            DrawRow("最后修改", dt.ToString("yyyy-MM-dd HH:mm:ss"));
            if (!m.IsDirectory && !string.IsNullOrEmpty(m.MD5))
            {
                DrawRow("MD5", m.MD5);
            }
            EditorGUILayout.Space();
        }

        private void DrawRow(string title, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.Width(120));
            if (GUILayout.Button(new GUIContent(value, "点击复制到剪贴板"), EditorStyles.label, GUILayout.ExpandWidth(true)))
            {
                GUIUtility.systemCopyBuffer = value;
                ShowNotification(new GUIContent("已复制到剪贴板"));
            }
            EditorGUILayout.EndHorizontal();
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 0) bytes = 0;
            double v = bytes;
            string u = "B";
            if (v >= 1024) { v /= 1024; u = "KB"; }
            if (v >= 1024) { v /= 1024; u = "MB"; }
            if (v >= 1024) { v /= 1024; u = "GB"; }
            if (u == "B") return bytes + " B";
            var fmt = v >= 100 ? "F0" : v >= 10 ? "F1" : "F2";
            return v.ToString(fmt) + " " + u;
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

        /*
        private void DrawList()
        {
            // ... (原有日志列表绘制逻辑保留注释)
        }

        private bool PassFilter(LogModel i)
        {
            // ... (原有筛选逻辑保留注释)
        }
        */
    }
}
