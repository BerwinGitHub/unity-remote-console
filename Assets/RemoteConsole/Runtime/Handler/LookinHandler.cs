using RConsole.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RConsole.Runtime
{
    public class LookinHandler : IHandler
    {
        public override void OnEnable()
        {
            RCCapability.Instance.WebSocket.On(EnvelopeKind.S2CLookIn, (byte)SubLookIn.LookIn, OnLookIn);
        }

        public override void OnDisable()
        {
            RCCapability.Instance.WebSocket.Off(EnvelopeKind.S2CLookIn, (byte)SubLookIn.LookIn, OnLookIn);
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
                var scene = SceneManager.GetActiveScene();
                var model = new LookInViewModel
                {
                    Name = scene.name,
                    Path = "/",
                    IsActive = true,
                    Rect = new Rect()
                };
                var roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    var child = BuildNode(roots[i], "/" + roots[i].name);
                    model.Children.Add(child);
                }

                return model;
            }

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

        private LookInViewModel BuildNode(GameObject go, string currentPath)
        {
            var model = new LookInViewModel
            {
                Name = go.name,
                Path = currentPath,
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
                var compModel = new ComponentModel
                {
                    TypeName = c.GetType().Name
                };

                // 通用属性
                if (c is Behaviour behaviour)
                {
                    compModel.Properties["Enabled"] = behaviour.enabled.ToString();
                }

                // 特殊组件处理
                if (c is UnityEngine.UI.Text text)
                {
                    compModel.Properties["Text"] = text.text;
                    compModel.Properties["Color"] = text.color.ToString();
                    compModel.Properties["FontSize"] = text.fontSize.ToString();
                }
                else if (c is UnityEngine.UI.Image image)
                {
                    compModel.Properties["Color"] = image.color.ToString();
                    if (image.sprite != null)
                    {
                        compModel.Properties["Sprite"] = image.sprite.name;
                        try
                        {
                            var texture = image.sprite.texture;
                            if (texture != null)
                            {
                                // 限制最大尺寸，保持长宽比
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
                else if (c is RectTransform rt)
                {
                    compModel.Properties["AnchoredPosition"] = rt.anchoredPosition.ToString();
                    compModel.Properties["SizeDelta"] = rt.sizeDelta.ToString();
                    compModel.Properties["AnchorMin"] = rt.anchorMin.ToString();
                    compModel.Properties["AnchorMax"] = rt.anchorMax.ToString();
                    compModel.Properties["Pivot"] = rt.pivot.ToString();
                }
                else if (c is Transform t)
                {
                    compModel.Properties["LocalPosition"] = t.localPosition.ToString();
                    compModel.Properties["LocalEulerAngles"] = t.localEulerAngles.ToString();
                    compModel.Properties["LocalScale"] = t.localScale.ToString();
                }

                model.Components.Add(compModel);
            }
        }

        private GameObject FindByPath(string path)
        {
            var scene = SceneManager.GetActiveScene();
            var segs = path.Split('/');
            var names = new System.Collections.Generic.List<string>();
            for (int i = 0; i < segs.Length; i++)
            {
                if (!string.IsNullOrEmpty(segs[i])) names.Add(segs[i]);
            }

            if (names.Count == 0) return null;
            GameObject root = null;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == names[0])
                {
                    root = roots[i];
                    break;
                }
            }

            if (root == null) return null;
            var current = root.transform;
            for (int i = 1; i < names.Count; i++)
            {
                current = current.Find(names[i]);
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