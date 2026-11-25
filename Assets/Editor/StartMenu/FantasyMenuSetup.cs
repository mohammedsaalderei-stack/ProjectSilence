using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

public static class FantasyMenuSetup
{
    static Color gold=new Color(.67f,.51f,.27f),cream=new Color(.92f,.84f,.64f),ink=new Color(.025f,.055f,.075f);
    static Font heading,body;
    static RectTransform R(string name,Transform parent,float w,float h,float x=0,float y=0)
    {var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x,y);return r;}
    static Image I(RectTransform r,Color color){var i=r.gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i;}
    static Text T(string n,Transform p,string text,int size,float w,float h,float x=0,float y=0,bool serif=true)
    {var r=R(n,p,w,h,x,y);var t=r.gameObject.AddComponent<Text>();t.text=text;t.font=serif?heading:body;t.fontSize=size;t.color=cream;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
    static RectTransform Frame(string n,Transform p,float w,float h,float x=0,float y=0)
    {var r=R(n,p,w,h,x,y);I(r,gold);I(R("Inset",r,w-4,h-4),ink);return r;}
    static Button B(string n,Transform p,float x,float y,float w=390)
    {
        var r=Frame(n,p,w,68,x,y);var i=r.GetComponent<Image>();i.raycastTarget=true;
        var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.Find("Inset").GetComponent<Image>();
        var c=button.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.9f,1.8f,1.55f);c.selectedColor=c.highlightedColor;c.pressedColor=new Color(.55f,.75f,.85f);button.colors=c;
        T("Label",r,n,25,w-22,58);r.gameObject.AddComponent<MenuButtonFeedback>();return button;
    }
    static Slider S(string name,Transform parent,float y,out Text value)
    {
        T(name+" Label",parent,name,23,380,45,0,y+48);value=T(name+" Percentage",parent,"",23,85,40,240,y,false);
        var r=R(name+" Slider",parent,390,40,0,y);var hit=I(r,Color.clear);hit.raycastTarget=true;var s=r.gameObject.AddComponent<Slider>();s.minValue=0;s.maxValue=1;
        var track=I(R("Track",r,390,5),new Color(.13f,.23f,.28f));track.raycastTarget=true;
        var fillArea=R("Fill Area",r,374,5);var fill=I(R("Fill",fillArea,0,0),gold);s.fillRect=fill.rectTransform;s.fillRect.sizeDelta=Vector2.zero;
        var handleArea=R("Handle Area",r,374,27);var handle=I(R("Handle",handleArea,17,0),cream);handle.raycastTarget=true;s.handleRect=handle.rectTransform;s.handleRect.sizeDelta=new Vector2(17,0);s.targetGraphic=handle;s.direction=Slider.Direction.LeftToRight;
        var nav=s.navigation;nav.mode=Navigation.Mode.Vertical;s.navigation=nav;return s;
    }
    [MenuItem("Tools/Start Menu/Apply Fantasy Styling And Settings")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Menu")EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        heading=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/CinzelDecorative/CinzelDecorative-Regular.ttf");body=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(heading==null)throw new System.Exception("Fantasy font has not imported.");
        var menu=Object.FindAnyObjectByType<SimpleStartMenu>();var canvas=menu.titleScreen.transform;
        while(canvas.childCount>0)Object.DestroyImmediate(canvas.GetChild(0).gameObject);
        var bg=R("Deep Blue Background",canvas,1920,1080);bg.anchorMin=Vector2.zero;bg.anchorMax=Vector2.one;bg.offsetMin=bg.offsetMax=Vector2.zero;I(bg,new Color(.012f,.027f,.043f));
        var outer=R("Outer Gold Border",canvas,1770,930);I(outer,new Color(.28f,.26f,.19f));I(R("Inner Background",outer,1768,928),new Color(.018f,.039f,.056f));
        var inner=R("Inner Gold Border",canvas,1738,898);I(inner,new Color(.12f,.19f,.21f));I(R("Inner Background",inner,1736,896),new Color(.018f,.039f,.056f));
        foreach(float x in new[]{-850f,850f})foreach(float y in new[]{-430f,430f}){var d=R("Corner Diamond",canvas,9,9,x,y);d.localRotation=Quaternion.Euler(0,0,45);I(d,gold);}
        var main=R("Main Menu",canvas,1100,730);
        var title=T("Silence Title",main,"SILENCE",112,1100,170,0,152);title.gameObject.AddComponent<Shadow>().effectColor=new Color(0,0,0,.7f);title.GetComponent<Shadow>().effectDistance=new Vector2(0,-4);
        foreach(float x in new[]{-175f,175f})I(R("Title Gold Rule",main,290,1,x,30),gold);
        var gem=R("Title Diamond",main,12,12,0,30);gem.localRotation=Quaternion.Euler(0,0,45);I(gem,cream);
        menu.startButton=B("Start Game",main,0,-87);UnityEventTools.AddPersistentListener(menu.startButton.onClick,menu.StartGame);
        var settingsButton=B("Settings",main,0,-179);
        var panel=Frame("Settings Panel",canvas,800,700);
        T("Settings Heading",panel,"SETTINGS",45,680,75,0,265);
        I(R("Settings Divider",panel,570,1,0,205),gold);
        var settings=menu.GetComponent<MenuSettings>();if(settings==null)settings=menu.gameObject.AddComponent<MenuSettings>();
        settings.mainPanel=main.gameObject;settings.settingsPanel=panel.gameObject;settings.settingsButton=settingsButton;
        settings.master=S("Master Volume",panel,104,out settings.masterValue);settings.music=S("Menu Music",panel,-20,out settings.musicValue);settings.effects=S("Button Sounds",panel,-144,out settings.effectsValue);
        settings.backButton=B("Back",panel,0,-267,280);
        UnityEventTools.AddPersistentListener(settingsButton.onClick,settings.Open);UnityEventTools.AddPersistentListener(settings.backButton.onClick,settings.Close);panel.gameObject.SetActive(false);
        var old=GameObject.Find("Audio - Menu");if(old!=null)Object.DestroyImmediate(old);var audio=new GameObject("Audio - Menu").AddComponent<MenuAudio>();
        audio.hover=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/UI_Hover.wav");audio.press=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/UI_Press.wav");audio.adjust=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/UI_Adjust.wav");audio.ambience=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/Menu_Quiet_Realms.wav");
        EditorUtility.SetDirty(menu);EditorUtility.SetDirty(settings);EditorUtility.SetDirty(audio);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);EditorSceneManager.SaveScene(menu.gameObject.scene);
    }
}
