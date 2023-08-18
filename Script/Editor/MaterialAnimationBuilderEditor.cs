using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace PogapogaEditor.Component
{
    [CustomEditor(typeof(MaterialAnimationBuilder))]
    public class MaterialAnimationBuilderEditor : Editor
    {
        MaterialAnimationBuilder animationBuilder;
        private Vector2 _scrollPosition = Vector2.zero;

        private List<string> _propertyNames = new List<string>();

        private void OnEnable()
        {
            animationBuilder = (MaterialAnimationBuilder)target;

            GetShaderPropertyNames();
        }

        private void GetShaderPropertyNames()
        {
            _propertyNames.Clear();
            _propertyNames.Add("_");
            if (animationBuilder.shaderList.Count == 0 ) { return; }
            int propertycount = animationBuilder.shaderList[animationBuilder.shaderIndex].GetPropertyCount();
            for (int i = 0; i < propertycount; i++)
            {
                _propertyNames.Add(animationBuilder.shaderList[animationBuilder.shaderIndex].GetPropertyName(i));
            }
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            #region // Material情報の取得
            GameObject _rootObject;
            _rootObject = EditorGUILayout.ObjectField("RootObject", animationBuilder.rootObject, typeof(GameObject), true) as GameObject;
            if (_rootObject != animationBuilder.rootObject)
            {
                Undo.RecordObject(animationBuilder, "MaterialAnimationBuilder");
                animationBuilder.rootObject = _rootObject;
            }

            if (GUILayout.Button("RootObjectからMaterialを取得"))
            {
                if (animationBuilder.rootObject == null)
                {
                    EditorUtility.DisplayDialog("MaterialAnimationBuilder", "RootObjectを設定してください。", "OK");
                    return;
                }

                Undo.RecordObject(animationBuilder, "MaterialAnimationBuilder");
                animationBuilder.GetMaterialInfo();

                if (animationBuilder.materialsList.Count == 0) 
                {
                    EditorUtility.DisplayDialog("MaterialAnimationBuilder", "取得できるMaterialがありませんでした。\nRootObjectを確認してください。", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("MaterialAnimationBuilder", "Material情報を取得しました。", "OK");
                }
            }
            #endregion

            #region // AnimationClipの設定
            // AnimationClipの更新
            AnimationClip _animationClip;
            _animationClip = EditorGUILayout.ObjectField("設定先のAnimationClip", animationBuilder.animationClip, typeof(AnimationClip), false) as AnimationClip;
            if (_animationClip != animationBuilder.animationClip)
            {
                Undo.RecordObject(animationBuilder, "MaterialAnimationBuilder");
                animationBuilder.animationClip = _animationClip;
            }

            // ShaderIndexの更新
            int _shaderIndex;
            _shaderIndex = EditorGUILayout.Popup("Animationの対象のShader", animationBuilder.shaderIndex, animationBuilder.shaderNameList.ToArray());
            if (_shaderIndex != animationBuilder.shaderIndex)
            {
                Undo.RecordObject(animationBuilder, "MaterialAnimationBuilder");
                animationBuilder.shaderIndex = _shaderIndex;
                GetShaderPropertyNames();
            }

            // Animationの開始時間と終了時間の更新
            float _startTime;
            float _endTime;
            _startTime = EditorGUILayout.FloatField("Animation開始時間", animationBuilder.startTime);
            _endTime = EditorGUILayout.FloatField("Animation終了時間", animationBuilder.endTime);
            if (_startTime != animationBuilder.startTime || _endTime != animationBuilder.endTime)
            {
                Undo.RecordObject(animationBuilder, "MaterialAnimationBuilder");
                animationBuilder.startTime = _startTime;
                animationBuilder.endTime = _endTime;
            }

            #region // Propertyに関する処理
            bool _propertyNamesIsOpen;
            _propertyNamesIsOpen = EditorGUILayout.Foldout(animationBuilder.propertyNamesIsOpen, "設定するPropertyName");
            if (_propertyNamesIsOpen != animationBuilder.propertyNamesIsOpen)
            {
                Undo.RecordObject(animationBuilder, "MaterialAnimationBuilder");
                animationBuilder.propertyNamesIsOpen = _propertyNamesIsOpen;
            }
            if (animationBuilder.propertyNamesIsOpen)
            {
                int _propertyNum;
                _propertyNum = EditorGUILayout.IntField("設定するProperty数", animationBuilder.propertyNum);
                if (_propertyNum != animationBuilder.propertyNames.Length)
                {
                    Undo.RecordObject(animationBuilder, "MaterialAnimationBuilder");
                    animationBuilder.propertyNum = _propertyNum;
                    Array.Resize(ref animationBuilder.propertyNames, animationBuilder.propertyNum);
                    Array.Resize(ref animationBuilder.startValues, animationBuilder.propertyNum);
                    Array.Resize(ref animationBuilder.endValues, animationBuilder.propertyNum);
                    Array.Resize(ref animationBuilder.propertyIndices, animationBuilder.propertyNum);
                }

                for (int i = 0; i < animationBuilder.propertyNames.Length; i++)
                {
                    string _propertyName;
                    int _propertyIndex;
                    float _startValue;
                    float _endValue;

                    EditorGUI.indentLevel++;
                    _propertyIndex = EditorGUILayout.Popup("PropertyName選択", animationBuilder.propertyIndices[i], _propertyNames.ToArray());
                    if (_propertyIndex != animationBuilder.propertyIndices[i])
                    {
                        Undo.RecordObject(animationBuilder, "MaterialAnimationBuilder");
                        animationBuilder.propertyIndices[i] = _propertyIndex;
                        animationBuilder.propertyNames[i] = $"material.{_propertyNames[animationBuilder.propertyIndices[i]]}";
                    }

                    _propertyName = EditorGUILayout.TextField($"PropertyName{i}", animationBuilder.propertyNames[i]);
                    EditorGUI.indentLevel++;

                    _startValue = EditorGUILayout.FloatField("開始値", animationBuilder.startValues[i]);
                    _endValue = EditorGUILayout.FloatField("終了値", animationBuilder.endValues[i]);

                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;

                    // 変更があった場合のUndo登録と更新
                    if (_propertyIndex != animationBuilder.propertyIndices[i] ||
                        _propertyName != animationBuilder.propertyNames[i] ||
                        _startValue != animationBuilder.startValues[i] ||
                        _endValue != animationBuilder.endValues[i])
                    {
                        Undo.RecordObject(animationBuilder, "MaterialAnimationBuilder");
                        animationBuilder.propertyIndices[i] = _propertyIndex;
                        animationBuilder.propertyNames[i] = _propertyName;
                        animationBuilder.startValues[i] = _startValue;
                        animationBuilder.endValues[i] = _endValue;
                    }
                }
            }
            #endregion

            #region // AnimationClipへの書き込み
            if (GUILayout.Button("AnimationClipへの書き込み"))
            {
                #region // 設定情報の不足時
                if (animationBuilder.rootObject == null)
                {
                    EditorUtility.DisplayDialog("MaterialAnimationBuilder", "RootObjectが設定されていません。\nRootObjectを確認してください。", "OK");
                    return;
                }
                if (animationBuilder.animationClip == null)
                {
                    EditorUtility.DisplayDialog("MaterialAnimationBuilder", "AnimationClipが設定されていません。\nAnimationClipを確認してください。", "OK");
                    return;
                }
                if (animationBuilder.materialsList.Count == 0)
                {
                    EditorUtility.DisplayDialog("MaterialAnimationBuilder", "Material情報が取得されていません。\nRootObjectからMaterial情報を取得してください。", "OK");
                    return;
                }
                #endregion

                if (animationBuilder.animationClip.empty == false)
                {
                    bool emptyCheck = EditorUtility.DisplayDialog("MaterialAnimationBuilder",
                        "AnimationClipが空ではありません。\nAnimationClipを初期化して書き込みます。", "OK", "Cancel");
                    if (emptyCheck == false)
                    {
                        return;
                    }
                }

                Undo.RecordObject(animationBuilder.animationClip, "MaterialAnimationBuilder AnimationClipへの書き込み");
                animationBuilder.SetAnimationClipData();
                EditorUtility.DisplayDialog("MaterialAnimationBuilder", "AnimationClipへの書き込みが完了しました", "OK");
            }
            #endregion
            #endregion

            #region // Material情報の閲覧用
            bool _materialsListIsOpen;
            _materialsListIsOpen = EditorGUILayout.Foldout(animationBuilder.materialsListIsOpen, "選択したShaderのMaterial一覧");
            if (_materialsListIsOpen != animationBuilder.materialsListIsOpen)
            {
                Undo.RecordObject(animationBuilder, "MaterialAnimationBuilder");
                animationBuilder.materialsListIsOpen = _materialsListIsOpen;
            }
            if (animationBuilder.materialsListIsOpen)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                for (int i = 0; i < animationBuilder.materialsList.Count; i++)
                {
                    if (animationBuilder.materialsList[i].shader != animationBuilder.shaderList[animationBuilder.shaderIndex]) { continue; }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(animationBuilder.materialsList[i], typeof(Material), false);
                        EditorGUILayout.ObjectField(animationBuilder.materialsList[i].shader, typeof(Shader), false);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            #endregion

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(animationBuilder);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

