using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

namespace EventControl.EventControllerEditor
{

    public class MessageWindow
    {

        Rect rect;

        Vector2 minSize;
        public int fontSize;
        public int Id { get; private set; }
        public string message;
        public Vector2 Position
        {
            get { return new Vector2(rect.x, rect.y); }
            set { rect.x = value.x; rect.y = value.y; }
        }
        public Vector2 Size
        {
            get { return new Vector2(rect.width, rect.height); }
            set
            {
                if (minSize.x > value.x)
                {
                    return;
                }
                if (minSize.y > value.y)
                {
                    return;
                }

                rect.width = value.x; rect.height = value.y;
            }
        }
        //public delegate void OnGUI();
        //public OnGUI onGUI;
        //public event OnGUI OnGUIEvent;
        public bool draggable;

        public MessageWindow(Vector2 minSize, bool draggable, int fontSize)
        {
            this.fontSize = fontSize;
            this.minSize = minSize;
            Size = minSize;
            this.draggable = draggable;
            Id = WindowIdManager.Create();
        }

        public void Update()
        {

            rect = GUI.Window(Id, rect, WindowCallback, string.Empty);
        }

        void WindowCallback(int id)
        {
            //if(OnGUIEvent != null)
            //{
            //    OnGUIEvent();
            //}

            //if (onGUI != null)
            //{
            //    onGUI();
            //}

            //Debug.Log(12);
            //GUI.enabled = true;

            var style = EditorStyles.wordWrappedLabel;
            var defaultAlignment = style.alignment;
            var defaultFontSize = style.fontSize;

            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = fontSize;

            GUIContent content = new GUIContent(message);
            var size = style.CalcSize(content);

            Size = size;

            var textArea = new Rect(0.0f, 0.0f, size.x, size.y);


            //Debug.Log(size);
            GUI.Label(textArea, message, style);

            style.alignment = defaultAlignment;
            style.fontSize = defaultFontSize;

            if (draggable)
            {

                GUI.DragWindow();
            }

        }
    }
}