using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public static class CombatExtensionImporter
{
    const string Folder="Assets/ForestRealm/CombatExtension";
    const string Name="Forest Realm - Right Combat Extension";
    static string Safe(string s){return string.Concat(s.Select(c=>char.IsLetterOrDigit(c)||c=='-'?c:'_'));}
    static Vector3 V(JToken p){return new Vector3((float)p[0],(float)p[1],(float)p[2]);}
    public static void Import()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit play mode before importing.");
        if(GameObject.Find(Name)!=null)throw new InvalidOperationException("The extension is already placed.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/SampleScene.unity")throw new InvalidOperationException("Open SampleScene first.");
        Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Backups");
        EditorSceneManager.SaveScene(scene,Folder+"/Backups/BeforeExtension_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
        var manifest=JObject.Parse(File.ReadAllText(Folder+"/forest_manifest.json"));
        var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/Models/ForestRealm.fbx");
        importer.isReadable=true;importer.importLights=false;importer.importCameras=false;importer.importAnimation=false;importer.addCollider=false;importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Models/ForestRealm.fbx");
        var root=new GameObject(Name);Undo.RegisterCreatedObjectUndo(root,"Add connected combat extension");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(model);visual.name="Blender Extension - Editable Geometry";
        visual.transform.SetParent(root.transform,false);visual.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
        PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var materials=new Dictionary<string,Material>();
        foreach(var item in manifest["materials"])
        {
            string name=(string)item["name"];Material mat=null;
            if(name.StartsWith("Amethyst"))
                mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/ForestRealm/Finishing/"+name+" - Twilight Glow.mat");
            if(mat==null)mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/ForestRealm/Materials/"+Safe(name)+".mat");
            if(mat==null)
            {
                mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name=name;
                var col=item["color"];mat.SetColor("_BaseColor",new Color((float)col[0],(float)col[1],(float)col[2],1).gamma);
                mat.SetFloat("_Metallic",(float)item["metallic"]);mat.SetFloat("_Smoothness",(float)item["smoothness"]);mat.SetFloat("_Cull",0);
                string texture=(string)item["texture"];
                if(!string.IsNullOrEmpty(texture)){mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));mat.SetColor("_BaseColor",Color.white);}
                AssetDatabase.CreateAsset(mat,Folder+"/Materials/"+Safe(name)+".mat");
            }
            materials[name]=mat;
        }
        var source=((JArray)manifest["objects"]).ToDictionary(o=>(string)o["name"],o=>o);
        int colliders=0,matched=0;var mismatches=new JArray();
        var physicsMat=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Physics/ForestRealm/ForestCollision.asset");
        foreach(var r in visual.GetComponentsInChildren<MeshRenderer>())
        {
            JToken data;if(source.TryGetValue(r.name,out data))
            {
                int count=r.GetComponent<MeshFilter>().sharedMesh.subMeshCount;
                r.sharedMaterials=((JArray)data["materials"]).Select(m=>materials[(string)m]).Take(count).ToArray();
                float error=Vector3.Distance(r.bounds.center,V(data["center"]));if(error<.035f)matched++;else mismatches.Add(new JObject{{"name",r.name},{"error",error}});
            }
            r.gameObject.isStatic=true;
            string n=r.name;
            if(n.StartsWith("Extension • solid")||n.StartsWith("Hub east exit")||n.StartsWith("Sparse ancient")||n.StartsWith("Clearing • worn")||n.Contains("tapered trunk")||n.StartsWith("Buttress root")||n.StartsWith("Framing mossy boulder")||n.StartsWith("Fallen hollow")||n.StartsWith("Ancient boundary")||n.StartsWith("Faceted violet"))
            {var c=r.gameObject.AddComponent<MeshCollider>();c.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;c.sharedMaterial=physicsMat;colliders++;}
        }
        // Replace only the solid eastern wall's middle section with an accessible doorway.
        var oldWall=GameObject.Find("East map edge");if(oldWall!=null)oldWall.SetActive(false);
        Wall(root.transform,"East hub border - north of exit",new Vector3(15.7f,3,8.3f),new Vector3(.6f,8,11.4f));
        Wall(root.transform,"East hub border - south of exit",new Vector3(15.7f,3,-8.3f),new Vector3(.6f,8,11.4f));
        Wall(root.transform,"Extension north safety boundary",new Vector3(34,3,12.7f),new Vector3(37.2f,8,.6f));
        Wall(root.transform,"Extension south safety boundary",new Vector3(34,3,-10.7f),new Vector3(37.2f,8,.6f));
        Wall(root.transform,"Extension east safety boundary",new Vector3(52.2f,3,1),new Vector3(.6f,8,24));
        var scenery=GameObject.Find("Forest Realm - Twilight Extension");int retired=0;
        if(scenery!=null)
        {
            foreach(Transform child in scenery.transform)
                if(child.name.StartsWith("UNS_")&&child.position.x>15.5f&&child.position.z>-11&&child.position.z<13){child.gameObject.SetActive(false);retired++;}
            var collar=scenery.transform.Find("Small forest continuation - scenic ground");
            if(collar!=null)
            {
                var filter=collar.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="Scenic collar with open east join";
                mesh.triangles=mesh.triangles.Take(6).Concat(mesh.triangles.Skip(12)).ToArray();
                AssetDatabase.CreateAsset(mesh,Folder+"/Models/ScenicCollar_OpenEast.asset");filter.sharedMesh=mesh;
            }
        }
        foreach(var spawn in manifest["enemySpawns"])
        {var marker=new GameObject((string)spawn["name"]);marker.transform.SetParent(root.transform);marker.transform.position=V(spawn["position"]);}
        var interior=GameObject.Find("Map Interior - Broad Moonlight");
        if(interior!=null)
        {var fill=UnityEngine.Object.Instantiate(interior,root.transform);fill.name="Combat Extension - Interior Moonlight";fill.transform.position=new Vector3(34,45,1);fill.GetComponent<Light>().spotAngle=50;fill.GetComponent<Light>().innerSpotAngle=38;fill.GetComponent<Light>().renderMode=LightRenderMode.ForcePixel;}
        var clusters=new List<Vector3>();
        foreach(var r in visual.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Faceted violet")))
        {
            if(clusters.Any(p=>Vector3.Distance(p,r.bounds.center)<2))continue;var p0=r.bounds.center;clusters.Add(p0);
            var light=new GameObject("Extension crystal glow");light.transform.SetParent(root.transform);light.transform.position=p0+Vector3.up*.3f;
            var l=light.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.57f,.24f,1);l.intensity=2.8f;l.range=4.4f;
        }
        // Overview is inactive; the player's existing follow camera remains the gameplay camera.
        var overview=new GameObject("Extension Overview Camera");overview.transform.SetParent(root.transform);overview.transform.position=new Vector3(33,43,-35);overview.transform.LookAt(new Vector3(33,1.8f,1));
        var oc=overview.AddComponent<Camera>();oc.orthographic=true;oc.orthographicSize=11.4f;oc.enabled=false;oc.backgroundColor=Camera.main.backgroundColor;oc.clearFlags=CameraClearFlags.SolidColor;
        Physics.SyncTransforms();
        var report=Validate(root);report["matchedBlenderObjectPositions"]=matched;report["positionMismatches"]=mismatches;report["meshColliders"]=colliders;report["oldSceneryRetired"]=retired;report["enemyMarkers"]=((JArray)manifest["enemySpawns"]).Count;
        File.WriteAllText("C:/Users/hp/Documents/Codex/2026-11-24/can/work/unity_extension_validation.json",report.ToString());
        PrefabUtility.SaveAsPrefabAsset(root,Folder+"/CombatExtension_WithCollisions.prefab");AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=root;Debug.Log("COMBAT_EXTENSION_IMPORTED "+report.ToString(Newtonsoft.Json.Formatting.None));
    }
    static void Wall(Transform parent,string name,Vector3 position,Vector3 size)
    {var o=new GameObject(name);o.transform.SetParent(parent);o.transform.position=position;o.AddComponent<BoxCollider>().size=size;}
    public static JObject Validate(GameObject root)
    {
        var test=EditorSceneManager.NewPreviewScene();var report=new JObject();var results=new JArray();
        try
        {
            foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>())
            {
                if(!c.enabled||c.isTrigger||c.gameObject.scene!=SceneManager.GetActiveScene()||c is CharacterController)continue;
                var go=new GameObject("Test "+c.name);SceneManager.MoveGameObjectToScene(go,test);go.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);go.transform.localScale=c.transform.lossyScale;
                var mc=c as MeshCollider;var bc=c as BoxCollider;
                if(mc!=null){var co=go.AddComponent<MeshCollider>();co.sharedMesh=mc.sharedMesh;}
                if(bc!=null){var co=go.AddComponent<BoxCollider>();co.center=bc.center;co.size=bc.size;}
            }
            var bodies=new List<Rigidbody>();
            Vector3[] spots={new Vector3(15,3,0),new Vector3(18,3,0),new Vector3(24,3,.5f),new Vector3(33,3,2),new Vector3(41,3,2.5f),new Vector3(50.5f,1.5f,1),new Vector3(41,1.5f,11),new Vector3(24,1.5f,-9)};
            for(int i=0;i<spots.Length;i++)
            {var go=new GameObject("Player probe "+i);SceneManager.MoveGameObjectToScene(go,test);go.transform.position=spots[i];var cap=go.AddComponent<CapsuleCollider>();cap.height=1.75f;cap.radius=.28f;var body=go.AddComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeRotation;if(i==5)body.linearVelocity=Vector3.right*5;if(i==6)body.linearVelocity=Vector3.forward*5;if(i==7)body.linearVelocity=Vector3.back*5;bodies.Add(body);}
            Physics.SyncTransforms();var physics=test.GetPhysicsScene();for(int i=0;i<150;i++)physics.Simulate(.02f);
            bool passed=true;
            for(int i=0;i<bodies.Count;i++){var p=bodies[i].position;bool ok=p.y>-.1f&&p.x<52&&p.z<12.5f&&p.z>-10.5f;passed&=ok;results.Add(new JObject{{"probe",i},{"passed",ok},{"position",p.ToString("F3")}});}
            var hits=new RaycastHit[100];int count=physics.CapsuleCast(new Vector3(12,1.1f,0),new Vector3(12,2.1f,0),.28f,Vector3.right,hits,9,~0,QueryTriggerInteraction.Ignore);
            bool exitOpen=true;for(int i=0;i<count;i++)if(hits[i].collider.name.Contains("border")||hits[i].collider.name.Contains("map edge"))exitOpen=false;
            report["exitOpen"]=exitOpen;report["passed"]=passed&&exitOpen;report["physicsProbes"]=results;
        }
        finally{EditorSceneManager.ClosePreviewScene(test);}
        return report;
    }
}
