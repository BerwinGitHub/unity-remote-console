using RConsole.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace RConsole.Runtime
{
    public class LookinHandler : IHandler
    {
        private static Dictionary<int, GameObject> _idCache = new Dictionary<int, GameObject>();

        public override void OnEnable()
        {
            RCCapability.Instance.WebSocket.On(EnvelopeKind.S2CLookIn, (byte)SubLookIn.LookIn, OnLookIn);
            RCCapability.Instance.WebSocket.On(EnvelopeKind.S2CLookIn, (byte)SubLookIn.SyncNode, OnSyncNode);
        }

        public override void OnDisable()
        {
            RCCapability.Instance.WebSocket.Off(EnvelopeKind.S2CLookIn, (byte)SubLookIn.LookIn, OnLookIn);
            RCCapability.Instance.WebSocket.Off(EnvelopeKind.S2CLookIn, (byte)SubLookIn.SyncNode, OnSyncNode);
        }

        private IBinaryModelBase OnSyncNode(IBinaryModelBase model)
        {
            var req = (StringModel)model;
            if (string.IsNullOrEmpty(req.Value)) return null;
            
            // Debug.Log($"OnSyncNode cmd: {req.Value}");
            var parts = req.Value.Split('|');
            if (parts.Length < 3) return null;

            if (int.TryParse(parts[0], out var id))
            {
                if (_idCache.TryGetValue(id, out var go) && go != null)
                {
                    string type = parts[1];
                    string value = parts[2];
                    ApplySync(go, type, value);
                }
            }
            return null;
        }

        private void ApplySync(GameObject go, string type, string value)
        {
            try
            {
                if (type == "Active")
                {
                    go.SetActive(value == "1");
                }
                else if (type == "Pos")
                {
                    var v = ParseVector3(value);
                    go.transform.localPosition = v;
                }
                else if (type == "Rot")
                {
                    var v = ParseVector3(value);
                    go.transform.localEulerAngles = v;
                }
                else if (type == "Scale")
                {
                    var v = ParseVector3(value);
                    go.transform.localScale = v;
                }
                else if (type == "Size")
                {
                    var rt = go.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        var v = ParseVector2(value);
                        rt.sizeDelta = v;
                    }
                }
            }
            catch { }
        }

        private Vector3 ParseVector3(string s)
        {
            var parts = s.Split(',');
            return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
        }

        private Vector2 ParseVector2(string s)
        {
            var parts = s.Split(',');
            return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
        }

        private IBinaryModelBase OnLookIn(IBinaryModelBase model)
        {
            var req = (StringModel)model;
            var root = BuildTree(req.Value);
            return root;
        }

        private LookInViewModel BuildTree(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "/")
            {
                _idCache.Clear();
                var model = new LookInViewModel
                {
                    Name = "World",
                    Path = "/",
                    IsActive = true,
                    Rect = new Rect()
                };

                // 1. Iterate all loaded scenes
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    var sceneModel = new LookInViewModel
                    {
                        Name = scene.name,
                        Path = "/" + scene.name, // Virtual path for scene
                        IsActive = true,
                        Rect = new Rect()
                    };

                    var roots = scene.GetRootGameObjects();
                    foreach (var rootGo in roots)
                    {
                        var child = BuildNode(rootGo, "/" + scene.name + "/" + rootGo.name);
                        sceneModel.Children.Add(child);
                    }
                    model.Children.Add(sceneModel);
                }

                // 2. DontDestroyOnLoad Scene
                var dontDestroyScene = GetDontDestroyOnLoadScene();
                if (dontDestroyScene.IsValid())
                {
                    var ddModel = new LookInViewModel
                    {
                        Name = "DontDestroyOnLoad",
                        Path = "/DontDestroyOnLoad",
                        IsActive = true,
                        Rect = new Rect()
                    };
                    
                    var roots = dontDestroyScene.GetRootGameObjects();
                    foreach (var rootGo in roots)
                    {
                        var child = BuildNode(rootGo, "/DontDestroyOnLoad/" + rootGo.name);
                        ddModel.Children.Add(child);
                    }
                    model.Children.Add(ddModel);
                }

                return model;
            }

            // Path format: /SceneName/GoName/ChildName
            // Note: This logic assumes path starts with /SceneName.
            // But FindByPath implementation needs to be checked or updated because GameObject.Find usually doesn't care about Scene unless specified.
            // Let's look at FindByPath later. For now, we update the tree structure.
            
            var go = FindByPath(path);
            if (go == null)
            {
                return new LookInViewModel
                {
                    Name = path,
                    Path = path,
                    IsActive = false,
                    Rect = new Rect()
                };
            }

            return BuildNode(go, path);
        }

        private Scene GetDontDestroyOnLoadScene()
        {
            GameObject temp = null;
            try
            {
                temp = new GameObject();
                Object.DontDestroyOnLoad(temp);
                return temp.scene;
            }
            finally
            {
                if (temp != null)
                    Object.DestroyImmediate(temp);
            }
        }

        private LookInViewModel BuildNode(GameObject go, string currentPath)
        {
            int id = go.GetInstanceID();
            _idCache[id] = go;

            var model = new LookInViewModel
            {
                Name = go.name,
                Path = currentPath,
                InstanceID = id,
                IsActive = go.activeInHierarchy,
                Rect = GetNodeRect(go)
            };
            var t = go.transform;
            GetComponentsData(go, model);
            var count = t.childCount;
            for (int i = 0; i < count; i++)
            {
                var c = t.GetChild(i).gameObject;
                var childPath = currentPath.EndsWith("/") ? currentPath + c.name : currentPath + "/" + c.name;
                model.Children.Add(BuildNode(c, childPath));
            }

            return model;
        }

        private void GetComponentsData(GameObject go, LookInViewModel model)
        {
            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null) continue;
                var type = c.GetType();
                var compModel = new ComponentModel
                {
                    TypeName = type.Name,
                    FullTypeName = type.FullName
                };

                // 通用反射属性提取
                try
                {
                    // 1. 提取属性 (Properties)
                    // 排除 Obsolete 的，排除索引器
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanRead && !p.IsDefined(typeof(System.ObsoleteAttribute), true) && p.GetIndexParameters().Length == 0);
                    
                    // 排除 Component/Object 基类及一些危险属性
                    var excludedProps = new HashSet<string> { 
                        "name", "tag", "hideFlags", "mesh", "material", "materials", "sharedMaterial", "sharedMaterials", 
                        "gameObject", "transform", "runInEditMode", "useGUILayout"
                    }; 
                    
                    foreach (var p in properties)
                    {
                        if (excludedProps.Contains(p.Name)) continue;
                        // 排除基于 Component/GameObject 的引用，防止循环
                        if (typeof(Component).IsAssignableFrom(p.PropertyType) || typeof(GameObject).IsAssignableFrom(p.PropertyType)) continue;
                        // 排除 UnityEvent，避免序列化一大堆垃圾信息
                        if (typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(p.PropertyType)) continue;

                        try
                        {
                            var val = p.GetValue(c);
                            if (val != null)
                            {
                                compModel.Properties[p.Name] = val.ToString();
                            }
                        }
                        catch { }
                    }

                    // 2. 提取字段 (Fields)
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Where(f => !f.IsDefined(typeof(System.ObsoleteAttribute), true));
                    
                    foreach (var f in fields)
                    {
                        if (typeof(Component).IsAssignableFrom(f.FieldType) || typeof(GameObject).IsAssignableFrom(f.FieldType)) continue;
                        
                        try 
                        {
                            var val = f.GetValue(c);
                            if (val != null)
                            {
                                compModel.Properties[f.Name] = val.ToString();
                            }
                        }
                        catch { }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"LookIn Reflection Error on {type.Name}: {e.Message}");
                }

                // 特殊组件增强处理
                if (c is UnityEngine.UI.Image image)
                {
                    if (image.sprite != null)
                    {
                        compModel.Properties["Sprite"] = image.sprite.name; // 覆盖/确保有 Sprite 名
                        
                        try
                        {
                            var texture = image.sprite.texture;
                            if (texture != null)
                            {
                                int maxDimension = 512;
                                int width = texture.width;
                                int height = texture.height;
                                if (width > maxDimension || height > maxDimension)
                                {
                                    float aspect = (float)width / height;
                                    if (width > height)
                                    {
                                        width = maxDimension;
                                        height = Mathf.RoundToInt(width / aspect);
                                    }
                                    else
                                    {
                                        height = maxDimension;
                                        width = Mathf.RoundToInt(height * aspect);
                                    }
                                }
                                
                                // 计算缩放后的 Border
                                var border = image.sprite.border;
                                float scaleX = (float)width / texture.width;
                                float scaleY = (float)height / texture.height;
                                border.x *= scaleX;
                                border.y *= scaleY;
                                border.z *= scaleX;
                                border.w *= scaleY;
                                compModel.Properties["SpriteBorder"] = border.ToString();

                                var tmp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                                Graphics.Blit(texture, tmp);
                                var previous = RenderTexture.active;
                                RenderTexture.active = tmp;
                                var myTexture2D = new Texture2D(width, height);
                                myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                                myTexture2D.Apply();
                                RenderTexture.active = previous;
                                RenderTexture.ReleaseTemporary(tmp);
                                compModel.ExtraData = myTexture2D.EncodeToPNG();
                                if (Application.isPlaying) Object.Destroy(myTexture2D);
                                else Object.DestroyImmediate(myTexture2D);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"LookIn Image Sync Error: {e.Message}");
                        }
                    }
                }

                model.Components.Add(compModel);
            }
        }

        private GameObject FindByPath(string path)
        {
            var segs = path.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (segs.Length < 2) return null; // At least SceneName and RootGoName

            string sceneName = segs[0];
            string rootName = segs[1];

            Scene targetScene = default;
            
            // 1. Find Scene
            if (sceneName == "DontDestroyOnLoad")
            {
                targetScene = GetDontDestroyOnLoadScene();
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (s.name == sceneName)
                    {
                        targetScene = s;
                        break;
                    }
                }
            }

            if (!targetScene.IsValid()) return null;

            // 2. Find Root GameObject
            GameObject root = null;
            var roots = targetScene.GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r.name == rootName)
                {
                    root = r;
                    break;
                }
            }

            if (root == null) return null;

            // 3. Find Child
            var current = root.transform;
            for (int i = 2; i < segs.Length; i++)
            {
                current = current.Find(segs[i]);
                if (current == null) return null;
            }

            return current.gameObject;
        }

        private Rect GetNodeRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                var canvas = rt.GetComponentInParent<Canvas>();
                Camera cam = null;
                if (canvas != null)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
                        cam = canvas.worldCamera;
                }

                var p0 = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                var p2 = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                var minX = Mathf.Min(p0.x, p2.x);
                var minY = Mathf.Min(p0.y, p2.y);
                var w = Mathf.Abs(p2.x - p0.x);
                var h = Mathf.Abs(p2.y - p0.y);
                return new Rect(minX, minY, w, h);
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var b = renderer.bounds;
                var cam = Camera.main;
                if (cam != null)
                {
                    var min = cam.WorldToScreenPoint(b.min);
                    var max = cam.WorldToScreenPoint(b.max);
                    var minX = Mathf.Min(min.x, max.x);
                    var minY = Mathf.Min(min.y, max.y);
                    var w = Mathf.Abs(max.x - min.x);
                    var h = Mathf.Abs(max.y - min.y);
                    return new Rect(minX, minY, w, h);
                }
            }

            return new Rect();
        }
    }
}