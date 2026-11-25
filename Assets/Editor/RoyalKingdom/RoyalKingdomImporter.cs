using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public static class RoyalKingdomImporter
{
    const string Folder="Assets/RoyalKingdom";
    const string RootName="Royal Kingdom - Cathedral District";
    static readonly Vector3 Offset=new Vector3(100,0,0);
    static string Safe(string s){return string.Concat(s.Select(c=>char.IsLetterOrDigit(c)?c:'_'));}
    static Vector3 V(JToken t){return new Vector3((float)t[0],(float)t[1],(float)t[2]);}

    [MenuItem("Tools/Royal Kingdom/Import Cathedral District")]
    public static void Import()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        if(GameObject.Find(RootName)!=null)throw new InvalidOperationException("Cathedral district already exists.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/SampleScene.unity")throw new InvalidOperationException("Open SampleScene first.");
        Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Backups");
        EditorSceneManager.SaveScene(scene,Folder+"/Backups/BeforeCathedral_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
        var manifest=JObject.Parse(File.ReadAllText(Folder+"/kingdom_manifest.json"));
        var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/Models/RoyalKingdomCathedral.fbx");
        importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.addCollider=false;importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Models/RoyalKingdomCathedral.fbx");
        var root=new GameObject(RootName);Undo.RegisterCreatedObjectUndo(root,"Add cathedral kingdom");root.transform.position=Offset;
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(model);visual.name="Updated Blender Kingdom - Cathedral";
        visual.transform.SetParent(root.transform,false);visual.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
        PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var materials=new Dictionary<string,Material>();
        foreach(var item in manifest["materials"])
        {
            string name=(string)item["name"],path=Folder+"/Materials/"+Safe(name)+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.name=name;var c=item["color"];mat.SetColor("_BaseColor",new Color((float)c[0],(float)c[1],(float)c[2],1).gamma);
            mat.SetFloat("_Metallic",(float)item["metallic"]);mat.SetFloat("_Smoothness",(float)item["smoothness"]);mat.SetFloat("_Cull",0);
            var e=V(item["emission"]);if(e.sqrMagnitude>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(e.x,e.y,e.z));}
            EditorUtility.SetDirty(mat);materials[name]=mat;
        }
        var source=((JArray)manifest["objects"]).ToDictionary(t=>(string)t["name"],t=>t);
        int matched=0,meshCount=0;var mismatches=new JArray();
        foreach(var r in visual.GetComponentsInChildren<MeshRenderer>())
        {
            JToken data;if(!source.TryGetValue(r.name,out data))throw new InvalidOperationException("Missing Blender object "+r.name);
            r.sharedMaterials=((JArray)data["materials"]).Select(m=>materials[(string)m]).ToArray();
            float error=Vector3.Distance(r.bounds.center,V(data["center"])+Offset);
            if(error<.035f)matched++;else mismatches.Add(new JObject{{"name",r.name},{"error",error}});
            r.gameObject.isStatic=true;
            string group=(string)data["group"],n=(string)data["sourceName"];
            bool solid=group=="Terrain"||group=="Roads_And_Plazas"||group=="Foundation_And_Terrace"||group=="Nave_And_Side_Aisles"||group=="Entrance_Facade"||group=="Bell_Towers"||group=="Buttresses"||group=="Stairs_And_Ramp"||group=="Walls_And_Gates"||group=="Shops_And_Market"||group=="Doors_And_Entrance_Arches"||group=="Props";
            if(group=="Vegetation"&&(n.ToLowerInvariant().Contains("trunk")||n.ToLowerInvariant().Contains("boulder")))solid=true;
            if(group=="Water_And_Magic"&&n.StartsWith("Fountain chunky basin rim"))solid=true;
            if(solid){var mc=r.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;meshCount++;}
        }
        var borders=new GameObject("Safety Borders - No Falls");borders.transform.SetParent(root.transform,false);
        Box(borders.transform,"West district border",new Vector3(-29.65f,2,0),new Vector3(.5f,5,40));
        Box(borders.transform,"East district border",new Vector3(29.65f,2,0),new Vector3(.5f,5,40));
        Box(borders.transform,"South district border",new Vector3(0,2,-19.65f),new Vector3(60,5,.5f));
        foreach(float x in new[]{-21.3f,21.3f})Box(borders.transform,"North garden edge",new Vector3(x,2,19.65f),new Vector3(17.4f,5,.5f));
        foreach(float x in new[]{-12.7f,12.7f})Box(borders.transform,"Cathedral rear support edge",new Vector3(x,3,22.6f),new Vector3(.45f,7,6.4f));
        Box(borders.transform,"Cathedral northern boundary",new Vector3(0,3,25.7f),new Vector3(26,7,.5f));
        // Low-friction collision surfaces let the existing capsule climb the 25cm steps.
        var pm=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Folder+"/Materials/KingdomCollision.asset");
        if(pm==null){pm=new PhysicsMaterial("Kingdom low snag stone"){dynamicFriction=.2f,staticFriction=.2f,bounciness=0};AssetDatabase.CreateAsset(pm,Folder+"/Materials/KingdomCollision.asset");}
        foreach(var c in root.GetComponentsInChildren<Collider>())c.sharedMaterial=pm;
        var spawn=new GameObject("Player Spawn - Kingdom Plaza");spawn.transform.SetParent(root.transform,false);spawn.transform.localPosition=new Vector3(0,.10f,-9);
        var forestReturn=new GameObject("Forest Return - Original Player Position");forestReturn.transform.SetParent(root.transform);
        var player=GameObject.Find("Musical Mage Player");forestReturn.transform.position=player.transform.position;
        var cc=player.GetComponent<CharacterController>();cc.stepOffset=.35f;cc.slopeLimit=55;
        SetupDaylight(root);
        var camgo=new GameObject("Kingdom Overview Camera - Preview Only");camgo.transform.SetParent(root.transform,false);camgo.transform.localPosition=new Vector3(0,-0f,-52)+Vector3.up*92;
        camgo.transform.LookAt(Offset+new Vector3(0,10,5));var cam=camgo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=28.125f;cam.farClipPlane=220;cam.enabled=false;cam.backgroundColor=new Color(.48f,.62f,.74f);cam.clearFlags=CameraClearFlags.SolidColor;
        Physics.SyncTransforms();
        var report=Validate(root);report["meshes"]=source.Count;report["matchedBlenderObjectPositions"]=matched;report["positionMismatches"]=mismatches;report["solidMeshColliders"]=meshCount;report["borderColliders"]=borders.GetComponentsInChildren<Collider>().Length;report["offset"]=Offset.ToString();report["gapFromForestEastBoundaryMeters"]=17.8f;
        File.WriteAllText(Folder+"/kingdom_collision_validation.json",report.ToString());
        if(!(bool)report["passed"]||mismatches.Count>0)Debug.LogError("KINGDOM_VALIDATION_REQUIRES_ATTENTION "+report);
        TravelToKingdom();
        PrefabUtility.SaveAsPrefabAsset(root,Folder+"/RoyalKingdom_WithCollisions.prefab");AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=root;
        if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(Offset+new Vector3(0,10,4),Quaternion.Euler(55,0,0),48);
        Debug.Log("ROYAL_KINGDOM_IMPORTED "+report.ToString(Newtonsoft.Json.Formatting.None));
    }

    static void Box(Transform parent,string name,Vector3 pos,Vector3 size){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.AddComponent<BoxCollider>().size=size;}
    public static void SetupDaylight(GameObject root)
    {
        LateAfternoonLighting.Apply();
    }
    [MenuItem("Tools/Royal Kingdom/Move Player To Kingdom")]
    public static void TravelToKingdom(){Travel("Player Spawn - Kingdom Plaza");}
    [MenuItem("Tools/Royal Kingdom/Move Player To Forest")]
    public static void TravelToForest(){Travel("Forest Return - Original Player Position");}
    static void Travel(string marker)
    {
        var p=GameObject.Find("Musical Mage Player");var m=GameObject.Find(marker);if(p==null||m==null)return;
        var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=m.transform.position;cc.enabled=true;
        var sun=GameObject.Find("Sun - Warm Late Afternoon");if(sun!=null)RenderSettings.sun=sun.GetComponent<Light>();
        Camera.main.backgroundColor=LateAfternoonLighting.Sky;Camera.main.clearFlags=CameraClearFlags.SolidColor;
        var follow=Camera.main.GetComponent<MusicalMageFollowCamera>();if(follow!=null)follow.SnapToTarget();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public static JObject Validate(GameObject root)
    {
        var test=EditorSceneManager.NewPreviewScene();
        var results=new JArray();bool passed=true;
        try
        {
            foreach(var c in root.GetComponentsInChildren<Collider>())
            {
                var g=new GameObject(c.name);SceneManager.MoveGameObjectToScene(g,test);g.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);g.transform.localScale=c.transform.lossyScale;
                var mc=c as MeshCollider;var bc=c as BoxCollider;
                if(mc!=null)g.AddComponent<MeshCollider>().sharedMesh=mc.sharedMesh;
                if(bc!=null){var co=g.AddComponent<BoxCollider>();co.size=bc.size;co.center=bc.center;}
            }
            Physics.SyncTransforms();var ps=test.GetPhysicsScene();ps.Simulate(.02f);
            // Test with the same feet-based capsule dimensions and step settings as the player.
            Vector3[] starts={new Vector3(0,.09f,4.1f),new Vector3(-12.7f,.09f,2.7f),new Vector3(0,1.6f,10),new Vector3(27.5f,.1f,-7),new Vector3(0,.1f,-18.7f),new Vector3(0,.1f,-8),new Vector3(0,1.6f,10),new Vector3(-12.7f,1.6f,11),new Vector3(-12.7f,1.6f,11.8f)};
            Vector3[] dirs={Vector3.forward,Vector3.forward,Vector3.forward,Vector3.right,Vector3.back,Vector3.right,Vector3.back,Vector3.back,Vector3.right};
            float[] lengths={6.1f,8.4f,5,4,4,8,6,8.6f,4};
            string[] names={"Central stairs climb","Side ramp climb","Closed cathedral doors block player","Eastern border blocks falls","Southern border blocks falls","Open plaza traversable","Central stairs descent","Side ramp descent","Ramp landing joins terrace"};
            for(int k=0;k<starts.Length;k++)
            {
                var g=new GameObject("Player collision probe");SceneManager.MoveGameObjectToScene(g,test);g.transform.position=Offset+starts[k];
                var cc=g.AddComponent<CharacterController>();cc.height=1.75f;cc.radius=.28f;cc.center=new Vector3(0,.875f,0);cc.stepOffset=.35f;cc.skinWidth=.025f;cc.slopeLimit=55;cc.minMoveDistance=0;
                ps.Simulate(.02f);for(int j=0;j<Mathf.CeilToInt(lengths[k]/.04f);j++){cc.Move(dirs[k]*.04f+Vector3.down*.06f);ps.Simulate(.02f);}
                var p=g.transform.position-Offset;bool ok=false;
                if(k==0)ok=p.y>1.45f&&p.z>9.6f;
                if(k==1)ok=p.y>1.42f&&p.z>10.7f;
                if(k==2)ok=p.z<13.2f&&p.z>10.3f&&p.y>1.4f;
                if(k==3)ok=p.x<29.5f&&p.x>27.4f&&p.y>-.05f;
                if(k==4)ok=p.z> -19.5f&&p.y>-.05f;
                if(k==5)ok=p.x>7.5f&&p.y>-.05f;
                if(k==6)ok=p.z<4.3f&&p.y<.2f&&p.y>-.05f;
                if(k==7)ok=p.z<2.8f&&p.y<.2f&&p.y>-.05f;
                if(k==8)ok=p.x> -9&&p.y>1.4f;
                passed&=ok;results.Add(new JObject{{"test",names[k]},{"passed",ok},{"localPosition",p.ToString("F3")}});UnityEngine.Object.DestroyImmediate(g);
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(test);}
        return new JObject{{"passed",passed},{"characterControllerChecks",results}};
    }
}
