using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using System.Linq;

public static class SimpleStartMenuSetup
{
    static readonly Color Ivory=new Color(.94f,.90f,.81f);
    static readonly Color Gold=new Color(.64f,.49f,.27f);
    static Font font;
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var g=new GameObject(name,typeof(RectTransform));var r=(RectTransform)g.transform;r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    static Text Label(string name,Transform parent,string value,int size,Vector2 dimensions,Vector2 position)
    {
        var r=Rect(name,parent,dimensions,position);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=Ivory;t.raycastTarget=false;return t;
    }
    [MenuItem("Tools/Start Menu/Create Simple Silence Menu")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
        if(GameObject.Find("Start Menu - Silence")!=null)throw new System.InvalidOperationException("Menu already exists.");
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root=new GameObject("Start Menu - Silence");Undo.RegisterCreatedObjectUndo(root,"Create start menu");var menu=root.AddComponent<SimpleStartMenu>();
        var screen=new GameObject("Title Screen",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));screen.transform.SetParent(root.transform,false);
        var canvas=screen.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
        var scale=screen.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
        var background=Rect("Background",screen.transform,Vector2.zero,Vector2.zero);background.anchorMin=Vector2.zero;background.anchorMax=Vector2.one;background.offsetMin=background.offsetMax=Vector2.zero;background.gameObject.AddComponent<Image>().color=new Color(.055f,.065f,.085f,1);
        Label("Game Title",screen.transform,"SILENCE",92,new Vector2(1100,145),new Vector2(0,95));
        var line=Rect("Simple Gold Divider",screen.transform,new Vector2(210,2),new Vector2(0,-5));line.gameObject.AddComponent<Image>().color=Gold;
        var br=Rect("Start Game",screen.transform,new Vector2(310,72),new Vector2(0,-118));var img=br.gameObject.AddComponent<Image>();img.color=new Color(.13f,.15f,.19f,1);
        var outline=br.gameObject.AddComponent<Outline>();outline.effectColor=Gold;outline.effectDistance=new Vector2(1,-1);
        var button=br.gameObject.AddComponent<Button>();button.targetGraphic=img;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.25f,1.2f,1.1f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.75f,.72f,.67f);button.colors=colors;
        Label("Button Label",br,"Start Game",29,new Vector2(300,64),Vector2.zero);
        menu.titleScreen=screen;menu.player=GameObject.Find("Musical Mage Player");var spawn=GameObject.Find("Player Spawn - Kingdom Plaza");menu.kingdomSpawn=spawn!=null?spawn.transform:null;menu.followCamera=Camera.main!=null?Camera.main.GetComponent<MusicalMageFollowCamera>():null;menu.startButton=button;menu.kingdomSceneName="Kingdom";
        UnityEventTools.AddPersistentListener(button.onClick,menu.StartGame);
        if(Object.FindAnyObjectByType<EventSystem>()==null){var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(root.transform,false);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var existing=EditorBuildSettings.scenes.Where(s=>s.path!=scene.path).ToList();existing.Insert(0,new EditorBuildSettingsScene(scene.path,true));EditorBuildSettings.scenes=existing.ToArray();
        EditorUtility.SetDirty(menu);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=root;
        Debug.Log("SILENCE_MENU_READY: Start Game spawns the player in the kingdom.");
    }
}
