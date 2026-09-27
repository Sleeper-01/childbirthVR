using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class 中文资产迁移
{
    static readonly Dictionary<string,string> Types=new Dictionary<string,string>{
        {"LessonCatalog","运动讲解配置"},{"ExerciseLesson","运动项目"},{"LessonController","讲解控制器"},
        {"DeepSeekService","智能问答服务"},{"DialoguePanel","对话面板"},{"QuestionInput","文字输入"},
        {"LessonVideo","视频控制器"},{"FirstPersonWalker","历史第一人称控制器"},{"NurseProximity","历史接近触发器"},
        {"PrenatalSceneBuilder","历史演示生成器"},{"FirstPersonSceneBuilder","历史第一人称生成器"},{"PrenatalSmokeCheck","讲解自检"}
    };
    static readonly Dictionary<string,string> Names=new Dictionary<string,string>{
        {"Ground","地板"},{"Floor","地板材质"},{"Nurse","护士"},{"Presentation Camera","固定摄像机"},{"Main Light","主光源"},
        {"EventSystem","界面事件系统"},{"Dialogue Canvas","交互画布"},{"Header","标题背景"},{"Title","标题"},{"Subtitle","副标题"},{"Mode","说明文字"},
        {"Prenatal Dialogue System","对话管理器"},{"Exercise Heading","运动标题"},{"Exercise Menu","运动选择区"},{"Exercise ","运动 "},{"Nurse Label","护士标识"},
        {"Video Area","视频区"},{"16 by 9 Viewport","视频视口"},{"Video Screen","视频画面"},{"Lesson Video","视频渲染纹理"},{"Video Placeholder","视频占位提示"},
        {"Play Video","播放按钮"},{"Replay Video","重播按钮"},{"Current Dialogue","底部对话框"},{"Speaker","角色名称"},{"Current Text","当前对话文字"},
        {"Page","页码"},{"Status","状态提示"},{"Safety","安全提示按钮"},{"Restart Lesson","重看按钮"},{"Resume Lesson","继续讲解按钮"},{"Next","下一句按钮"},
        {"Question Input","文字输入框"},{"Text Area","输入文字区域"},{"Input Text","输入文字"},{"Placeholder","输入占位提示"},{"Send Question","发送按钮"},{"Label","按钮文字"},
        {"Boundary North","北侧边界"},{"Boundary South","南侧边界"},{"Boundary East","东侧边界"},{"Boundary West","西侧边界"},{"Player","玩家占位"},
        {"Player Capsule","玩家胶囊"},{"First Person Camera","历史第一人称摄像机"},{"Panel Background","面板背景"},{"End Conversation","结束对话按钮"},
        {"World Video Screen","历史空间视频屏"},{"Video Stand","视频支架"},{"Video Base","视频底座"},{"Exploration HUD","历史探索提示"},
        {"Crosshair","准星"},{"Movement Hint","移动提示"},{"Nurse Name","护士名称"},{"Nurse Name Canvas","护士名称画布"}
    };
    static string NewPath(string path)
    {
        path=path.Replace("Assets/Prenatal","Assets/产前运动").Replace("/Scenes","/场景").Replace("/Scripts","/脚本").Replace("/Data","/配置").Replace("/Fonts","/字体");
        path=path.Replace("PrenatalDemo.unity","历史界面演示.unity").Replace("PrenatalFirstPerson.unity","历史第一人称演示.unity").Replace("BuildRecovery.unity","历史搭建恢复.unity")
            .Replace("Lessons.asset","运动讲解配置.asset").Replace("Floor.mat","地板材质.mat").Replace("Nurse.mat","护士.mat").Replace("LessonVideo.renderTexture","视频渲染纹理.renderTexture").Replace("Chinese SDF.asset","中文字体.asset").Replace("README.md","使用说明.md");
        return path;
    }
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("请先停止运行");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        foreach(var path in Directory.GetFiles("Assets/Prenatal","*.unity",SearchOption.AllDirectories))
        {
            var scene=EditorSceneManager.OpenScene(path.Replace('\\','/'));
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(Names.TryGetValue(t.name,out var name))t.name=name;
                else if(t.name.StartsWith("Exercise "))t.name=t.name.Replace("Exercise ","运动 ");
            }
            EditorSceneManager.SaveScene(scene);
        }
        // Close scenes before moving their assets. Saved copies above retain every GUID reference.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var codeFiles=Directory.GetFiles("Assets/Prenatal","*.cs",SearchOption.AllDirectories).Where(p=>!p.EndsWith("中文资产迁移.cs")).ToArray();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var path in codeFiles)
            {
                var text=File.ReadAllText(path);
                // Change asset paths before type names (LessonVideo is both a type and old texture filename).
                text=NewPath(text);
                foreach(var item in Types)text=Regex.Replace(text,@"\b"+item.Key+@"\b",item.Value);
                foreach(var item in Names)text=text.Replace("\""+item.Key+"\"","\""+item.Value+"\"");
                text=text.Replace("namespace Prenatal","namespace 产前运动").Replace("using Prenatal;","using 产前运动;");
                text=text.Replace("Prenatal/Create Demo Scene","产前运动/打开历史界面演示").Replace("Prenatal/Create First Person Scene","产前运动/打开历史第一人称演示").Replace("Prenatal/Run Play Mode Smoke Check","产前运动/运行讲解自检");
                File.WriteAllText(path,text,new UTF8Encoding(false));
                var basename=System.IO.Path.GetFileNameWithoutExtension(path);
                if(Types.TryGetValue(basename,out var name))Move(path.Replace('\\','/'),(System.IO.Path.GetDirectoryName(path)+"/"+name+".cs").Replace('\\','/'));
            }
            foreach(var pair in new[]{new[]{"Scenes/PrenatalDemo.unity","Scenes/历史界面演示.unity"},new[]{"Scenes/PrenatalFirstPerson.unity","Scenes/历史第一人称演示.unity"},new[]{"Scenes/BuildRecovery.unity","Scenes/历史搭建恢复.unity"},new[]{"Data/Lessons.asset","Data/运动讲解配置.asset"},new[]{"Data/Floor.mat","Data/地板材质.mat"},new[]{"Data/Nurse.mat","Data/护士.mat"},new[]{"Data/LessonVideo.renderTexture","Data/视频渲染纹理.renderTexture"},new[]{"Fonts/Chinese SDF.asset","Fonts/中文字体.asset"},new[]{"README.md","使用说明.md"}})
                Move("Assets/Prenatal/"+pair[0],"Assets/Prenatal/"+pair[1]);
            foreach(var pair in new[]{new[]{"Scenes","场景"},new[]{"Scripts","脚本"},new[]{"Data","配置"},new[]{"Fonts","字体"}})Move("Assets/Prenatal/"+pair[0],"Assets/Prenatal/"+pair[1]);
            Move("Assets/Prenatal","Assets/产前运动");
        }
        finally { AssetDatabase.StopAssetEditing(); AssetDatabase.Refresh(); }
    }
    static void Move(string from,string to)
    {var error=AssetDatabase.MoveAsset(from,to);if(!string.IsNullOrEmpty(error))throw new System.InvalidOperationException(error);}
}
