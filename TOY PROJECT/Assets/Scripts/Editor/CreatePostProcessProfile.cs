using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class CreatePostProcessProfile : MonoBehaviour
{
    [MenuItem("Tools/Create Stylized Post Process Profile")]
    static void CreateProfile()
    {
        string path = "Assets/Settings/StylizedPostProcess.asset";
        
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        if (profile.TryGet(out ColorAdjustments colorAdj))
        {
            colorAdj.active = true;
        }
        else
        {
            colorAdj = profile.Add<ColorAdjustments>(true);
        }
        colorAdj.saturation.Override(10f);
        colorAdj.contrast.Override(15f);
        
        if (profile.TryGet(out Tonemapping tonemap))
        {
            tonemap.active = true;
        }
        else
        {
            tonemap = profile.Add<Tonemapping>(true);
        }
        tonemap.mode.Override(TonemappingMode.Neutral);
        
        if (profile.TryGet(out Bloom bloom))
        {
            bloom.active = true;
        }
        else
        {
            bloom = profile.Add<Bloom>(true);
        }
        bloom.intensity.Override(0.3f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.5f);
        
        if (profile.TryGet(out Vignette vignette))
        {
            vignette.active = true;
        }
        else
        {
            vignette = profile.Add<Vignette>(true);
        }
        vignette.intensity.Override(0.25f);
        vignette.smoothness.Override(0.4f);
        
        if (profile.TryGet(out FilmGrain grain))
        {
            grain.active = true;
        }
        else
        {
            grain = profile.Add<FilmGrain>(true);
        }
        grain.intensity.Override(0.1f);
        grain.type.Override(FilmGrainLookup.Medium1);
        
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Profile created at {path}");
    }
}