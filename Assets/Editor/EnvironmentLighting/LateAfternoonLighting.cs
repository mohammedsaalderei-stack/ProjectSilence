using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;

public static class LateAfternoonLighting
{
    const string Folder="Assets/EnvironmentLighting";
    public static readonly Color Sky=new Color(.54f,.62f,.70f);
    [MenuItem("Tools/Environment/Apply 5 PM Lighting Everywhere")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
        Directory.CreateDirectory(Folder);
        // One shared lighting setup for every district; no regional night lights.
        foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if(l.type==LightType.Directional||l.type==LightType.Spot){Object.DestroyImmediate(l.gameObject);continue;}
            l.renderingLayerMask=-1;
            var data=l.GetComponent<UniversalAdditionalLightData>();if(data!=null)data.renderingLayers=uint.MaxValue;
        }
        var previous=GameObject.Find("Environment - 5 PM Golden Hour");if(previous!=null)Object.DestroyImmediate(previous);
        var root=new GameObject("Environment - 5 PM Golden Hour");
        var sun=new GameObject("Sun - Warm Late Afternoon");sun.transform.SetParent(root.transform);sun.transform.rotation=Quaternion.Euler(27,-48,0);
        var key=sun.AddComponent<Light>();key.type=LightType.Directional;key.lightUnit=LightUnit.Lux;key.intensity=3.2f;key.color=new Color(1,.82f,.62f);key.shadows=LightShadows.Soft;key.shadowStrength=.62f;key.shadowBias=.025f;key.shadowNormalBias=.25f;
        sun.AddComponent<UniversalAdditionalLightData>().renderingLayers=uint.MaxValue;RenderSettings.sun=key;
        var sky=new GameObject("Sky - Soft Afternoon Bounce");sky.transform.SetParent(root.transform);sky.transform.rotation=Quaternion.Euler(65,135,0);
        var fill=sky.AddComponent<Light>();fill.type=LightType.Directional;fill.lightUnit=LightUnit.Lux;fill.intensity=.65f;fill.color=new Color(.79f,.87f,1);fill.shadows=LightShadows.None;sky.AddComponent<UniversalAdditionalLightData>().renderingLayers=uint.MaxValue;
        foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))r.renderingLayerMask=1;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.45f,.51f,.59f);RenderSettings.ambientEquatorColor=new Color(.37f,.34f,.29f);RenderSettings.ambientGroundColor=new Color(.23f,.20f,.16f);RenderSettings.ambientIntensity=1;RenderSettings.fog=false;
        var skyMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/FivePM_Sky.mat");
        if(skyMat==null){skyMat=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(skyMat,Folder+"/FivePM_Sky.mat");}
        skyMat.SetColor("_SkyTint",new Color(.48f,.56f,.66f));skyMat.SetColor("_GroundColor",new Color(.44f,.37f,.29f));skyMat.SetFloat("_AtmosphereThickness",1.05f);skyMat.SetFloat("_Exposure",1);skyMat.SetFloat("_SunSize",.035f);RenderSettings.skybox=skyMat;EditorUtility.SetDirty(skyMat);
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder+"/FivePM_Afternoon.asset");
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Folder+"/FivePM_Afternoon.asset");}
        Tonemapping tone;if(!profile.TryGet(out tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
        ColorAdjustments colors;if(!profile.TryGet(out colors)){colors=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(colors,profile);}colors.postExposure.Override(-.2f);colors.contrast.Override(0);colors.saturation.Override(3);colors.colorFilter.Override(Color.white);
        Bloom bloom;if(!profile.TryGet(out bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.intensity.Override(.16f);bloom.threshold.Override(1.2f);bloom.scatter.Override(.55f);
        foreach(var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)){v.enabled=false;v.sharedProfile=profile;EditorUtility.SetDirty(v);}
        var global=GameObject.Find("Global Volume");if(global==null)global=new GameObject("Global Volume");var volume=global.GetComponent<Volume>();if(volume==null)volume=global.AddComponent<Volume>();volume.sharedProfile=profile;volume.enabled=true;volume.isGlobal=true;volume.priority=100;volume.weight=1;
        foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){c.backgroundColor=Sky;c.clearFlags=CameraClearFlags.SolidColor;c.allowHDR=true;c.GetUniversalAdditionalCameraData().renderPostProcessing=true;}
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(pipeline!=null){pipeline.supportsHDR=true;pipeline.shadowDistance=160;pipeline.mainLightShadowmapResolution=2048;EditorUtility.SetDirty(pipeline);}
        foreach(var entry in new[]{new {name="Shadowed Forest Foliage",color=new Color(.23f,.34f,.19f)},new {name="Dark Woodland Earth",color=new Color(.28f,.29f,.17f)},new {name="Moss Shadow Rock",color=new Color(.34f,.37f,.28f)}})
        {var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/ForestRealm/Finishing/"+entry.name+".mat");if(m!=null){m.SetColor("_BaseColor",entry.color);EditorUtility.SetDirty(m);}}
        foreach(var c in profile.components)EditorUtility.SetDirty(c);EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("5 PM lighting applied across forest, combat clearings, and cathedral kingdom.");
    }
}
