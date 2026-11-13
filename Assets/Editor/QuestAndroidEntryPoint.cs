#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

// App UI can rewrite the source manifest before Unity generates the Gradle project.
public sealed class QuestAndroidEntryPoint : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => int.MaxValue;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var manifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        var document = new XmlDocument();
        document.Load(manifest);

        const string android = "http://schemas.android.com/apk/res/android";
        var namespaces = new XmlNamespaceManager(document.NameTable);
        namespaces.AddNamespace("android", android);
        var activity = document.SelectSingleNode(
            "/manifest/application/activity[intent-filter/action[@android:name='android.intent.action.MAIN'] and intent-filter/category[@android:name='com.oculus.intent.category.VR']]",
            namespaces) as XmlElement;

        if (activity == null)
            throw new System.InvalidOperationException("No se encontró la actividad VR de arranque en AndroidManifest.xml.");

        activity.SetAttribute("name", android, "com.unity3d.player.UnityPlayerGameActivity");
        activity.SetAttribute("theme", android, "@style/BaseUnityGameActivityTheme");
        document.Save(manifest);
    }
}
#endif
