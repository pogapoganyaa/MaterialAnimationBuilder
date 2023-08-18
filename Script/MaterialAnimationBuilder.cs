#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace PogapogaEditor.Component
{
    /// <summary>
    /// Avatar内に含まれる複数のMaterialの
    /// Property値をまとめて変更するAnimationClipの作成を補助するツールです。
    /// </summary>
    public class MaterialAnimationBuilder : MonoBehaviour
    {
        public GameObject rootObject;
        private SkinnedMeshRenderer[] skinnedMeshRenderers;
        private MeshRenderer[] meshRenderers;
        public List<Material> materialsList = new List<Material>();
        public List<Shader> shaderList = new List<Shader>();
        public List<string> shaderNameList = new List<string>();
        public int shaderIndex = 0;

        public int propertyNum = 1;
        public string[] propertyNames = { "material._" };
        public int[] propertyIndices = { 0 };

        public AnimationClip animationClip;
        public float startTime = 0f;
        public float endTime = 1f;
        public float[] startValues = { 0f };
        public float[] endValues = { 1f };

        // Inspector用
        public bool materialsListIsOpen = false;
        public bool propertyNamesIsOpen = false;

        public void GetMaterialInfo()
        {
            materialsList.Clear();
            shaderList.Clear();
            shaderNameList.Clear();

            GetMaterialInfo<SkinnedMeshRenderer>(skinnedMeshRenderers);
            GetMaterialInfo<MeshRenderer>(meshRenderers);

            skinnedMeshRenderers = rootObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            meshRenderers = rootObject.GetComponentsInChildren<MeshRenderer>();

            Debug.Log("処理完了");
        }
        public void GetMaterialInfo<TMeshRendere>(TMeshRendere[] tMeshRenderes) where TMeshRendere : Renderer
        {
            tMeshRenderes = rootObject.GetComponentsInChildren<TMeshRendere>();

            foreach (TMeshRendere tMeshRendere in tMeshRenderes)
            {
                if (tMeshRendere.gameObject.tag == "EditorOnly") { continue; }

                foreach (Material material in tMeshRendere.sharedMaterials)
                {
                    materialsList.Add(material);

                    if (shaderList.Contains(material.shader) == false)
                    {
                        shaderList.Add(material.shader);
                        shaderNameList.Add(material.shader.name);
                    }
                }
            }
        }

        public void SetAnimationClipData()
        {
            animationClip.ClearCurves();
            SetAnimationClipData<SkinnedMeshRenderer>(skinnedMeshRenderers);
            SetAnimationClipData<MeshRenderer>(meshRenderers);
            Debug.Log("AnimationClip書き込み完了");
        }

        public void SetAnimationClipData<TMeshRendere>(TMeshRendere[] tMeshRenderes) where TMeshRendere : Renderer
        {
            foreach (TMeshRendere tMeshRendere in tMeshRenderes)
            {
                if (tMeshRendere.gameObject.tag == "EditorOnly") { continue; }

                bool shaderFlag = false;
                foreach (Material mat in tMeshRendere.sharedMaterials)
                {
                    if (mat.shader == shaderList[shaderIndex])
                    {
                        shaderFlag = true;
                    }
                }
                if (shaderFlag == false) { continue; }

                string hierarchyPath = GetHierarchyPath(tMeshRendere.gameObject, rootObject);

                for (int i = 0; i < propertyNames.Length; i++)
                {
                    AnimationCurve curve = new AnimationCurve();
                    curve.AddKey(startTime, startValues[i]);
                    curve.AddKey(endTime, endValues[i]);
                    animationClip.SetCurve(hierarchyPath, typeof(TMeshRendere), propertyNames[i], curve);
                }
            }
        }

        public string GetHierarchyPath(GameObject targetObject, GameObject rootObject)
        {
            string resultPath = targetObject.name;
            Transform _parent = targetObject.transform.parent;

            if (targetObject == rootObject)
            {
                return "";
            }

            // parentのObjectがなくなるまでループする
            while (_parent != null)
            {
                // _rootObjectまでの到達
                if (_parent == rootObject.transform)
                {
                    break;
                }

                // pathとparentの更新
                resultPath = $"{_parent.name}/{resultPath}";
                _parent = _parent.parent;
            }
            return resultPath;
        }
    }
}
#endif