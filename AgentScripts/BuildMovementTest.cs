using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Hackaton.Player;

public static class BuildMovementTest
{
    public static string Build()
    {
        const string prefabPath = "Assets/Prefabs/Player.prefab";
        const string scenePath = "Assets/Scenes/MovementTest.unity";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null ||
            System.IO.File.Exists(scenePath))
            throw new System.InvalidOperationException("Movement assets already exist; refusing to overwrite.");
        foreach (var scene in EnumerableScenes())
            if (scene.isDirty) throw new System.InvalidOperationException("Save the open scene before building.");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var player = new GameObject("Player");
        var controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.4f;
        controller.center = Vector3.up;
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.04f;
        controller.minMoveDistance = 0f;
        controller.slopeLimit = 45f;
        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.up;
        visual.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        // Keep a capsule in the prefab/Scene view without blocking the first-person camera.
        visual.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        var pivot = new GameObject("CameraPivot").transform;
        pivot.SetParent(player.transform, false);
        pivot.localPosition = new Vector3(0f, 1.65f, 0f);
        var cameraObject = new GameObject("FirstPersonCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(pivot, false);
        var camera = cameraObject.GetComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 200f;
        camera.fieldOfView = 75f;
        camera.backgroundColor = new Color(0.25f, 0.35f, 0.45f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        var input = player.AddComponent<PlayerInputReader>();
        var serializedInput = new SerializedObject(input);
        serializedInput.FindProperty("inputActions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        serializedInput.ApplyModifiedPropertiesWithoutUndo();
        var look = player.AddComponent<FirstPersonLook>();
        var serializedLook = new SerializedObject(look);
        serializedLook.FindProperty("cameraPivot").objectReferenceValue = pivot;
        serializedLook.ApplyModifiedPropertiesWithoutUndo();
        player.AddComponent<PlayerMovement>();
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, prefabPath, InteractionMode.AutomatedAction);
        player.transform.position = new Vector3(0f, 0.1f, -8f);
        Box("Ground", new Vector3(0f, -0.25f, 0f), new Vector3(30f, 0.5f, 30f));
        Box("BackWall", new Vector3(0f, 1.5f, 14.5f), new Vector3(30f, 3f, 0.5f));
        Box("LeftWall", new Vector3(-14.5f, 1.5f, 0f), new Vector3(0.5f, 3f, 30f));
        Box("RightWall", new Vector3(14.5f, 1.5f, 0f), new Vector3(0.5f, 3f, 30f));
        Box("FrontWall", new Vector3(0f, 1.5f, -14.5f), new Vector3(30f, 3f, 0.5f));
        Box("LowBlock", new Vector3(-3f, 0.35f, 0f), new Vector3(2f, 0.7f, 2f));
        Box("TallBlock", new Vector3(3f, 1.5f, 1f), new Vector3(2f, 3f, 2f));
        Box("CollisionWall", new Vector3(0f, 1.5f, 6f), new Vector3(6f, 3f, 0.5f));
        var light = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
        EditorSceneManager.SaveScene(testScene, scenePath);
        AssetDatabase.SaveAssets();
        return scenePath;
    }

    private static System.Collections.Generic.IEnumerable<UnityEngine.SceneManagement.Scene> EnumerableScenes()
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            yield return UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    }

    private static void Box(string name, Vector3 position, Vector3 scale)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
    }
}
