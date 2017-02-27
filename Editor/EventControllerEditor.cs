/*
*   最終更新日:
*       8.6.2016
*
*   用語:
*       EventNode:
*           各状態のこと
*           NodeにEventClipを登録することができる
*
*       EventClip:
*           実際にEventを実行するもの
*
*       EventController:
*           各EventNodeへの遷移をコントロールするもの
*
*       Transition:
*           あるEventNodeから別のEventNodeに移ること
*
*       Layer:
*           EventNodeとTransitionを含んだ一つの集まり
*           LayerごとにEventNodeは独立に遷移する
*
*   更新履歴:
*       6.1.2016:
*           プログラムの完成
*
*       6.7.2016:
*           EventControllerの更新に伴う修正
*           Layerの概念を取り入れた
*
*       7.2.2016:
*           バグの修正:
*               EditorWindowが開いている状態でもう一度EditorWindowを開こうとすると設定した繊維がすべて消える問題を修正
*               EditorWindowが開いてる状態でスクリプトをコンパイルするとEditorの機能が停止する問題を修正
*
*           Editor上でEventClipのプロパティーを編集できるようになった
*
*       7.3.2016:
*           Game実行中でもEventClipのプロパティーを編集できるようにした
*
*       7.6.2016:
*           比較オプション追加
*
*       7.22.2016:
*           バグの修正:
*               Editorの機能が停止する問題を修正
*
*       8.6.2016:
*           Node内で文字の折り返しができるようにした
*           Nodeを削除した際, Editor画面の更新がすぐにされない問題を修正
*
*/

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


namespace EventControllerEditor
{
    //接続点のサイド
    public enum JUNCTION_SIDE
    {
        LEFT,
        RIGHT
    }

    //ノードのカラー
    public enum NODE_COLOR
    {
        BLUE = 1,
        GREEN = 3,
        YELLOW = 4,
        ORANGE = 5,
        RED = 6
    }

    //接続点の設定のためのクラス
    public class JunctionPosition
    {
        public JUNCTION_SIDE side = JUNCTION_SIDE.LEFT;
        public int top;

        public JunctionPosition(JUNCTION_SIDE side, int top)
        {
            this.side = side;
            this.top = top;
        }
    }

    //ノードID生成クラス
    public static class NodeId
    {
        static long id = 1;
        public static long Create()
        {
            return id++;
        }
    }

    //===EditorWindow===========================================================================================================
    public class EditorWindow : UnityEditor.EditorWindow
    {
        //===パラメータ============================================================================
        //EventController
        EventController eventCtrlBeingEdited = null;

        //Save用
        SerializedObject serializedCurrentEventCtrl = null;
        SerializedProperty serializedCurrentLayerList = null;
        SerializedProperty serializedParamList = null;

        //Layer
        EventController.Layer currentLayer = null;

        EventController.EventNode currentEventNodePrev = null;

        BaseEventClip inputEventClip;
        Dictionary<int, Node> nodes = new Dictionary<int, Node>();

        bool eventClipFoldout = false;
        SerializedPropertyDrawer eventClipPropertyDrawer = new SerializedPropertyDrawer();
        BaseEventClip selectedEventClip = null;
        BaseEventClip selectedEventClipPrev = null;
        Vector2 eventClipSettingScroll;

        //Editorによる編集を行うかどうか
        bool enabled = false;

        bool isPlayingPrev = false;
        bool isCompilingPrev = false;


        //UnityEditorの初期化を検知
        [UnityEditor.InitializeOnLoad]
        static class InitializeChecker
        {
            public static bool onLoad = false;
            static InitializeChecker()
            {
                onLoad = true;
            }
        }

        [MenuItem("EventControllerEditor/Open...")]
        public static void Open()
        {
            var window = GetWindow<EditorWindow>();
            window.minSize = new Vector2(600f, 300f);
        }

        void Init()
        {
            wantsMouseMove = true;
            ConnectorManager.Init();
            NodeManager.Init();
        }

        void OnGUI()
        {
            //編集前の処理
            BeginEdit();

            if (!enabled)
            {
                return;
            }

            //編集
            Edit();
        }

        //編集前の処理
        void BeginEdit()
        {
            enabled = true;

            //コンパイル時
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.LabelField("Restarting...");
                EditorGUILayout.LabelField("Editor is compiling scripts.");
                enabled = false;
            }

            //ゲーム実行に入るとき
            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                enabled = false;
            }

            //コンパイル終了時
            if (!EditorApplication.isCompiling && isCompilingPrev)
            {
                ResetEditor();
            }

            //GameModeからEditorModeになったとき
            if (!EditorApplication.isPlaying && isPlayingPrev)
            {
                ResetEditor();
            }

            //その他の場合としてInitializeCheckerが初期化を確認したとき
            if (InitializeChecker.onLoad)
            {
                InitializeChecker.onLoad = false;
                ResetEditor();
            }

            isCompilingPrev = EditorApplication.isCompiling;
            isPlayingPrev = EditorApplication.isPlaying;

            //ゲーム実行に入るとき
            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.LabelField("Restarting...");
                EditorGUILayout.LabelField("Editor is about to switch to play mode.");
            }


            EventController selectedEventCtrl = null;
            //EventControllerを取得
            if (Selection.gameObjects.Length == 1)
            {
                selectedEventCtrl = Selection.gameObjects[0].GetComponent<EventController>();
            }

            //編集中のEventCtrlと選択されたEventCtrlが異なるとき
            if (selectedEventCtrl)
            {
                if (eventCtrlBeingEdited != selectedEventCtrl)
                {
                    eventCtrlBeingEdited = selectedEventCtrl;
                    StartEditor();
                }
            }

            if (!eventCtrlBeingEdited)
            {
                EditorGUILayout.LabelField("No EventController selected.");
                enabled = false;
            }

        }

        //Editorをリセット
        void ResetEditor()
        {
            eventCtrlBeingEdited = null;
        }

        //編集処理
        void Edit()
        {
            //userからの入力情報取得
            var currentEvent = Event.current;

            //---ノードの描画---------------------------------------------------------------------------------------
            if (currentLayer != null)
            {
                //マウスの中央ボタンでノード全体をドラッグする
                if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 2)
                {
                    foreach (var node in nodes.Values)
                    {
                        node.rect.position += currentEvent.delta;
                    }

                    //描画する
                    Repaint();
                }

                BeginWindows();
                foreach (var node in nodes.Values)
                {
                    node.Update();
                }
                EndWindows();

                // 決定中の接続がある場合は右クリックでキャンセル
                if (ConnectorManager.HasCurrent && currentEvent.type == EventType.mouseDown && currentEvent.button == 1)
                {
                    ConnectorManager.CancelConnecting();
                }

                ConnectorManager.Update(Event.current.mousePosition);
                if (ConnectorManager.HasCurrent)
                {
                    // 関連付けようとしている接続がある場合は描画する
                    Repaint();
                }
            }
            //----------------------------------------------------------------------------------------------------


            //---レイヤーリスト--------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.Width(200));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("LayerList");

            GUI.enabled = !EditorApplication.isPlaying;
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                AddLayer(null);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < eventCtrlBeingEdited.layerList.Count; i++)
            {
                var layer = eventCtrlBeingEdited.layerList[i];

                EditorGUILayout.BeginHorizontal();

                serializedCurrentEventCtrl.Update();
                SerializedProperty serializedLayer = serializedCurrentLayerList.GetArrayElementAtIndex(i);
                SerializedProperty textProperty = serializedLayer.FindPropertyRelative("name");

                //現在編集されているレイヤー名の前に'*'を表示
                if (layer == currentLayer)
                {
                    EditorGUILayout.LabelField("*", GUILayout.Width(10));
                }

                GUI.enabled = !EditorApplication.isPlaying;
                textProperty.stringValue = EditorGUILayout.TextField(textProperty.stringValue);
                GUI.enabled = true;

                serializedCurrentEventCtrl.ApplyModifiedProperties();

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    currentLayer = layer;
                    SetEditor();
                }

                GUI.enabled = !EditorApplication.isPlaying;
                if (GUILayout.Button(string.Empty, GUILayout.Width(30)))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Delete"), false, DeleteLayer, layer);
                    menu.ShowAsContext();
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            //レイヤーが選択されていない場合はここで終了する
            if (currentLayer == null)
            {
                return;
            }

            //----------------------------------------------------------------------------------------

            //---ゲーム実行中-------------------------------------------------------------------------------------
            if (EditorApplication.isPlaying)
            {
                if (currentLayer.currentEventNode != null && currentLayer.currentEventNode != currentEventNodePrev)
                {
                    currentEventNodePrev = currentLayer.currentEventNode;
                    NodeManager.running = FindNode(currentLayer.serializableEventNodeList[currentLayer.eventNodeList.IndexOf(currentLayer.currentEventNode)]);
                }
                Repaint();
            }
            else
            {
                NodeManager.running = null;
            }
            //-------------------------------------------------------------------------------------------------

            //---ノード詳細画面---------------------------------------------------------------------
            // ノードを作成するための左カラムを描画していく
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.Width(200));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(currentLayer.name);
            EditorGUILayout.EndHorizontal();

            //Game再生中は編集させない
            GUI.enabled = !EditorApplication.isPlaying;

            // テキストを表示するノードの作成
            EditorGUILayout.BeginHorizontal();
            inputEventClip = EditorGUILayout.ObjectField("EventClip", inputEventClip, typeof(BaseEventClip), true, GUILayout.ExpandWidth(true)) as BaseEventClip;

            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                var eventNode = new EventController.SerializableEventNode(inputEventClip);
                currentLayer.serializableEventNodeList.Add(eventNode);

                var node = new TextNode(eventNode, NODE_COLOR.GREEN);
                nodes.Add(node.Id, node);
                inputEventClip = null;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Node");
            EditorGUILayout.EndHorizontal();

            var selectedNode = NodeManager.selected;
            if (selectedNode != null)
            {
                if (NodeManager.message == "Edit")
                {
                    EditorGUILayout.BeginHorizontal();
                    selectedNode.eventNode.name = EditorGUILayout.TextField("Name", selectedNode.eventNode.name);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    selectedNode.eventNode.eventClip = EditorGUILayout.ObjectField(
                        "EventClip", selectedNode.eventNode.eventClip, typeof(BaseEventClip), true, GUILayout.ExpandWidth(true)) as BaseEventClip;
                    EditorGUILayout.EndHorizontal();


                    //---EventClipのプロパティーを表示, 設定--------------------------------------------

                    //Game実行中でも編集可能
                    GUI.enabled = true;

                    eventClipSettingScroll = EditorGUILayout.BeginScrollView(eventClipSettingScroll);
                    selectedEventClip = selectedNode.eventNode.eventClip;

                    //タイトル
                    eventClipFoldout = EditorGUILayout.InspectorTitlebar(eventClipFoldout, selectedEventClip);

                    SerializedObject target = null;
                    SerializedProperty iterator = null;
                    if (selectedEventClip != selectedEventClipPrev)
                    {
                        target = new SerializedObject(selectedEventClip);
                        iterator = target.GetIterator();
                    }

                    //SerializedObjectとproperyが存在するとき
                    if (target != null && iterator != null)
                    {
                        if (eventClipFoldout && iterator.NextVisible(true))
                        {
                            EditorGUI.indentLevel++;

                            //再帰的にすべてのプロパティーを表示する
                            do
                            {
                                eventClipPropertyDrawer.DrawSerializedProperty(iterator);
                            }
                            while (iterator.NextVisible(false));

                            EditorGUI.indentLevel--;
                        }

                        //編集内容を保存
                        target.ApplyModifiedProperties();
                    }

                    EditorGUILayout.EndScrollView();
                }
                else if (NodeManager.message == "Delete")
                {
                    DeleteNode(selectedNode);
                    NodeManager.message = "(non)";
                    Repaint();
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndVertical();
            //----------------------------------------------------------------------------

            //---パラメータ表示-----------------------------------------------------------
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.Width(200));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Params");

            if (GUILayout.Button(string.Empty, GUILayout.Width(10)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add/Int"), false, AddInt, null);
                menu.AddItem(new GUIContent("Add/Float"), false, AddFloat, null);
                menu.AddItem(new GUIContent("Add/Bool"), false, AddBool, null);
                menu.AddItem(new GUIContent("Add/Trigger"), false, AddTrigger, null);
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            UpdateEventCtrlParam();

            EditorGUILayout.EndVertical();
            //-------------------------------------------------------------------------------------

            //---Conditions表示--------------------------------------------------------
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.Width(200));

            UpdateEventCtrlConditions();

            EditorGUILayout.EndVertical();
            //---------------------------------------------------------------------------

            //---何もないところで右クリック選択解除-------------------------------------------------------------
            if (currentEvent.type == EventType.mouseDown && currentEvent.button == 0)
            {
                NodeManager.selected = null;
                ConnectorManager.selected = null;
                Repaint();
            }
            //-----------------------------------------------------------------------------------------------

            //EventControllerに変更適用
            UpdateEventCtrlNode();
        }

        //
        //関数
        //  説明:
        //      EventControllerを読み込みレイヤー読み込みの手前までします
        //      レイヤーが1つ以上あるとき1番目のレイヤーの編集を開始します
        //
        void StartEditor()
        {
            Init();

            serializedCurrentEventCtrl = new SerializedObject(eventCtrlBeingEdited);
            serializedCurrentLayerList = serializedCurrentEventCtrl.FindProperty("layerList");
            serializedParamList = serializedCurrentEventCtrl.FindProperty("paramList");

            if (eventCtrlBeingEdited.layerList.Count > 0)
            {
                currentLayer = eventCtrlBeingEdited.layerList[0];
                SetEditor();
            }
        }


        //
        //関数
        //  説明:
        //      レイヤーの情報を読み込みエディターを設定します
        //
        void SetEditor()
        {
            //各コンポーネント初期化
            nodes.Clear();
            ConnectorManager.Init();
            NodeManager.Init();

            //EventControllerからノードを取得,作成
            foreach (var eventNode in currentLayer.serializableEventNodeList)
            {
                var node = new TextNode(eventNode, NODE_COLOR.GREEN, eventNode.rect);
                if (eventNode.entry)
                {
                    NodeManager.entry = node;
                }
                nodes.Add(node.Id, node);
            }

            //Transition作成
            foreach (var eventNode in currentLayer.serializableEventNodeList)
            {
                Node startNode = FindNode(eventNode);
                if (startNode != null)
                {
                    foreach (var transition in eventNode.outputTransitions)
                    {
                        JunctionPosition startPosition = startNode.AddExit();
                        if (transition.indexOfToEventNode != -1)
                        {
                            Node endNode = FindNode(currentLayer.serializableEventNodeList[transition.indexOfToEventNode]);
                            if (endNode != null)
                            {
                                JunctionPosition endPosition = endNode.AddEntrance();
                                ConnectorManager.StartConnecting(startNode, startPosition);
                                ConnectorManager.Connect(endNode, endPosition);

                                ConnectorManager.GetConnector(startNode, startPosition).conditions = transition.conditions;
                            }
                        }
                    }
                }
            }
        }

        //
        //関数
        //  説明:
        //      パラメータ更新
        //      ゲーム実行中に変更可能
        //
        void UpdateEventCtrlParam()
        {
            for (int i = 0; i < eventCtrlBeingEdited.paramList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var param = eventCtrlBeingEdited.paramList[i];

                serializedCurrentEventCtrl.Update();
                var serializedParam = serializedParamList.GetArrayElementAtIndex(i);
                SerializedProperty nameProperty = serializedParam.FindPropertyRelative("name");
                SerializedProperty intProperty = serializedParam.FindPropertyRelative("valInt");
                SerializedProperty floatProperty = serializedParam.FindPropertyRelative("valFloat");
                SerializedProperty boolProperty = serializedParam.FindPropertyRelative("valBool");

                nameProperty.stringValue = EditorGUILayout.TextField(nameProperty.stringValue, GUILayout.Width(100));
                switch (param.type)
                {
                    case EventController.PARAM_TYPE.INT:

                        intProperty.intValue = EditorGUILayout.IntField("(Int)", intProperty.intValue);
                        break;

                    case EventController.PARAM_TYPE.FLOAT:

                        floatProperty.floatValue = EditorGUILayout.FloatField("(Float)", floatProperty.floatValue);
                        break;

                    case EventController.PARAM_TYPE.BOOL:
                        boolProperty.boolValue = EditorGUILayout.Toggle("(Bool)", boolProperty.boolValue);
                        break;

                    case EventController.PARAM_TYPE.TRIGGER:
                        boolProperty.boolValue = EditorGUILayout.Toggle("(Trigger)", boolProperty.boolValue);
                        break;
                }

                if (GUILayout.Button(string.Empty, GUILayout.Width(10)))
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Delete"), false, DeleteParam, param);
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Add/Int"), false, AddInt, null);
                    menu.AddItem(new GUIContent("Add/Float"), false, AddFloat, null);
                    menu.AddItem(new GUIContent("Add/Bool"), false, AddBool, null);
                    menu.AddItem(new GUIContent("Add/Trigger"), false, AddTrigger, null);
                    menu.ShowAsContext();
                }
                serializedCurrentEventCtrl.ApplyModifiedProperties();
                EditorGUILayout.EndHorizontal();
            }
        }


        //
        //関数:
        //  説明:
        //      Condition更新
        //      Connectorが選択されているときに変更が有効になる
        //      ゲーム実行中に変更不可
        //
        void UpdateEventCtrlConditions()
        {
            if (ConnectorManager.selected != null)
            {
                var conditions = ConnectorManager.selected.conditions;

                //ゲーム再生中は編集させない
                GUI.enabled = !EditorApplication.isPlaying;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Conditions");

                if (GUILayout.Button(string.Empty, GUILayout.Width(10)))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (var param in eventCtrlBeingEdited.paramList)
                    {
                        menu.AddItem(new GUIContent("Add/" + param.name), false, AddCondition, param);
                    }
                    menu.ShowAsContext();
                }

                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < conditions.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    var condition = conditions[i];
                    condition.paramName = EditorGUILayout.TextField(condition.paramName);
                    switch (condition.paramType)
                    {
                        case EventController.PARAM_TYPE.INT:
                            condition.compareOption = (EventController.COMPARE_OPTIONS)EditorGUILayout.EnumPopup(condition.compareOption);
                            condition.judgeInt = EditorGUILayout.IntField(condition.judgeInt);
                            break;

                        case EventController.PARAM_TYPE.FLOAT:
                            condition.compareOption = (EventController.COMPARE_OPTIONS)EditorGUILayout.EnumPopup(condition.compareOption);
                            condition.judgeFloat = EditorGUILayout.FloatField(condition.judgeFloat);
                            break;

                        case EventController.PARAM_TYPE.BOOL:
                            condition.judgeBool = EditorGUILayout.Toggle(condition.judgeBool);
                            break;

                        case EventController.PARAM_TYPE.TRIGGER:
                            break;
                    }


                    if (GUILayout.Button(string.Empty, GUILayout.Width(10)))
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(new GUIContent("Delete"), false, DeleteCondition, condition);
                        menu.AddSeparator("");
                        foreach (var param in eventCtrlBeingEdited.paramList)
                        {
                            menu.AddItem(new GUIContent("Add/" + param.name), false, AddCondition, param);
                        }
                        menu.ShowAsContext();
                    }

                    EditorGUILayout.EndHorizontal();
                }

                GUI.enabled = true;
            }
        }

        //
        //関数:
        //  説明:
        //      ノードを更新
        //      EventControllerに変更を反映させます
        //      Transitionは設定しなおされます
        //      変更内容はrect, tranditionsです
        //      
        void UpdateEventCtrlNode()
        {
            //各ノードに対する処理
            foreach (var node in nodes.Values)
            {
                var transitions = new List<EventController.SerializableTransition>();
                foreach (var junction in node.junctionList)
                {
                    //Exit部分のジャンクションを対象とする
                    if (junction.side == JUNCTION_SIDE.RIGHT)
                    {
                        var connector = ConnectorManager.GetConnector(node, junction);
                        if (connector != null)
                        {
                            //
                            var transition = new EventController.SerializableTransition(connector.conditions, currentLayer.serializableEventNodeList.IndexOf(connector.EndNode.eventNode));
                            transitions.Add(transition);
                        }
                    }

                }

                //ノードを更新
                node.eventNode.rect = node.Rect;
                node.eventNode.outputTransitions = transitions;
            }

        }

        //---パラメータに対する処理--------------------------------------------------------------------------------

        //
        //関数:
        //  説明:
        //      パラメータ削除
        //
        //  引数:
        //      object:
        //          EventController.Param 
        void DeleteParam(object obj)
        {
            var param = (EventController.Param)obj;

            eventCtrlBeingEdited.paramList.Remove(param);
        }

        //
        //関数:
        //  説明:
        //      パラメータ追加
        //
        //  引数:
        //      object:
        //          null
        void AddInt(object obj)
        {
            eventCtrlBeingEdited.paramList.Add(new EventController.Param("NewInt", EventController.PARAM_TYPE.INT));
        }

        //
        //関数:
        //  説明:
        //      パラメータ追加
        //
        //  引数:
        //      object:
        //          null
        void AddFloat(object obj)
        {
            eventCtrlBeingEdited.paramList.Add(new EventController.Param("NewFloat", EventController.PARAM_TYPE.FLOAT));
        }

        //
        //関数:
        //  説明:
        //      パラメータ追加
        //
        //  引数:
        //      object:
        //          null
        void AddBool(object obj)
        {
            eventCtrlBeingEdited.paramList.Add(new EventController.Param("NewBool", EventController.PARAM_TYPE.BOOL));
        }

        //
        //関数:
        //  説明:
        //      パラメータ追加
        //
        //  引数:
        //      object:
        //          null
        void AddTrigger(object obj)
        {
            eventCtrlBeingEdited.paramList.Add(new EventController.Param("NewTrigger", EventController.PARAM_TYPE.TRIGGER));
        }


        //---Conditionに対する処理--------------------------------------------------------------------------------------

        //
        //関数:
        //  説明:
        //      Condition追加
        //
        //  引数:
        //      object:
        //          EventController.Param 
        void AddCondition(object obj)
        {
            var param = obj as EventController.Param;

            if (ConnectorManager.selected != null)
            {
                var conditions = ConnectorManager.selected.conditions;

                conditions.Add(new EventController.Condition(param.name, param.type));
            }
        }

        //
        //関数:
        //  説明:
        //      Condition削除
        //
        //  引数:
        //      object:
        //          EventController.Condition
        void DeleteCondition(object obj)
        {
            var condition = obj as EventController.Condition;
            if (ConnectorManager.selected != null)
            {
                var conditions = ConnectorManager.selected.conditions;

                conditions.Remove(condition);
            }
        }


        //---Nodeに対する処理-----------------------------------------------------------------------------------------

        void DeleteNode(Node removed)
        {
            foreach (var position in removed.junctionList)
            {
                removed.DeleteTransition(position);
            }

            int removedKey = -1;
            foreach (var node in nodes)
            {
                if (node.Value == removed)
                {
                    removedKey = node.Key;
                }
            }

            currentLayer.serializableEventNodeList.Remove(removed.eventNode);
            if (removedKey != -1)
            {
                nodes.Remove(removedKey);
            }
        }

        Node FindNode(EventController.SerializableEventNode eventNode)
        {
            foreach (var node in nodes.Values)
            {
                if (node.eventNode == eventNode)
                {
                    return node;
                }
            }

            return null;
        }


        void AddLayer(object obj)
        {
            string name;
            if (eventCtrlBeingEdited.layerList.Count > 0)
            {
                name = "NewLayer";
            }
            else
            {
                name = "BaseLayer";
            }
            eventCtrlBeingEdited.layerList.Add(new EventController.Layer(name));
        }

        void DeleteLayer(object obj)
        {
            EventController.Layer layer = obj as EventController.Layer;

            if (currentLayer == layer)
            {
                currentLayer = null;
            }
            eventCtrlBeingEdited.layerList.Remove(layer);
        }
    }




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

        public List<EventController.Condition> conditions = new List<EventController.Condition>();

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
            var startTan = new Vector3(StartPosition.side == JUNCTION_SIDE.LEFT ? start.x - 100f : start.x + 100f, start.y, 0f);

            var end = EndNode.CalculateConnectorPoint(EndPosition);
            var endV3 = new Vector3(end.x, end.y, 0f);
            var endTan = new Vector3(EndPosition.side == JUNCTION_SIDE.LEFT ? end.x - 100f : end.x + 100f, end.y, 0f);

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

    //ノードを管理するクラス
    //ノードからのメッセージを外部に伝えます
    public static class NodeManager
    {
        public static Node selected;
        public static string message = "(non)";
        public static Node entry;
        public static Node running;
        public static void Init()
        {
            message = "(non)";
            selected = null;
            entry = null;
            running = null;
        }
    }

    /// <summary>
    /// ノードの基底クラス
    /// ノード自身の描画や接続点の管理を行う
    /// </summary>
    public abstract class Node
    {
        //===パラメータ=====================================================
        protected readonly Vector2 JunctionSize = new Vector2(10.0f, 10.0f);

        protected readonly NODE_COLOR defaultColor = NODE_COLOR.GREEN;
        protected readonly NODE_COLOR selectedColor = NODE_COLOR.BLUE;
        protected readonly NODE_COLOR entryColor = NODE_COLOR.ORANGE;
        protected readonly NODE_COLOR runningColor = NODE_COLOR.YELLOW;

        public EventController.SerializableEventNode eventNode;
        int id;
        public Rect rect;
        NODE_COLOR color;
        Vector2 defaultSize = new Vector2(150.0f, 50.0f);

        public List<JunctionPosition> junctionList = new List<JunctionPosition>();

        public int Id { get { return id; } }

        public Rect Rect { get { return rect; } }


        //===コード=============================================================================

        public Node(Rect rect, NODE_COLOR color)
        {
            id = (int)NodeId.Create();
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
                var style = position.side == JUNCTION_SIDE.LEFT ? "LargeButtonRight" : "LargeButtonLeft";
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
                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
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
                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
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
                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
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
                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
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
            GUI.DragWindow();
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
            var isLeft = (position.side == JUNCTION_SIDE.LEFT);
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

            var isLeft = (position.side == JUNCTION_SIDE.LEFT);
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
                if (junction.side == JUNCTION_SIDE.LEFT)
                {
                    count++;
                }
            }

            junctionList.Add(new JunctionPosition(JUNCTION_SIDE.LEFT, count));
            ResizeNode();

            return junctionList[junctionList.Count - 1];
        }

        public JunctionPosition AddExit()
        {
            int count = 0;
            foreach (JunctionPosition junction in junctionList)
            {
                if (junction.side == JUNCTION_SIDE.RIGHT)
                {
                    count++;
                }
            }

            junctionList.Add(new JunctionPosition(JUNCTION_SIDE.RIGHT, count));
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
                if (junction.side == JUNCTION_SIDE.LEFT)
                {
                    topLeft++;
                }
                else if (junction.side == JUNCTION_SIDE.RIGHT)
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

    /// <summary>
    /// テキストを表示するノード
    /// </summary>
    public class TextNode : Node
    {
        string text;

        public TextNode(EventController.SerializableEventNode eventNode, NODE_COLOR color) : base(new Rect(310, 10, 150, 50), color)
        {
            this.text = eventNode.name;
            this.eventNode = eventNode;
        }

        public TextNode(EventController.SerializableEventNode eventNode, NODE_COLOR color, Rect rect) : base(rect, color)
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

