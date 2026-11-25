using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;
using System.IO;
public static class MusicalMageAnimations
{
    const string Folder="Assets/MusicalMage";
    static readonly string[] Directions={"S","SW","W","NW","N","NE","E","SE"};
    [MenuItem("Tools/Musical Mage/Build Movement Clips")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder+"/Animations");
        foreach(string file in Directory.GetFiles(Folder+"/Sprites","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=120;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,.1f);importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        var controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/MusicalMageMovement.controller");
        controller.AddParameter("Direction",AnimatorControllerParameterType.Int);controller.AddParameter("Sprint",AnimatorControllerParameterType.Bool);
        var sm=controller.layers[0].stateMachine;
        foreach(string action in new[]{"Walk","Sprint"})for(int direction=0;direction<8;direction++)
        {
            string name=action+"_"+Directions[direction];var clip=new AnimationClip();clip.name=name;clip.frameRate=action=="Walk"?7:12;
            var keys=new ObjectReferenceKeyframe[5];
            for(int frame=0;frame<5;frame++)
            {
                string path=Folder+"/Sprites/"+name+"_"+(frame%4).ToString("00")+".png";
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite==null)throw new System.Exception("Missing "+path);
                keys[frame]=new ObjectReferenceKeyframe{time=frame/clip.frameRate,value=sprite};
            }
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
            AssetDatabase.CreateAsset(clip,Folder+"/Animations/"+name+".anim");
            var state=sm.AddState(name);state.motion=clip;if(action=="Walk"&&direction==0)sm.defaultState=state;
            var transition=sm.AddAnyStateTransition(state);transition.hasExitTime=false;transition.duration=0;transition.canTransitionToSelf=false;
            transition.AddCondition(AnimatorConditionMode.Equals,direction,"Direction");transition.AddCondition(action=="Sprint"?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Sprint");
        }
        var go=new GameObject("Musical Mage - Eight Direction Movement");
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Sprites/Walk_S_00.png");
        var animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
        PrefabUtility.SaveAsPrefabAsset(go,Folder+"/MusicalMageMovement.prefab");Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        int count=AssetDatabase.FindAssets("t:AnimationClip",new[]{Folder+"/Animations"}).Length;
        Debug.Log("MAGE_ANIMATIONS_COMPLETE: "+count+" looping clips; 64 sprite frames; Direction S,SW,W,NW,N,NE,E,SE = 0..7; Sprint toggles run.");
    }
}
