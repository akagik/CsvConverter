using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace CsvConverter
{
    public class CsvConverterWindow : EditorWindow
    {
        public static string SETTINGS_KEY = "CsvConverter/settings";

        public CsvConverterSettings settings;
        private bool isDownloading;
        private Vector2 scrollPosition;

        // 二重に保存しないようにするために導入
        private string savedGUID;

        // 検索ボックス用
        private static GUIStyle toolbarSearchField;
        private static GUIStyle toolbarSearchFieldCancelButton;
        private static GUIStyle toolbarSearchFieldCancelButtonEmpty;
        private string searchTxt = "";

        // チェックボックス用
        private bool isAll = true;
        CsvConverterSettings.Setting[] cachedAllSettings;

        [MenuItem("Window/CsvConverter", false, 0)]
        static public void OpenWindow()
        {
            EditorWindow.GetWindow<CsvConverterWindow>(false, "CsvConverter", true).Show();
        }

        void OnEnable()
        {
            string guid = EditorUserSettings.GetConfigValue(SETTINGS_KEY);
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (path != "")
            {
                // Debug.Log("Found prefs settings GUID: " + EditorPrefs.GetString(SETTINGS_KEY));
                settings = AssetDatabase.LoadAssetAtPath<CsvConverterSettings>(path);
            }
            else
            {
                // Debug.Log("Not Found GUID");
            }
        }

        void OnFocus()
        {
            // isAll用のデータをキャッシュ
            var allSettingList = new List<CsvConverterSettings.Setting>();
            string[] settingGUIDArray = AssetDatabase.FindAssets("t:CsvConverterSettings");
            for (int i = 0; i < settingGUIDArray.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(settingGUIDArray[i]);
                allSettingList.AddRange(AssetDatabase.LoadAssetAtPath<CsvConverterSettings>(assetPath).list);
            }

            cachedAllSettings = allSettingList.ToArray();
        }

        private void OnGUI()
        {
            GUILayout.Space(6f);
            isAll = EditorGUILayout.Toggle("AllSettings", isAll);
            CsvConverterSettings.Setting[] setting = null;
            if (isAll)
            {
                if (cachedAllSettings != null)
                {
                    setting = cachedAllSettings;
                }
            }
            else
            {
                settings =
                    EditorGUILayout.ObjectField("Settings", settings, typeof(CsvConverterSettings), false) as
                        CsvConverterSettings;
                setting = settings.list;
            }

            // 検索ボックスを表示
            GUILayout.BeginHorizontal();
            searchTxt = SearchField(searchTxt);
            searchTxt = searchTxt.ToLower();
            GUILayout.EndHorizontal();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            if (settings != null && setting != null)
            {
                // セットされている settings 情報を EditorUserSettings に保存する.
                {
                    string guid;
                    long localId;

                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(settings, out guid, out localId))
                    {
                        if (savedGUID != guid)
                        {
                            // Debug.Log("Save GUID(" + guid + ") at " + SETTINGS_KEY);
                            EditorPrefs.SetString(SETTINGS_KEY, guid);
                            EditorUserSettings.SetConfigValue(SETTINGS_KEY, guid);
                            savedGUID = guid;
                        }
                    }
                }

                for (int i = 0; i < setting.Length; i++)
                {
                    var s = setting[i];

                    // 設定が削除されている場合などに対応
                    if (s == null)
                    {
                        continue;
                    }

                    // 検索ワードチェック
                    if (!string.IsNullOrEmpty(searchTxt))
                    {
                        if (s.tableGenerate)
                        {
                            if (!searchTxt.IsSubsequence(s.tableAssetName.ToLower()))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!searchTxt.IsSubsequence(s.className.ToLower()))
                            {
                                continue;
                            }
                        }
                    }

                    GUILayout.BeginHorizontal("box");

                    if (s.tableGenerate)
                    {
                        GUILayout.Label(s.tableAssetName);
                    }
                    else
                    {
                        GUILayout.Label(s.className);
                    }

                    GUI.enabled = s.canGenerateCode;
                    if (GUILayout.Button("Generate Code", GUILayout.Width(110)) && !isDownloading)
                    {
                        isDownloading = true;
                        GenerateOneCode(s);
                        isDownloading = false;

                        GUIUtility.ExitGUI();
                    }

                    GUI.enabled = s.canCreateAsset;

                    if (GUILayout.Button("Create Assets", GUILayout.Width(110)) && !isDownloading)
                    {
                        isDownloading = true;
                        CreateOneAssets(s);
                        isDownloading = false;

                        GUIUtility.ExitGUI();
                    }

                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Generate All Codes", "LargeButtonMid") && !isDownloading)
                {
                    isDownloading = true;
                    GenerateAllCode(setting);
                    isDownloading = false;

                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Create All Assets", "LargeButtonMid") && !isDownloading)
                {
                    isDownloading = true;
                    CreateAllAssets(setting);
                    isDownloading = false;

                    GUIUtility.ExitGUI();
                }

                GUILayout.EndHorizontal();
            }
        }

        public static void GenerateAllCode(CsvConverterSettings.Setting[] setting)
        {
            int i = 0;

            try
            {
                foreach (CsvConverterSettings.Setting s in setting)
                {
                    show_progress(s.className, (float) i / setting.Length, i, setting.Length);
                    CsvConverter.GenerateCode(s);
                    i++;
                    show_progress(s.className, (float) i / setting.Length, i, setting.Length);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        public static void CreateAllAssets(CsvConverterSettings.Setting[] setting)
        {
            try
            {
                foreach (CsvConverterSettings.Setting s in setting)
                {
                    CsvConverter.CreateAssets(s);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        public static void GenerateOneCode(CsvConverterSettings.Setting s)
        {
            show_progress(s.className, 0, 0, 1);

            try
            {
                CsvConverter.GenerateCode(s);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            show_progress(s.className, 1, 1, 1);

            EditorUtility.ClearProgressBar();
        }

        public static void CreateOneAssets(CsvConverterSettings.Setting s)
        {
            try
            {
                CsvConverter.CreateAssets(s);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        private static void show_progress(string name, float progress, int i, int total)
        {
            EditorUtility.DisplayProgressBar("Progress", progress_msg(name, i, total), progress);
        }

        private static string progress_msg(string name, int i, int total)
        {
            return string.Format("Creating {0} ({1}/{2})", name, i, total);
        }

        private static string SearchField(string text)
        {
            Rect rect = GUILayoutUtility.GetRect(16f, 24f, 16f, 24f, new GUILayoutOption[]
            {
                GUILayout.Width(400f), // 検索ボックスのサイズ
            });
            rect.x += 4f;
            rect.y += 4f;

            return (string) ToolbarSearchField(rect, text);
        }

        private static string ToolbarSearchField(Rect position, string text)
        {
            Rect rect = position;
            rect.x += position.width;
            rect.width = 14f;

            if (toolbarSearchField == null)
            {
                toolbarSearchField = GetStyle("ToolbarSeachTextField");
            }

            text = EditorGUI.TextField(position, text, toolbarSearchField);
            if (text == "")
            {
                if (toolbarSearchFieldCancelButtonEmpty == null)
                {
                    toolbarSearchFieldCancelButtonEmpty = GetStyle("ToolbarSeachCancelButtonEmpty");
                }

                GUI.Button(rect, GUIContent.none, toolbarSearchFieldCancelButtonEmpty);
            }
            else
            {
                if (toolbarSearchFieldCancelButton == null)
                {
                    toolbarSearchFieldCancelButton = GetStyle("ToolbarSeachCancelButton");
                }

                if (GUI.Button(rect, GUIContent.none, toolbarSearchFieldCancelButton))
                {
                    text = "";
                    GUIUtility.keyboardControl = 0;
                }
            }

            return text;
        }

        private static GUIStyle GetStyle(string styleName)
        {
            GUIStyle gUIStyle = GUI.skin.FindStyle(styleName);
            if (gUIStyle == null)
            {
                gUIStyle = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            }

            if (gUIStyle == null)
            {
                Debug.LogError("Missing built-in guistyle " + styleName);
                gUIStyle = new GUIStyle();
            }

            return gUIStyle;
        }
    }
}