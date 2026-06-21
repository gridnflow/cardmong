#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cardmong.EditorTools
{
    /// <summary>
    /// 모바일 플레이테스트가 한 번에 되도록 에디터를 설정한다.
    /// - 어느 씬에서 Play를 눌러도 항상 Boot(→Game)에서 시작 (playModeStartScene)
    /// - Game 뷰를 세로 1080x1920 로 강제
    /// 에디터 로드 시 자동 적용되며, 메뉴 Cardmong/Set Portrait Game View 로 수동 실행도 가능.
    /// </summary>
    [InitializeOnLoad]
    public static class MobilePlaytestSetup
    {
        private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";

        static MobilePlaytestSetup()
        {
            EditorApplication.delayCall += ApplyAll;
        }

        [MenuItem("Cardmong/Set Up Mobile Playtest")]
        public static void ApplyAll()
        {
            SetPlayModeStartScene();
            SetPortraitGameView();
        }

        private static void SetPlayModeStartScene()
        {
            var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (boot != null && EditorSceneManager.playModeStartScene != boot)
            {
                EditorSceneManager.playModeStartScene = boot;
                Debug.Log("[MobilePlaytest] Play mode will start from Boot → Game.");
            }
        }

        [MenuItem("Cardmong/Set Portrait Game View")]
        public static void SetPortraitGameView()
        {
            try
            {
                GameViewPortrait.Apply(1080, 1920);
                Debug.Log("[MobilePlaytest] Game view set to portrait 1080x1920.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MobilePlaytest] Portrait game view setup skipped: " + e.Message +
                                 "  (수동으로 Game 뷰 해상도를 세로로 선택하세요.)");
            }
        }
    }

    /// <summary>
    /// 내부 GameViewSizes 에 1080x1920 고정 해상도를 추가하고 선택한다(리플렉션).
    /// 버전에 따라 실패할 수 있어 호출부에서 try/catch 로 감싼다.
    /// </summary>
    internal static class GameViewPortrait
    {
        public static void Apply(int width, int height)
        {
            var asm = typeof(Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var groupEnum = asm.GetType("UnityEditor.GameViewSizeGroupType");
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var sizeTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");

            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singletonType.GetProperty("instance",
                BindingFlags.Public | BindingFlags.Static).GetValue(null);

            object groupType = CurrentGroupType(groupEnum);
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType });

            int index = FindIndex(group, width, height);
            if (index < 0)
            {
                // GameViewSize(GameViewSizeType type, int width, int height, string baseText)
                object fixedRes = Enum.Parse(sizeTypeEnum, "FixedResolution");
                var ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
                object newSize = ctor.Invoke(new[] { fixedRes, (object)width, height, "Portrait 1080x1920" });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                index = FindIndex(group, width, height);
            }

            // GameView.SizeSelectionCallback(int index, object size) via SetSizeIndex
            var gameViewType = asm.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            var setSize = gameViewType.GetMethod("SizeSelectionCallback",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object size = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { index });
            setSize.Invoke(window, new[] { (object)index, size });
        }

        private static object CurrentGroupType(Type groupEnum)
        {
            BuildTarget t = EditorUserBuildSettings.activeBuildTarget;
            string name =
                t == BuildTarget.Android ? "Android" :
                t == BuildTarget.iOS ? "iOS" :
                "Standalone";
            return Enum.Parse(groupEnum, name);
        }

        private static int FindIndex(object group, int width, int height)
        {
            int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = group.GetType().GetMethod("GetGameViewSize");
            for (int i = 0; i < total; i++)
            {
                object size = getSize.Invoke(group, new object[] { i });
                int w = (int)size.GetType().GetProperty("width").GetValue(size);
                int h = (int)size.GetType().GetProperty("height").GetValue(size);
                if (w == width && h == height) return i;
            }
            return -1;
        }
    }
}
#endif
