using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


namespace EventControl.EventControllerEditor
{

    //コネクターを管理します
    //ノード同士の接続,解除,
    public static class ConnectorManager
    {
        //===パラメータ=====================================================
        static List<Connector> connectors;
        static Dictionary<int, Dictionary<JunctionPosition, Connector>> connected;
        static Connector current;
        public static Connector selected;
        public static Connector Current { get { return current; } }
        public static bool HasCurrent { get { return current != null; } }

        //===コード==============================================================

        public static void Init()
        {
            connectors = new List<Connector>();
            connected = new Dictionary<int, Dictionary<JunctionPosition, Connector>>();
            current = null;
            selected = null;
        }

        /// <summary>
        /// あるノードを始点にして接続を作成
        /// </summary>
        /// <param name="startNode">始点となるノード</param>
        /// <param name="startPosition">ノードの接点の位置</param>
        public static void StartConnecting(Node startNode, JunctionPosition startPosition)
        {
            if (current != null)
            {
                throw new UnityException("Already started connecting.");
            }

            if (connected.ContainsKey(startNode.Id) && connected[startNode.Id].ContainsKey(startPosition))
            {
                throw new UnityException("Already connected node.");
            }

            current = new Connector(startNode, startPosition);
        }

        public static void CancelConnecting()
        {
            current = null;
        }

        public static bool IsCurrent(Node node, JunctionPosition position)
        {
            return HasCurrent && current.StartNode.Id == node.Id && current.StartPosition == position;
        }

        /// <summary>
        /// 終点となるノードを決定
        /// </summary>
        /// <param name="endNode">終点となるノード</param>
        /// <param name="endPosition">ノードの接点の位置</param>
        public static void Connect(Node endNode, JunctionPosition endPosition)
        {
            if (current == null)
            {
                throw new UnityException("No current connector.");
            }

            current.Connect(endNode, endPosition);
            connectors.Add(current);

            // 接続情報を登録
            if (!connected.ContainsKey(current.StartNode.Id))
            {
                connected[current.StartNode.Id] = new Dictionary<JunctionPosition, Connector>();
            }
            connected[current.StartNode.Id].Add(current.StartPosition, current);

            if (!connected.ContainsKey(current.EndNode.Id))
            {
                connected[current.EndNode.Id] = new Dictionary<JunctionPosition, Connector>();
            }
            connected[current.EndNode.Id].Add(current.EndPosition, current);

            current = null;
        }

        /// <summary>
        /// あるノードの接続点に接続されている接続を返します
        /// </summary>
        /// <param name="node">ノード</param>
        /// <param name="position">接続点の位置</param>
        /// <returns>接続. 接続されていない場合はnull</returns>
        public static Connector GetConnector(Node node, JunctionPosition position)
        {
            if (connected.ContainsKey(node.Id) && connected[node.Id].ContainsKey(position))
            {
                return connected[node.Id][position];
            }
            else
            {
                return null;
            }
        }

        public static bool IsConnected(Node node, JunctionPosition position)
        {
            return GetConnector(node, position) != null;
        }

        /// <summary>
        /// ある接続点に接続されている接続を解除します
        /// </summary>
        /// <param name="node">始点若しくは終点として接続されているノード</param>
        /// <param name="position">接続点の位置</param>
        public static void Disconnect(Node node, JunctionPosition position)
        {
            var con = GetConnector(node, position);
            if (con == null)
            {
                return;
            }

            //選択されている接続が削除されるとき
            if (selected == con)
            {
                selected = null;
            }

            for (int i = 0; i < connectors.Count; i++)
            {
                var other = connectors[i];
                if (con.StartNode.Id == other.StartNode.Id && con.StartPosition == other.StartPosition &&
                    con.EndNode.Id == other.EndNode.Id && con.EndPosition == other.EndPosition)
                {
                    connectors.RemoveAt(i);
                    break;
                }
            }

            connected[con.StartNode.Id].Remove(con.StartPosition);
            connected[con.EndNode.Id].Remove(con.EndPosition);
        }


        /// <summary>
        /// 管理している接続の描画
        /// </summary>
        /// <param name="mousePosition">マウスの位置情報</param>
        public static void Update(Vector2 mousePosition)
        {
            connectors.ForEach(con => con.Draw());

            if (current != null)
            {
                current.DrawTo(mousePosition);
            }
        }
    }


}