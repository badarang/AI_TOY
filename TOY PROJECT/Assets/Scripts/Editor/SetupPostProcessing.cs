using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class SetupPostProcessing : MonoBehaviour
{
    [MenuItem("Tools/Enable Post Processing")]
    static void EnablePostProcessing()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found");
            return;
        }
        
        var cameraData = mainCamera.GetUniversalAdditionalCameraData();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            Debug.Log("Post Processing enabled on Main Camera");
        }
    }
}