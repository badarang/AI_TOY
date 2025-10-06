using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class AssignVolumeProfile : MonoBehaviour
{
    [MenuItem("Tools/Assign Volume Profile")]
    static void AssignProfile()
    {
        GameObject volumeObj = GameObject.Find("Global Volume");
        if (volumeObj == null)
        {
            Debug.LogError("Global Volume not found");
            return;
        }
        
        Volume volume = volumeObj.GetComponent<Volume>();
        if (volume == null)
        {
            Debug.LogError("Volume component not found");
            return;
        }
        
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/StylizedPostProcess.asset");
        if (profile == null)
        {
            Debug.LogError("Profile not found at Assets/Settings/StylizedPostProcess.asset");
            return;
        }
        
        volume.profile = profile;
        volume.isGlobal = true;
        volume.priority = 0;
        
        Debug.Log("Volume Profile assigned successfully");
    }
}