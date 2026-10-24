using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Renders the itch.io cover: Henry in the first aisle giving a thumbs up, the title above him.
static class CoverShot
{
    const int Width = 1260, Height = 1000;

    [MenuItem("JameGam/Debug/Render Cover Image %#&3")]
    static void Render()
    {
        var outside = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid())
            .FirstOrDefault(t => t.name == "Outside" && t.parent != null && t.parent.name == "Wake-up Cereal Box");
        if (outside == null)
        {
            Debug.LogWarning("[CoverShot] Build the store first.");
            return;
        }
        var spot = outside.position + Vector3.forward * 12f;
        spot.y = 0f;
        var camPos = spot + new Vector3(0f, 1.0f, -4.4f);

        var stage = new GameObject("Cover Stage");
        try
        {
            var henry = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterSetup.ModelPath), stage.transform);
            var mat = CharacterSetup.HenryMaterial();
            var renderers = henry.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
            var toCam = camPos - spot;
            toCam.y = 0f;
            henry.transform.SetPositionAndRotation(spot, Quaternion.LookRotation(toCam));
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            henry.transform.localScale *= 2f / b.size.y;

            var anim = henry.GetComponent<Animator>();
            var body = henry.transform;
            var f = body.forward;
            // thumbs up on his right, other hand on his hip
            Bend(anim, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                Vector3.down * 0.3f + body.right * 0.8f + f * 0.35f, Vector3.up * 0.95f + f * 0.3f + body.right * 0.1f);
            Bend(anim, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                Vector3.down * 0.55f - body.right * 0.85f - f * 0.1f, body.right * 0.8f + Vector3.down * 0.55f + f * 0.2f);
            var rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            Object.DestroyImmediate(anim);

            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            henry.transform.position += Vector3.up * (spot.y - b.min.y);

            var glove = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.93f, 0.7f, 0.55f) };
            var handPos = rightHand != null ? rightHand.position : spot + Vector3.up * 1.6f;
            var thumb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(thumb.GetComponent<Collider>());
            thumb.transform.SetParent(stage.transform);
            thumb.transform.position = handPos + Vector3.up * 0.24f + f * 0.03f;
            thumb.transform.localScale = new Vector3(0.11f, 0.09f, 0.11f);
            thumb.GetComponent<Renderer>().sharedMaterial = glove;

            Light L(Vector3 at, float intensity, Color c, float range)
            {
                var l = new GameObject("Light").AddComponent<Light>();
                l.transform.SetParent(stage.transform);
                l.transform.position = at;
                l.type = LightType.Point;
                l.intensity = intensity;
                l.range = range;
                l.color = c;
                l.shadows = LightShadows.None;
                return l;
            }
            L(spot + f * 3f + Vector3.up * 3f - body.right * 1.5f, 30f, new Color(1f, 0.95f, 0.88f), 12f);
            L(spot - f * 2f + Vector3.up * 3f + body.right * 2f, 25f, new Color(0.4f, 0.95f, 0.85f), 10f);

            var camGo = new GameObject("Cover Camera");
            camGo.transform.SetParent(stage.transform);
            camGo.transform.position = camPos;
            camGo.transform.LookAt(spot + Vector3.up * 1.75f);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2000f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            var canvasGo = new GameObject("Cover Title", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(stage.transform);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.5f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            var title = new GameObject("Title", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            title.transform.SetParent(canvasGo.transform, false);
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -40);
            rt.sizeDelta = new Vector2(0, 260);
            title.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BebasNeue-Regular.ttf");
            title.fontSize = 230;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.95f, 0.97f, 0.97f);
            title.text = "TINY HENRY";
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.02f, 0.05f, 0.06f, 0.85f);
            shadow.effectDistance = new Vector2(8, -10);
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0.2f, 0.85f, 0.75f, 0.9f);

            var rtex = new RenderTexture(Width, Height, 24) { antiAliasing = 8 };
            cam.targetTexture = rtex;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rtex;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            string folder = Path.Combine(Path.GetTempPath(), "TinyHenrySnapshots");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "TinyHenryCover.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.DestroyImmediate(rtex);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(glove);
            Debug.Log($"[CoverShot] Saved {path}");
        }
        finally
        {
            Object.DestroyImmediate(stage);
        }
    }

    static void Bend(Animator anim, HumanBodyBones upperBone, HumanBodyBones lowerBone, HumanBodyBones handBone, Vector3 elbowDir, Vector3 foreDir)
    {
        var upper = anim.GetBoneTransform(upperBone);
        var lower = anim.GetBoneTransform(lowerBone);
        var hand = anim.GetBoneTransform(handBone);
        if (upper == null || lower == null || hand == null) return;
        upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbowDir.normalized) * upper.rotation;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, foreDir.normalized) * lower.rotation;
    }
}
