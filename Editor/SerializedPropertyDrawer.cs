
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;



namespace EventControl.EventControllerEditor
{

    public class SerializedPropertyDrawer
    {
        const int defaultVisibleArrayElements = 20;
        int maxVisibleArrayElements = defaultVisibleArrayElements;
        bool someArrayVisible;


        public void DrawSerializedProperty(SerializedProperty property)
        {
            //配列が表示されていないとき表示数をリセット
            if (!someArrayVisible)
            {
                maxVisibleArrayElements = defaultVisibleArrayElements;
            }
            else
            {
                someArrayVisible = true;
            }

            switch (property.propertyType)
            {
                //List, Arrayなど
                case SerializedPropertyType.Generic:
                    property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, property.name);
                    if (!property.isExpanded)
                    {
                        break;
                    }

                    //インデント
                    EditorGUI.indentLevel++;

                    //配列ではないとき
                    if (!property.isArray)
                    {
                        //要素数が変わるものはコピーする
                        var child = property.Copy();
                        var end = property.GetEndProperty(true);

                        if (child.Next(true))
                        {
                            while (!SerializedProperty.EqualContents(child, end))
                            {
                                DrawSerializedProperty(child);
                                if (!child.Next(false))
                                {
                                    break;
                                }
                            }
                        }
                    }

                    //配列の場合は用意されているAPIを使用する
                    else
                    {
                        property.arraySize = EditorGUILayout.IntField("Length", property.arraySize);
                        var showCount = Mathf.Min(property.arraySize, maxVisibleArrayElements);
                        for (int i = 0; i < showCount; i++)
                        {
                            DrawSerializedProperty(property.GetArrayElementAtIndex(i));
                        }

                        //重くなるのですべて表示しない
                        if (property.arraySize > showCount)
                        {
                            GUILayout.BeginHorizontal();

                            //インデント
                            for (int i = 0; i < EditorGUI.indentLevel; i++)
                            {
                                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                            }

                            if (GUILayout.Button("Show more..."))
                            {
                                maxVisibleArrayElements += defaultVisibleArrayElements;
                            }
                            GUILayout.EndHorizontal();
                            someArrayVisible = true;
                        }
                    }

                    //インデントを戻す
                    EditorGUI.indentLevel--;
                    break;

                case SerializedPropertyType.Integer:
                    property.intValue = EditorGUILayout.IntField(property.name, property.intValue);
                    break;

                case SerializedPropertyType.Boolean:
                    property.boolValue = EditorGUILayout.Toggle(property.name, property.boolValue);
                    break;

                case SerializedPropertyType.Float:
                    property.floatValue = EditorGUILayout.FloatField(property.name, property.floatValue);
                    break;

                case SerializedPropertyType.String:
                    property.stringValue = EditorGUILayout.TextField(property.name, property.stringValue);
                    break;

                case SerializedPropertyType.Color:
                    property.colorValue = EditorGUILayout.ColorField(property.name, property.colorValue);
                    break;

                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = EditorGUILayout.ObjectField(
                        property.name, property.objectReferenceValue, typeof(Object), true);
                    break;

                case SerializedPropertyType.LayerMask:
                    property.intValue = EditorGUILayout.LayerField(property.name, property.intValue);
                    break;

                case SerializedPropertyType.Enum:
                    property.enumValueIndex = EditorGUILayout.Popup(property.name, property.enumValueIndex, property.enumNames);
                    break;

                case SerializedPropertyType.Vector2:
                    property.vector2Value = EditorGUILayout.Vector2Field(property.name, property.vector2Value);
                    break;

                case SerializedPropertyType.Vector3:
                    property.vector3Value = EditorGUILayout.Vector3Field(property.name, property.vector3Value);
                    break;

                case SerializedPropertyType.Vector4:
                    property.vector4Value = EditorGUILayout.Vector4Field(property.name, property.vector4Value);
                    break;

                case SerializedPropertyType.Rect:
                    property.rectValue = EditorGUILayout.RectField(property.name, property.rectValue);
                    break;

                case SerializedPropertyType.Character:
                    EditorGUILayout.PropertyField(property);
                    break;

                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = EditorGUILayout.CurveField(property.name, property.animationCurveValue);
                    break;

                case SerializedPropertyType.Bounds:
                    property.boundsValue = EditorGUILayout.BoundsField(property.name, property.boundsValue);
                    break;

                case SerializedPropertyType.Gradient:
                    EditorGUILayout.PropertyField(property);
                    break;

                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.Euler(
                        EditorGUILayout.Vector3Field(property.name, property.quaternionValue.eulerAngles));
                    break;
            }
        }
    }

}