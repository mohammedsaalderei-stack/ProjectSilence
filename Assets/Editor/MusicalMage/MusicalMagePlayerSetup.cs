using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static class MusicalMagePlayerSetup
{
    [MenuItem("Tools/Musical Mage/Add Playable 2.5D Character")]
    public static void Build()
    {
        if(GameObject.Find("Musical Mage Player")!=null)throw new System.InvalidOperationException("The player is already in this scene.");
        var root=GameObject.CreatePrimitive(PrimitiveType.Capsule);root.name="Musical Mage Player";
        Undo.RegisterCreatedObjectUndo(root,"Add 2.5D player");
        Object.DestroyImmediate(root.GetComponent<CapsuleCollider>());
        // Keep the capsule shape for selection/debugging, hide its primitive surface in game.
        root.GetComponent<MeshRenderer>().enabled=false;
        var cc=root.AddComponent<CharacterController>();cc.height=1.75f;cc.radius=.28f;cc.center=new Vector3(0,.875f,0);cc.stepOffset=.35f;cc.slopeLimit=55;cc.skinWidth=.025f;cc.minMoveDistance=0;
        root.tag="Player";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MusicalMage/MusicalMageMovement.prefab");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(source);visual.name="2D Character - Animated Billboard";
        visual.transform.SetParent(root.transform,false);visual.transform.localPosition=new Vector3(0,.015f,0);
        visual.transform.rotation=Camera.main.transform.rotation;
        var controller=root.AddComponent<MusicalMagePlayer>();controller.spriteAnimator=visual.GetComponent<Animator>();controller.spriteRenderer=visual.GetComponent<SpriteRenderer>();controller.movementCamera=Camera.main;
        string[] dirs={"S","SW","W","NW","N","NE","E","SE"};
        for(int i=0;i<8;i++)controller.standingFrames[i]=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MusicalMage/Sprites/Walk_"+dirs[i]+"_00.png");
        var spawn=GameObject.Find("Player Spawn - Clearing");Vector3 position=spawn!=null?spawn.transform.position:new Vector3(0,1,-1.5f);
        RaycastHit hit;if(Physics.Raycast(new Vector3(position.x,8,position.z),Vector3.down,out hit,20))position.y=hit.point.y+.03f;
        cc.enabled=false;root.transform.position=position;cc.enabled=true;
        Directory.CreateDirectory("Assets/MusicalMage/Player");
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/MusicalMage/Player/MusicalMagePlayer.prefab");
        Selection.activeGameObject=root;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("PLAYABLE_MAGE_READY: WASD/arrows move; Shift sprints. Camera-relative eight-direction 2D sprites, 3D capsule collisions.");
    }
}
