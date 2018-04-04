using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


namespace EventControl.EventControllerEditor
{


    /// <summary>
    /// ノード間の接続を表すクラス
    /// </summary>
    public class Connector
    {
        readonly Color defaultColor = Color.gray;
        readonly Color selectedColor = Color.blue;

        Color color = Color.gray;

        public Node StartNode { get; private set; }
        public JunctionPosition StartPosition { get; private set; }

        public Node EndNode { get; private set; }
        public JunctionPosition EndPosition { get; private set; }

        public List<Condition> conditions = new List<Condition>();

        public Connector(Node node, JunctionPosition position)
        {
            StartNode = node;
            StartPosition = position;
        }

        public void Connect(Node node, JunctionPosition position)
        {
            EndNode = node;
            EndPosition = position;
        }

        /// <summary>
        /// 接続を曲線として描画
        /// </summary>
        public void Draw()
        {
            if (EndNode == null)
            {
                throw new UnityException("No end node.");
            }

            var start = StartNode.CalculateConnectorPoint(StartPosition);
            var startV3 = new Vector3(start.x, start.y, 0f);

            var end = EndNode.CalculateConnectorPoint(EndPosition);
            var endV3 = new Vector3(end.x, end.y, 0f);

            var distanceX = Mathf.Abs(startV3.x - endV3.x);
            var startTan = new Vector3(StartPosition.side == JunctionSide.Left ? start.x - distanceX / 2.0f : start.x + distanceX / 2.0f, start.y, 0f);
            var endTan = new Vector3(EndPosition.side == JunctionSide.Left ? end.x - distanceX / 2.0f : end.x + distanceX / 2.0f, end.y, 0f);

            if (ConnectorManager.selected == this)
            {
                color = selectedColor;
            }
            else
            {
                color = defaultColor;
            }
            Handles.DrawBezier(startV3, endV3, startTan, endTan, color, null, 4f);
        }

        /// <summary>
        /// 始点となるノードと, 指定の座標を結ぶ直線の描画
        /// 終点の決定中に始点とマウス間の直線を描画する際に使う
        /// </summary>
        /// <param name="end">描画する直線の終点</param>
        public void DrawTo(Vector2 to)
        {
            var start = StartNode.CalculateConnectorPoint(StartPosition);
            Handles.DrawLine(new Vector3(start.x, start.y, 0f), new Vector3(to.x, to.y, 0f));
        }
    }



}