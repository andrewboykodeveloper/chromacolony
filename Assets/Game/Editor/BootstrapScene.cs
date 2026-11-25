using Chromacolony.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Chromacolony.Editor
{
    public static class BootstrapScene
    {
        private const string UiFolder = "Assets/UI/";
        private const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string PanelPath = UiFolder + "Panel.asset";
        private const string CrewShaderPath = "Assets/Game/Art/CrewColor.shader";
        private const string CrewMaterialPath = "Assets/Game/Art/CrewColor.mat";

        private const string SpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        public static void Create()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiFolder + "Theme.tss");
            if (theme == null)
            {
                Debug.LogError("BootstrapScene: no " + UiFolder + "Theme.tss");
                EditorApplication.Exit(1);
                return;
            }

            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet = theme;
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1080, 1920);
            panel.screenMatchMode = PanelScreenMatchMode.Expand;
            AssetDatabase.CreateAsset(panel, PanelPath);
            var crewShader = AssetDatabase.LoadAssetAtPath<Shader>(CrewShaderPath);
            if (crewShader == null)
            {
                Debug.LogError("BootstrapScene: no " + CrewShaderPath);
                EditorApplication.Exit(1);
                return;
            }

            AssetDatabase.CreateAsset(new Material(crewShader) { name = "CrewColor" }, CrewMaterialPath);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();

            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.27f, 0.47f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var uiObject = new GameObject("Game");
            var document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiFolder + "Main.uxml");

            var root = uiObject.AddComponent<GameRoot>();
            Set(root, "document", document);
            Set(root, "configCsv", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/Data/Config.csv"));
            Set(root, "localizationCsv", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/Data/Localization.csv"));
            Set(root, "boardsText", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/Data/Boards.txt"));
            Set(root, "crewMaterial", AssetDatabase.LoadAssetAtPath<Material>(CrewMaterialPath));
            Set(root, "spriteMaterial", AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("BootstrapScene: done");
            EditorApplication.Exit(0);
        }

        private static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null || value == null)
            {
                throw new System.InvalidOperationException($"BootstrapScene: {target.GetType().Name}.{field} or its value is missing");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
