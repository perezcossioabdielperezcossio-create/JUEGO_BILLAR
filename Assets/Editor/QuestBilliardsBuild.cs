using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
public static class QuestBilliardsBuild
{
    public static void BuildQuestApk()
    {
        var scene = "Assets/Scenes/BilliardsPrototype.unity";
        if (Application.isBatchMode)
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene);
        ValidateStartup(scene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene, true) };
        PlayerSettings.companyName = "BilliardsVR";
        PlayerSettings.productName = "Billar VR";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.billiardsvr.questbillar");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        EditorUserBuildSettings.buildAppBundle = false;
        AssetDatabase.SaveAssets();
        var path = Environment.GetEnvironmentVariable("BILLIARDS_APK_PATH");
        if (string.IsNullOrEmpty(path)) path = "Builds/Android/BillarVR-Quest3.apk";
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { scene }, locationPathName = path,
            target = BuildTarget.Android, options = BuildOptions.Development
        });
        Debug.Log("BILLIARDS_BUILD_RESULT: " + report.summary.result + " | " + report.summary.totalSize + " bytes | " + path);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("APK build failed: " + report.summary.result);
    }
    private static void ValidateStartup(string scenePath)
    {
        if (EditorApplication.isPlaying) throw new Exception("Sal del modo Play antes de compilar.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != scenePath || scene.isDirty) throw new Exception("Abre y guarda BilliardsPrototype antes de compilar.");
        foreach (var root in scene.GetRootGameObjects())
            foreach (var part in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(part.gameObject) > 0)
                    throw new Exception("Hay scripts sin referencia: " + part.name);
        var mode = UnityEngine.Object.FindFirstObjectByType<QuestBilliards.Inputs.BilliardsPlayerMode>();
        var table = UnityEngine.Object.FindFirstObjectByType<QuestBilliards.GameRules.PocketDifficultyController>();
        var floorObject = GameObject.Find("RoomFloor");
        var floor = floorObject != null ? floorObject.GetComponent<Collider>() : null;
        if (mode == null || table == null || floor == null || !floor.enabled || floor.isTrigger)
            throw new Exception("Falta el jugador, la mesa o el suelo solido.");
        if (!floor.Raycast(new Ray(mode.transform.position + Vector3.up * 2, Vector3.down), out _, 3))
            throw new Exception("El jugador no tiene suelo debajo de su punto de inicio.");
        if (Physics.GetIgnoreLayerCollision(mode.gameObject.layer, floor.gameObject.layer))
            throw new Exception("La capa del jugador ignora el suelo.");
        Vector3 ball = mode.whiteBall.position;
        if (!table.surfaceCollider.Raycast(new Ray(ball + Vector3.up * 0.2f, Vector3.down), out var hit, 0.4f) || Mathf.Abs(ball.y - hit.point.y - 0.028575f) > 0.01f)
            throw new Exception("La blanca no inicia sobre el paño.");
        foreach (var pocket in table.GetComponentsInChildren<QuestBilliards.GameRules.PocketTrigger>())
            if (Vector3.ProjectOnPlane(ball - pocket.transform.position, Vector3.up).magnitude < pocket.openingRadius + 0.034f)
                throw new Exception("La blanca inicia demasiado cerca de una tronera.");
        Debug.Log("Billar: inicio verificado; suelo solido y blanca sobre el paño.");
    }
}
