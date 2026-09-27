using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace ChuJian.Merge
{
    /// <summary>
    /// 修复缺失材质/图集子资产的 TMP 动态字体。
    /// 这些字体只有孤立的 TMP_FontAsset 脚本块，没有内嵌的材质(!u!21)与图集纹理(!u!28)，
    /// TMP 3.0.9 访问时会抛 "The variable material of TMP_FontAsset has not been assigned"。
    /// 做法：用旁边的源字体(.ttf/.otf)重建材质+图集，作为子资产写回原 .asset
    /// （主资产 GUID 不变，场景引用全部保持有效）。
    /// 用法：菜单「初见新生 → 修复TMP字体材质」，执行一次即可。
    /// </summary>
    public static class FixTmpFonts
    {
        [MenuItem("初见新生/修复TMP字体材质")]
        public static void Fix()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("提示", "请先停止 Play 再执行修复。", "好");
                return;
            }

            // 全工程源字体清单（用于无同名邻接字体时的兜底）
            string[] allFontFiles = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories)
                .Where(p => { string e = Path.GetExtension(p).ToLowerInvariant(); return e == ".ttf" || e == ".otf" || e == ".ttc"; })
                .Select(p => p.Replace('\\', '/'))
                .ToArray();

            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" });
            var report = new List<string>();
            int fixedCount = 0, okCount = 0, failCount = 0;

            foreach (string g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fa == null) continue;

                // 用序列化属性判断材质是否缺失（直接访问 fa.material 会抛异常）
                var so = new SerializedObject(fa);
                var matProp = so.FindProperty("material") ?? so.FindProperty("m_Material");
                if (matProp != null && matProp.objectReferenceValue != null) { okCount++; continue; }

                string baseName = Path.GetFileNameWithoutExtension(path);
                string dir = Path.GetDirectoryName(path).Replace('\\', '/');

                try
                {
                    // 选源字体：同目录名字匹配 → 全工程名字匹配 → 同目录任意 → 全工程任意中文黑体
                    string[] dirFonts = allFontFiles.Where(f => Path.GetDirectoryName(f).Replace('\\', '/') == dir).ToArray();
                    string srcPath =
                        dirFonts.FirstOrDefault(f => NameMatch(baseName, f))
                        ?? allFontFiles.FirstOrDefault(f => NameMatch(baseName, f))
                        ?? dirFonts.FirstOrDefault()
                        ?? allFontFiles.FirstOrDefault(f => f.ToLowerInvariant().Contains("simhei"))
                        ?? allFontFiles.FirstOrDefault();

                    if (srcPath == null)
                    {
                        report.Add("[跳过] 工程内没有可用源字体: " + baseName);
                        failCount++;
                        continue;
                    }

                    var srcFont = AssetDatabase.LoadAssetAtPath<Font>(srcPath);
                    if (srcFont == null)
                    {
                        report.Add("[跳过] 源字体无法加载: " + srcPath);
                        failCount++;
                        continue;
                    }

                    var fresh = TMP_FontAsset.CreateFontAsset(srcFont);
                    if (fresh == null || fresh.material == null)
                    {
                        report.Add("[跳过] 重建失败: " + baseName);
                        failCount++;
                        continue;
                    }

                    var mat = fresh.material;
                    var tex = fresh.atlasTexture;
                    mat.name = baseName + " Material";
                    if (tex != null) tex.name = baseName + " Atlas";
                    AssetDatabase.AddObjectToAsset(mat, path);
                    if (tex != null) AssetDatabase.AddObjectToAsset(tex, path);

                    matProp = so.FindProperty("material") ?? so.FindProperty("m_Material");
                    if (matProp != null) matProp.objectReferenceValue = mat;
                    var atlasProp = so.FindProperty("m_AtlasTextures");
                    if (atlasProp != null && atlasProp.arraySize == 0 && tex != null)
                    {
                        atlasProp.arraySize = 1;
                        atlasProp.GetArrayElementAtIndex(0).objectReferenceValue = tex;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(fa);

                    fixedCount++;
                    report.Add("[已修复] " + baseName + "  ←  " + Path.GetFileName(srcPath));
                }
                catch (Exception ex)
                {
                    report.Add("[异常] " + baseName + ": " + ex.Message);
                    failCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = "修复成功 " + fixedCount + " 个，原有正常 " + okCount + " 个，失败 " + failCount + " 个。\n\n"
                + string.Join("\n", report);
            Debug.Log("[初见新生] TMP字体修复报告：\n" + msg);
            EditorUtility.DisplayDialog("修复完成", msg, "好");
        }

        static bool NameMatch(string fontAssetName, string fontFilePath)
        {
            string file = Path.GetFileNameWithoutExtension(fontFilePath).ToLowerInvariant();
            return fontAssetName.ToLowerInvariant().Contains(file);
        }
    }
}
