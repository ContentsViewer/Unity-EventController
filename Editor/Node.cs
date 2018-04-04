
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;



namespace EventControl.EventControllerEditor
{

    /// <summary>
    /// ノードの基底クラス
    /// ノード自身の描画や接続点の管理を行う
    /// </summary>
    public abstract class Node
    {
        //===パラメータ=====================================================
        protected readonly Vector2 JunctionSize = new Vector2(10.0f, 10.0f);

        protected readonly NodeColor defaultColor = NodeColor.Green;
        protected readonly NodeColor selectedColor = NodeColor.Blue;
        protected readonly NodeColor entryColor = NodeColor.Orange;
        protected readonly NodeColor runningColor = NodeColor.Yellow;

        public SerializableEventNode eventNode;
        int id;
        public Rect rect;
        NodeColor color;
        Vector2 defaultSize = new Vector2(150.0f, 50.0f);

        public List<JunctionPosition> junctionList = new List<JunctionPosition>();

        public int Id { get { return id; } }

        public Rect Rect { get { return rect; } }


        //===コード=============================================================================

        public Node(Rect rect, NodeColor color)
        {
            id = (int)WindowIdManager.Create();
            this.rect = rect;
            this.color = color;
        }

        public void Update()
        {
            if (this == NodeManager.running)
            {
                color = runningColor;
            }
            else if (this == NodeManager.selected)
            {
                color = selectedColor;
            }
            else if (eventNode.entry)
            {
                color = entryColor;
            }
            else
            {
                color = defaultColor;
            }

            //Debug.Log("1");
            rect = GUI.Window(id, rect, WindowCallback, string.Empty, "flow node " + ((int)color).ToString());
        }

        /// <summary>
        /// ウィンドウ内のGUI(接続点等)の描画
        /// </summary>
        void WindowCallback(int id)
        {
            //ユーザーからの処理(各ジャンクション)
            for (int i = 0; i < junctionList.Count; i++)
            {
                JunctionPosition position = junctionList[i];
                var style = position.side == JunctionSide.Left ? "LargeButtonRight" : "LargeButtonLeft";
                if (ConnectorManager.HasCurrent)
                {
                    // 決定中の接続がある場合は始点となっている場合, 既に接続済みである場合,決定中の接続と同じサイドに非アクティブ
                    GUI.enabled = !ConnectorManager.IsConnected(this, position) &&
                        !ConnectorManager.IsCurrent(this, position) && ConnectorManager.Current.StartPosition.side != position.side;

                    if (GUI.Button(CalculateJunctionRect(position), string.Empty, style))
                    {
                        // クリックされたら接続
                        ConnectorManager.Connect(this, position);
                    }
                    GUI.enabled = true;
                }
                else
                {
                    Event currentEvent = Event.current;
                    Rect junctionRect = CalculateJunctionRect(position);
                    EditorGUI.DrawRect(junctionRect, Color.gray);

                    //右クリック
                    if (EditorWindow.Instance.MouseIsInGraphArea &&
                        currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
                    {
                        Vector2 mousePos = currentEvent.mousePosition;
                        if (junctionRect.Contains(mousePos))
                        {
                            GenericMenu menu = new GenericMenu();
                            if (!EditorApplication.isPlaying)
                            {
                                menu.AddItem(new GUIContent("MakeTransition"), false, MakeTransition, position);
                                menu.AddItem(new GUIContent("EditTransition"), false, EditTransition, position);
                                menu.AddItem(new GUIContent("DeleteTransition"), false, DeleteTransition, position);
                                menu.AddSeparator("");
                                menu.AddItem(new GUIContent("DeleteJunction"), false, DeleteJunction, position);
                            }
                            else
                            {
                                menu.AddDisabledItem(new GUIContent("MakeTransition"));
                                menu.AddDisabledItem(new GUIContent("EditTransition"));
                                menu.AddDisabledItem(new GUIContent("DeleteTransition"));
                                menu.AddSeparator("");
                                menu.AddDisabledItem(new GUIContent("DeleteJunction"));
                            }

                            menu.ShowAsContext();
                            currentEvent.Use();
                        }
                    }

                    //左クリック
                    if (EditorWindow.Instance.MouseIsInGraphArea &&
                        currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                    {
                        Vector2 mousePos = currentEvent.mousePosition;
                        if (junctionRect.Contains(mousePos))
                        {
                            EditTransition(position);
                            currentEvent.Use();
                        }
                    }
                }
            }


            //ユーザーからの処理(Node全体)
            {
                Event currentEvent = Event.current;
                Rect contextRect = new Rect(0.0f, 0.0f, rect.width, rect.height);

                //右クリック
                if (EditorWindow.Instance.MouseIsInGraphArea &&
                    currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
                {
                    Vector2 mousePos = currentEvent.mousePosition;
                    if (contextRect.Contains(mousePos))
                    {
                        GenericMenu menu = new GenericMenu();

                        if (!EditorApplication.isPlaying)
                        {
                            menu.AddItem(new GUIContent("NewEntrance"), false, NodeMenu, "NewEntrance");
                            menu.AddItem(new GUIContent("NewExit"), false, NodeMenu, "NewExit");
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Edit"), false, NodeMenu, "Edit");
                            menu.AddItem(new GUIContent("SetAsEntry"), false, NodeMenu, "SetAsEntry");
                            menu.AddItem(new GUIContent("Delete"), false, NodeMenu, "Delete");

                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent("NewEntrance"));
                            menu.AddDisabledItem(new GUIContent("NewExit"));
                            menu.AddSeparator("");
                            menu.AddDisabledItem(new GUIContent("Edit"));
                            menu.AddDisabledItem(new GUIContent("SetAsEntry"));
                            menu.AddDisabledItem(new GUIContent("Delete"));
                        }

                        menu.ShowAsContext();
                        currentEvent.Use();
                    }
                }

                //左クリック
                //Debug.Log(currentEvent.mousePosition);
                if (EditorWindow.Instance.MouseIsInGraphArea &&
                     currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                {
                    Vector2 mousePos = currentEvent.mousePosition;
                    if (contextRect.Contains(mousePos))
                    {
                        NodeMenu("Edit");
                        //ここで'currentEvent.Use()'を使わない;もし使うとノードを移動できなくなる
                        //Eventが消費されてしまう
                    }
                }
            }

            OnGUI();

            //Debug.Log("12");

            if (EditorWindow.Instance.MouseIsInGraphArea)
            {

                GUI.DragWindow();
            }
        }


        // ノードの種別毎のUIは子クラスで実装
        abstract protected void OnGUI();

        /// <summary>
        /// 接続点の描画位置を計算して返す
        /// </summary>
        /// <param name="position">接続点の位置</param>
        /// <returns>接続点の描画位置を表す矩形</returns>
        Rect CalculateJunctionRect(JunctionPosition position)
        {
            var isLeft = (position.side == JunctionSide.Left);
            var x = isLeft ? 0.0f : rect.width - JunctionSize.x;
            var y = Mathf.Floor(position.top * JunctionSize.y * 2);

            return new Rect(x, y, JunctionSize.x, JunctionSize.y);
        }

        /// <summary>
        /// 接続点を結ぶ接続を描画する際の始点若しくは終点の座標位置を計算して返す
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector2 CalculateConnectorPoint(JunctionPosition position)
        {
            var junction = CalculateJunctionRect(position);

            var isLeft = (position.side == JunctionSide.Left);
            var x = isLeft ? junction.x : junction.x + junction.width;
            var y = junction.y + JunctionSize.y / 2.0f;

            // ノード(ウィンドウ)の位置を加算して返す
            return new Vector2(x + rect.x, y + rect.y);
        }

        //
        //関数
        //  説明:
        //      ノードメニュー画面から処理の内容を受け取り、
        //      各処理内容に応じて処理します
        //
        void NodeMenu(object obj)
        {
            if ((string)obj == "NewEntrance")
            {
                AddEntrance();
            }
            else if ((string)obj == "NewExit")
            {
                AddExit();
            }
            else if ((string)obj == "Edit")
            {
                NodeManager.message = "Edit";
                NodeManager.selected = this;
            }
            else if ((string)obj == "Delete")
            {
                NodeManager.message = "Delete";
                NodeManager.selected = this;
            }
            else if ((string)obj == "SetAsEntry")
            {
                if (NodeManager.entry != null)
                {
                    NodeManager.entry.eventNode.entry = false;
                }
                eventNode.entry = true;
                NodeManager.entry = this;
            }
        }

        void MakeTransition(object obj)
        {
            JunctionPosition position = obj as JunctionPosition;

            if (!ConnectorManager.IsConnected(this, (JunctionPosition)position))
            {
                ConnectorManager.StartConnecting(this, (JunctionPosition)position);
            }
        }

        public void DeleteTransition(object obj)
        {
            JunctionPosition position = obj as JunctionPosition;

            if (ConnectorManager.IsConnected(this, (JunctionPosition)position))
            {
                ConnectorManager.Disconnect(this, position);
            }
        }

        void DeleteJunction(object obj)
        {
            JunctionPosition position = obj as JunctionPosition;
            DeleteTransition(position);

            var targetSide = position.side;
            var targetTop = position.top;

            junctionList.Remove(position);

            foreach (JunctionPosition junction in junctionList)
            {
                if (junction.side == targetSide && junction.top > targetTop)
                {
                    junction.top--;
                }
            }

            ResizeNode();
        }

        void EditTransition(object obj)
        {
            JunctionPosition position = obj as JunctionPosition;

            var connector = ConnectorManager.GetConnector(this, position);
            if (connector != null)
            {
                ConnectorManager.selected = connector;
            }
            else
            {
                ConnectorManager.selected = null;
            }
        }

        public JunctionPosition AddEntrance()
        {
            int count = 0;
            foreach (JunctionPosition junction in junctionList)
            {
                if (junction.side == JunctionSide.Left)
                {
                    count++;
                }
            }

            junctionList.Add(new JunctionPosition(JunctionSide.Left, count));
            ResizeNode();

            return junctionList[junctionList.Count - 1];
        }

        public JunctionPosition AddExit()
        {
            int count = 0;
            foreach (JunctionPosition junction in junctionList)
            {
                if (junction.side == JunctionSide.Right)
                {
                    count++;
                }
            }

            junctionList.Add(new JunctionPosition(JunctionSide.Right, count));
            ResizeNode();

            return junctionList[junctionList.Count - 1];
        }

        void ResizeNode()
        {
            int topRight = 0;
            int topLeft = 0;

            int topMax = 0;
            foreach (JunctionPosition junction in junctionList)
            {
                if (junction.side == JunctionSide.Left)
                {
                    topLeft++;
                }
                else if (junction.side == JunctionSide.Right)
                {
                    topRight++;
                }
            }

            topMax = (topRight > topLeft ? topRight : topLeft);

            float newHeight = Mathf.Floor(topMax * JunctionSize.y * 2);

            if (newHeight < defaultSize.y)
            {
                rect.height = defaultSize.y;
            }
            else
            {
                rect.height = newHeight;
            }
        }
    }

}