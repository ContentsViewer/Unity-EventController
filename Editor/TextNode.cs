
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


namespace EventControl.EventControllerEditor
{

    /// <summary>
    /// テキストを表示するノード
    /// </summary>
    public class TextNode : Node
    {
        string text;

        public TextNode(SerializableEventNode eventNode, NodeColor color) : base(new Rect(310, 10, 150, 50), color)
        {
            this.text = eventNode.name;
            this.eventNode = eventNode;
        }

        public TextNode(SerializableEventNode eventNode, NodeColor color, Rect rect) : base(rect, color)
        {
            this.text = eventNode.name;
            this.eventNode = eventNode;
        }

        protected override void OnGUI()
        {
            var style = EditorStyles.wordWrappedLabel;
            var defaultAlignment = style.alignment;
            style.alignment = TextAnchor.MiddleCenter;

            var rect = new Rect(JunctionSize.x, 0, Rect.width - JunctionSize.x * 2, Rect.height);

            this.text = eventNode.name;
            GUI.Label(rect, text, style);

            style.alignment = defaultAlignment;
        }
    }
}
