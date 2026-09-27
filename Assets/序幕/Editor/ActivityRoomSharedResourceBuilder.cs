using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChanFangVR.EditorTools
{
    /// <summary>
    /// Converts the three ActivityRoom GLB visual variants into Unity assets.
    /// The source GLBs are read only; this tool creates independent Mesh assets,
    /// one shared Material/Texture set, and three visual-only Prefabs.
    /// </summary>
    public static class ActivityRoomSharedResourceBuilder
    {
        private const string SourceRoot = "Assets/序幕/Resources/Models/";
        private const string OutputRoot = SourceRoot + "ActivityRoom";
        private const string MeshRoot = OutputRoot + "/Mesh";
        private const string MaterialRoot = OutputRoot + "/Materials";
        private const string TextureRoot = OutputRoot + "/Textures";
        private const string PrefabRoot = OutputRoot + "/Prefabs";

        private static readonly string[] SourcePaths =
        {
            SourceRoot + "ActivityRoom_LOD0.glb",
            SourceRoot + "ActivityRoom_LOD1.glb",
            SourceRoot + "ActivityRoom_LOD2.glb"
        };

        private static readonly string[] MeshNames =
        {
            "ActivityRoom_LOD0_Mesh",
            "ActivityRoom_LOD1_Mesh",
            "ActivityRoom_LOD2_Mesh"
        };

        private static readonly string[] PrefabNames =
        {
            "ActivityRoom_LOD0",
            "ActivityRoom_LOD1",
            "ActivityRoom_LOD2"
        };

        [MenuItem("Tools/ActivityRoom/Build Shared LOD Resources")]
        public static void Build()
        {
            try
            {
                BuildInternal();
                Debug.Log("[ActivityRoom] Shared LOD resources generated successfully.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Tools/ActivityRoom/Check Source LOD References")]
        public static void Check()
        {
            try
            {
                CheckInternal();
                Debug.Log("[ActivityRoom CHECK PASS] All source LOD Mesh/Material/Texture references are valid.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildInternal()
        {
            ValidateSources();
            EnsureFolder(OutputRoot);
            EnsureFolder(MeshRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(TextureRoot);
            EnsureFolder(PrefabRoot);

            var sourcePrefabs = new GameObject[SourcePaths.Length];
            var sourceMeshes = new Mesh[SourcePaths.Length];
            for (int i = 0; i < SourcePaths.Length; i++)
            {
                sourcePrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePaths[i]);
                if (sourcePrefabs[i] == null)
                    throw new InvalidOperationException("Unable to load GLB prefab: " + SourcePaths[i]);

                sourceMeshes[i] = FindVisualMesh(sourcePrefabs[i]);
                if (sourceMeshes[i] == null)
                    throw new InvalidOperationException("No visual Mesh found in: " + SourcePaths[i]);
            }

            var sourceRenderer = FindSingleVisualRenderer(sourcePrefabs[0]);
            var sourceMaterial = sourceRenderer.sharedMaterial;
            if (sourceMaterial == null)
                throw new InvalidOperationException("ActivityRoom LOD0 has no material.");

            var textureSources = ResolveTextureRoles(sourceMaterial);
            var sharedTextures = new Dictionary<int, Texture2D>();
            var baseColor = CloneTexture(textureSources.BaseColor, TextureRoot + "/BaseColor.asset", "ActivityRoom_BaseColor");
            var normal = CloneTexture(textureSources.Normal, TextureRoot + "/Normal.asset", "ActivityRoom_Normal");
            var metallic = CloneTexture(textureSources.Metallic, TextureRoot + "/Metallic.asset", "ActivityRoom_Metallic");
            sharedTextures[textureSources.BaseColor.GetInstanceID()] = baseColor;
            sharedTextures[textureSources.Normal.GetInstanceID()] = normal;
            sharedTextures[textureSources.Metallic.GetInstanceID()] = metallic;

            var sharedMaterial = UnityEngine.Object.Instantiate(sourceMaterial);
            sharedMaterial.name = "ActivityRoom_Shared_Material";
            RemapMaterialTextures(sourceMaterial, sharedMaterial, sharedTextures);
            AssetDatabase.CreateAsset(sharedMaterial, MaterialRoot + "/ActivityRoom_Shared_Material.mat");

            var meshAssets = new Mesh[SourcePaths.Length];
            for (int i = 0; i < sourceMeshes.Length; i++)
            {
                meshAssets[i] = UnityEngine.Object.Instantiate(sourceMeshes[i]);
                meshAssets[i].name = MeshNames[i];
                AssetDatabase.CreateAsset(meshAssets[i], MeshRoot + "/" + MeshNames[i] + ".asset");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            for (int i = 0; i < sourcePrefabs.Length; i++)
                CreateVisualPrefab(sourcePrefabs[i], meshAssets[i], sharedMaterial, PrefabRoot + "/" + PrefabNames[i] + ".prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log(string.Format(
                "[ActivityRoom] Meshes={0}, shared material={1}, shared textures=3, prefabs=3",
                meshAssets.Length, AssetDatabase.GetAssetPath(sharedMaterial)));
        }

        private static void CheckInternal()
        {
            if (AssetDatabase.IsValidFolder(OutputRoot))
                throw new InvalidOperationException(
                    "Shared-resource output already exists; source-only check will not continue: " + OutputRoot);

            var sourcePrefabs = new GameObject[SourcePaths.Length];
            var rotations = new Quaternion[SourcePaths.Length];
            var scales = new Vector3[SourcePaths.Length];
            var positions = new Vector3[SourcePaths.Length];

            for (int i = 0; i < SourcePaths.Length; i++)
            {
                AssetDatabase.ImportAsset(SourcePaths[i], ImportAssetOptions.ForceSynchronousImport);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePaths[i]);
                if (prefab == null)
                    throw new InvalidOperationException("GLB main GameObject reference is null: " + SourcePaths[i]);

                var renderer = FindSingleVisualRenderer(prefab);
                var mesh = FindVisualMesh(prefab);
                if (mesh == null || mesh.vertexCount <= 0 || mesh.triangles.Length <= 0)
                    throw new InvalidOperationException("Mesh data is empty: " + SourcePaths[i]);
                if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
                    throw new InvalidOperationException("UV data does not match vertex count: " + SourcePaths[i]);
                if (mesh.normals == null || mesh.normals.Length != mesh.vertexCount)
                    throw new InvalidOperationException("Normal data does not match vertex count: " + SourcePaths[i]);

                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length != 1 || materials[0] == null)
                    throw new InvalidOperationException("Expected one valid Material reference: " + SourcePaths[i]);
                if (materials[0].shader == null)
                    throw new InvalidOperationException("Material shader reference is null: " + SourcePaths[i]);

                var textures = CollectMaterialTextures(materials[0]);
                if (textures.Count != 3)
                    throw new InvalidOperationException(string.Format(
                        "Expected three unique Texture references in {0}, found {1}.", SourcePaths[i], textures.Count));

                sourcePrefabs[i] = prefab;
                rotations[i] = prefab.transform.localRotation;
                scales[i] = prefab.transform.localScale;
                positions[i] = prefab.transform.localPosition;

                Debug.Log(string.Format(
                    "[ActivityRoom CHECK] {0}: Renderer=1, Mesh vertices={1}, triangles={2}, UV={3}, Normals={4}, Material=1, Textures={5}, Shader={6}, Rotation={7}, Scale={8}, Position={9}",
                    SourcePaths[i], mesh.vertexCount, mesh.triangles.Length / 3, mesh.uv.Length,
                    mesh.normals.Length, textures.Count, materials[0].shader.name,
                    prefab.transform.localRotation.eulerAngles, prefab.transform.localScale,
                    prefab.transform.localPosition));
            }

            for (int i = 1; i < sourcePrefabs.Length; i++)
            {
                if (Quaternion.Angle(rotations[0], rotations[i]) > 0.01f)
                    throw new InvalidOperationException("LOD root rotations do not match.");
                if (Vector3.Distance(scales[0], scales[i]) > 0.0001f)
                    throw new InvalidOperationException("LOD root scales do not match.");
                if (Vector3.Distance(positions[0], positions[i]) > 0.0001f)
                    throw new InvalidOperationException("LOD root positions do not match.");
            }
        }

        private static void ValidateSources()
        {
            if (AssetDatabase.IsValidFolder(OutputRoot))
                throw new InvalidOperationException(
                    "Output folder already exists. Refusing to overwrite generated assets: " + OutputRoot);

            for (int i = 0; i < SourcePaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SourcePaths[i]) == null)
                    throw new FileNotFoundException("Required source GLB is missing or not imported.", SourcePaths[i]);
            }
        }

        private static Mesh FindVisualMesh(GameObject root)
        {
            var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            if (meshFilters.Length > 0 && meshFilters[0].sharedMesh != null)
                return meshFilters[0].sharedMesh;

            var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinned.Length > 0 && skinned[0].sharedMesh != null)
                return skinned[0].sharedMesh;

            return null;
        }

        private static Renderer FindSingleVisualRenderer(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 1)
                throw new InvalidOperationException(string.Format(
                    "Expected one visual Renderer in {0}, found {1}.", root.name, renderers.Length));
            return renderers[0];
        }

        private static TextureRoles ResolveTextureRoles(Material material)
        {
            var unique = new List<Texture2D>();
            Texture2D baseColor = null;
            Texture2D normal = null;
            Texture2D metallic = null;

            if (material.shader == null)
                throw new InvalidOperationException("ActivityRoom material has no shader.");

            int propertyCount = ShaderUtil.GetPropertyCount(material.shader);
            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(material.shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propertyName = ShaderUtil.GetPropertyName(material.shader, i);
                var texture = material.GetTexture(propertyName) as Texture2D;
                if (texture == null)
                    continue;

                if (!unique.Contains(texture)) unique.Add(texture);
                string lower = propertyName.ToLowerInvariant();
                if (normal == null && (lower.Contains("normal") || lower.Contains("bump")))
                    normal = texture;
                else if (metallic == null && (lower.Contains("metal") || lower.Contains("rough") || lower.Contains("occlusion")))
                    metallic = texture;
                else if (baseColor == null && (lower.Contains("base") || lower.Contains("albedo") || lower.Contains("main")))
                    baseColor = texture;
            }

            if (unique.Count != 3)
                throw new InvalidOperationException("Expected exactly three unique ActivityRoom textures, found " + unique.Count);

            // UnityGLTF's imported material exposes the same three maps in the
            // current project. The fallback preserves the source GLB map order
            // if a shader uses non-standard property names.
            if (normal == null) normal = unique[0];
            if (baseColor == null) baseColor = unique.Count > 1 ? unique[1] : unique[0];
            if (metallic == null) metallic = unique.Count > 2 ? unique[2] : unique[unique.Count - 1];

            if (baseColor == normal || baseColor == metallic || normal == metallic)
                throw new InvalidOperationException("ActivityRoom texture roles are not three distinct textures.");

            return new TextureRoles { BaseColor = baseColor, Normal = normal, Metallic = metallic };
        }

        private static Texture2D CloneTexture(Texture2D source, string path, string name)
        {
            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = name;
            AssetDatabase.CreateAsset(clone, path);
            return clone;
        }

        private static void RemapMaterialTextures(
            Material source,
            Material destination,
            Dictionary<int, Texture2D> textureMap)
        {
            int propertyCount = ShaderUtil.GetPropertyCount(source.shader);
            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(source.shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propertyName = ShaderUtil.GetPropertyName(source.shader, i);
                var texture = source.GetTexture(propertyName) as Texture2D;
                if (texture == null) continue;

                Texture2D replacement;
                if (textureMap.TryGetValue(texture.GetInstanceID(), out replacement))
                    destination.SetTexture(propertyName, replacement);
            }
        }

        private static List<Texture2D> CollectMaterialTextures(Material material)
        {
            var textures = new List<Texture2D>();
            if (material == null || material.shader == null) return textures;

            int propertyCount = ShaderUtil.GetPropertyCount(material.shader);
            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(material.shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propertyName = ShaderUtil.GetPropertyName(material.shader, i);
                var texture = material.GetTexture(propertyName) as Texture2D;
                if (texture != null && !textures.Contains(texture)) textures.Add(texture);
            }
            return textures;
        }

        private static void CreateVisualPrefab(
            GameObject sourcePrefab,
            Mesh mesh,
            Material sharedMaterial,
            string prefabPath)
        {
            var instance = UnityEngine.Object.Instantiate(sourcePrefab);
            try
            {
                instance.name = Path.GetFileNameWithoutExtension(prefabPath);

                var meshFilters = instance.GetComponentsInChildren<MeshFilter>(true);
                if (meshFilters.Length == 1)
                {
                    meshFilters[0].sharedMesh = mesh;
                }
                else
                {
                    var skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    if (skinned.Length != 1)
                        throw new InvalidOperationException("Expected one MeshFilter or SkinnedMeshRenderer in " + sourcePrefab.name);
                    skinned[0].sharedMesh = mesh;
                }

                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length != 1)
                    throw new InvalidOperationException("Expected one Renderer in " + sourcePrefab.name);

                var materials = renderers[0].sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = sharedMaterial;
                renderers[0].sharedMaterials = materials;

                bool success;
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out success);
                if (!success)
                    throw new InvalidOperationException("Failed to save prefab: " + prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private struct TextureRoles
        {
            public Texture2D BaseColor;
            public Texture2D Normal;
            public Texture2D Metallic;
        }
    }
}
