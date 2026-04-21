#if UNITY_EDITOR && UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class MultisetPostProcess
{
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        // Add Info.plist entries for MultipeerConnectivity
        string plistPath = Path.Combine(path, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        var bonjourArray = plist.root.CreateArray("NSBonjourServices");
        bonjourArray.AddString("_multiset-sdk._tcp");
        bonjourArray.AddString("_multiset-sdk._udp");

        plist.root.SetString("NSLocalNetworkUsageDescription",
            "This app uses the local network to discover and connect with nearby AR devices for shared localization.");

        plist.WriteToFile(plistPath);

        // Link MultipeerConnectivity.framework
        string projPath = PBXProject.GetPBXProjectPath(path);
        PBXProject proj = new PBXProject();
        proj.ReadFromString(File.ReadAllText(projPath));

        string targetGuid = proj.GetUnityMainTargetGuid();
        proj.AddFrameworkToProject(targetGuid, "MultipeerConnectivity.framework", false);

        File.WriteAllText(projPath, proj.WriteToString());

        UnityEngine.Debug.Log("[MultisetPostProcess] Added Bonjour services + MultipeerConnectivity.framework");
    }
}
#endif
