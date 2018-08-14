using System;
using UnityEngine;
using UnityEditor;

namespace CsvConverter {

    public class CsvConverterWindow : EditorWindow {
        public CsvConverterSettings settings;
        private bool isDownloading;
        private Vector2 scrollPosition;

        [MenuItem("Window/CsvConverter", false, 0)]
        static public void OpenWindow() {
            EditorWindow.GetWindow<CsvConverterWindow>(false, "CsvConverter", true).Show();
        }

        void OnEnable() {
            var settingsList = CsvConverter.GetSettings();

            if (settingsList.Length > 0) {
                settings = settingsList[0];
            }
        }

        private void OnGUI() {
            GUILayout.Space(6f);
            settings = EditorGUILayout.ObjectField("Settings", settings, typeof(CsvConverterSettings), false) as CsvConverterSettings;

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < settings.list.Length; i++) {
                var s = settings.list[i];

                GUILayout.BeginHorizontal("box");
                GUILayout.Label(s.className);

                GUI.enabled = s.canGenerateCode;
                if (GUILayout.Button("Generate Code", GUILayout.Width(110)) && !isDownloading) {
                    isDownloading = true;
                    GenerateOneCode(s);
                    isDownloading = false;

                    GUIUtility.ExitGUI();
                }

                GUI.enabled = s.canCreateAsset;

                if (GUILayout.Button("Create Assets", GUILayout.Width(110)) && !isDownloading) {
                    isDownloading = true;
                    CreateOneAssets(s);
                    isDownloading = false;

                    GUIUtility.ExitGUI();
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Generate All Codes", "LargeButtonMid") && !isDownloading) {
                isDownloading = true;
                GenerateAllCode(settings);
                isDownloading = false;

                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("Create All Assets", "LargeButtonMid") && !isDownloading) {
                isDownloading = true;
                CreateAllAssets(settings);
                isDownloading = false;

                GUIUtility.ExitGUI();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

        }

        public static void GenerateAllCode(CsvConverterSettings setting) {
            int i = 0;

            try {
                foreach (CsvConverterSettings.Setting s in setting.list) {
                    show_progress(s.className, (float)i / setting.list.Length, i, setting.list.Length);
                    CsvConverter.GenerateCode(s);
                    i++;
                    show_progress(s.className, (float)i / setting.list.Length, i, setting.list.Length);
                }
            }
            catch (Exception e) {
                Debug.LogException(e);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        public static void CreateAllAssets(CsvConverterSettings setting) {
            try {
                foreach (CsvConverterSettings.Setting s in setting.list) {
                    CsvConverter.CreateAssets(s);
                }
            }
            catch (Exception e) {
                Debug.LogException(e);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        public static void GenerateOneCode(CsvConverterSettings.Setting s) {
            show_progress(s.className, 0, 0, 1);

            try {
                CsvConverter.GenerateCode(s);
            }
            catch (Exception e) {
                Debug.LogException(e);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            show_progress(s.className, 1, 1, 1);

            EditorUtility.ClearProgressBar();
        }

        public static void CreateOneAssets(CsvConverterSettings.Setting s) {
            try {
                CsvConverter.CreateAssets(s);
            }
            catch (Exception e) {
                Debug.LogException(e);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        private static void show_progress(string name, float progress, int i, int total) {
            EditorUtility.DisplayProgressBar("Progress", progress_msg(name, i, total), progress);
        }

        private static string progress_msg(string name, int i, int total) {
            return string.Format("Creating {0} ({1}/{2})", name, i, total);
        }
    }
}