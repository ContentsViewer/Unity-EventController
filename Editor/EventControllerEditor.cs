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



namespace EventControl.EventControllerEditor
{




    //===EditorWindow===========================================================================================================
    public class EditorWindow : UnityEditor.EditorWindow
    {
        public static EditorWindow Instance { get; private set; }




        public bool MouseIsInGraphArea { get; private set; }




        //===パラメータ============================================================================
        //EventController
        EventController eventControllerUnderEditing = null;

        //Save用
        SerializedObject serializedCurrentEventCtrl = null;
        SerializedProperty serializedCurrentLayerList = null;
        SerializedProperty serializedParamList = null;

        //Layer
        Layer currentLayer = null;

        EventNode currentEventNodePrev = null;

        Node selectedNodePrev = null;

        BaseEventClip inputEventClip;
        MonoScript inputMonoScript;
        Dictionary<int, Node> nodes = new Dictionary<int, Node>();

        bool eventClipFoldout = false;
        SerializedPropertyDrawer eventClipPropertyDrawer = new SerializedPropertyDrawer();
        BaseEventClip selectedEventClip = null;
        BaseEventClip selectedEventClipPrev = null;


        Vector2 eventClipSettingScroll;
        Vector2 parameterListScroll;


        MessageWindow messageWindow = new MessageWindow(new Vector2(100.0f, 20.0f), false, 20);

        //Editorによる編集を行うかどうか
        bool enabled = false;

        bool isPlayingPrev = false;
        bool isCompilingPrev = false;

        Rect sideAreaRect;
        Rect graphAreaRect;



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
            window.minSize = new Vector2(100f, 100f);
        }

        void Init()
        {
            wantsMouseMove = true;
            ConnectorManager.Init();
            NodeManager.Init();

            Instance = this;
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
                EditorGUILayout.LabelField("Editor is switching to play mode.");
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
                if (eventControllerUnderEditing != selectedEventCtrl)
                {
                    eventControllerUnderEditing = selectedEventCtrl;
                    StartEditor();
                }
            }

            if (!eventControllerUnderEditing)
            {
                EditorGUILayout.LabelField("No EventController selected.");
                enabled = false;
            }

        }

        //Editorをリセット
        void ResetEditor()
        {
            eventControllerUnderEditing = null;
        }

        //編集処理
        void Edit()
        {
            //userからの入力情報取得
            var currentEvent = Event.current;

            // graphAreaのサイズ
            graphAreaRect = new Rect(sideAreaRect.xMax, sideAreaRect.yMin, position.width - sideAreaRect.xMax, position.height);
            MouseIsInGraphArea = graphAreaRect.Contains(currentEvent.mousePosition);
            //Debug.Log(GraphAreaRect);


            //---ノードグラフの描画---------------------------------------------------------------------------------------
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

                ConnectorManager.Update(Event.current.mousePosition);




                BeginWindows();
                foreach (var node in nodes.Values)
                {
                    node.Update();
                }
                EndWindows();


                // 決定中の接続がある場合は右クリックでキャンセル
                if (ConnectorManager.HasCurrent && currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
                {
                    ConnectorManager.CancelConnecting();
                }

                if (ConnectorManager.HasCurrent)
                {
                    // 関連付けようとしている接続がある場合は描画する
                    Repaint();
                }


            }

            //----------------------------------------------------------------------------------------------------


            // --- Side Area -----------------------------------------------------------------------------------------------
            var sideAreaRectTemp = EditorGUILayout.BeginVertical("box", GUILayout.Width(200), GUILayout.Height(position.height - 6.0f));
            {
                // BeginVerticalの返り値でゼロのときがある. これは省く.
                if (sideAreaRectTemp.width != 0.0f && sideAreaRectTemp.height != 0.0f)
                {
                    sideAreaRect = sideAreaRectTemp;
                }
                //Debug.Log(sideAreaRect);


                //---レイヤーリスト--------------------------------------------------------------------------------

                EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("LayerList");

                GUI.enabled = !EditorApplication.isPlaying;
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    AddLayer(null);
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < eventControllerUnderEditing.layerList.Count; i++)
                {
                    var layer = eventControllerUnderEditing.layerList[i];

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

                        var node = FindNode(currentLayer.serializableEventNodeList[currentLayer.eventNodeList.IndexOf(currentLayer.currentEventNode)]);
                        NodeManager.running = node;
                        //if (currentLayer.currentEventNode.eventClip is IMessageableEventClip)
                        //{

                        //}
                    }

                    if (NodeManager.running != null)
                    {
                        if (NodeManager.running.eventNode.eventClip is IMessageableEventClip)
                        {

                            messageWindow.Position = new Vector2(NodeManager.running.rect.x, NodeManager.running.rect.y + NodeManager.running.rect.height + 10.0f);
                            messageWindow.message = ((IMessageableEventClip)NodeManager.running.eventNode.eventClip).Message;
                            //Debug.Log(messageWindow.message);
                            messageWindow.Update();
                        }
                    }

                    Repaint();
                }

                // ゲーム実行中ではない場合
                else
                {
                    NodeManager.running = null;

                    //messageWindow.OnGUIEvent = null;


                }
                //-------------------------------------------------------------------------------------------------

                //---ノード詳細画面---------------------------------------------------------------------
                // ノードを作成するための左カラムを描画していく
                EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(currentLayer.name);
                EditorGUILayout.EndHorizontal();

                //Game再生中は編集させない
                GUI.enabled = !EditorApplication.isPlaying;
                {
                    // テキストを表示するノードの作成
                    EditorGUILayout.BeginHorizontal();
                    inputEventClip = EditorGUILayout.ObjectField("EventClip(from GameObject)", inputEventClip, typeof(BaseEventClip), true, GUILayout.ExpandWidth(true)) as BaseEventClip;
                    //inputMonoScript = EditorGUILayout.ObjectField("EventClip", inputMonoScript, typeof(MonoScript), true, GUILayout.ExpandWidth(true)) as MonoScript;
                    if (GUILayout.Button("Create", GUILayout.Width(60)))
                    {
                        if (inputEventClip == null)
                        {

                            var eventNode = new SerializableEventNode(inputEventClip);

                            currentLayer.serializableEventNodeList.Add(eventNode);

                            var node = new TextNode(eventNode, NodeColor.Green);
                            nodes.Add(node.Id, node);

                            inputEventClip = null;
                        }
                        else if (inputEventClip.eventController == null)
                        {


                            inputEventClip.eventController = eventControllerUnderEditing;
                            //inputEventClip.layer = currentLayer;


                            var eventNode = new SerializableEventNode(inputEventClip);
                            eventNode.name = inputEventClip.gameObject.name;
                            currentLayer.serializableEventNodeList.Add(eventNode);

                            var node = new TextNode(eventNode, NodeColor.Green);
                            nodes.Add(node.Id, node);

                            inputEventClip = null;
                        }

                        // 対象のeventclipがすでに割り当てられているとき
                        else
                        {
                            ShowNotification(new GUIContent("This EventClip has already been assigned. "));
                        }


                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    inputMonoScript = EditorGUILayout.ObjectField("EventClip(from MonoScript)", inputMonoScript, typeof(MonoScript), true, GUILayout.ExpandWidth(true)) as MonoScript;
                    if (GUILayout.Button("Create", GUILayout.Width(60)))
                    {
                        //Debug.Log(inputMonoScript.GetClass()); 
                        if (inputMonoScript != null && inputMonoScript.GetClass() != null && inputMonoScript.GetClass().IsSubclassOf(typeof(BaseEventClip)))
                        {
                            var name = inputMonoScript.GetClass().Name;


                            //Debug.Log(name);
                            GameObject go = new GameObject();
                            go.transform.parent = eventControllerUnderEditing.transform;
                            go.name = name;
                            Undo.AddComponent(go, inputMonoScript.GetClass());
                            Undo.RegisterCreatedObjectUndo(go, "Create Event Node( " + name + " )");

                            var eventNode = new SerializableEventNode(go.GetComponent<BaseEventClip>());
                            eventNode.eventClip.eventController = eventControllerUnderEditing;
                            //eventNode.eventClip.layer = currentLayer;

                            eventNode.name = name;
                            currentLayer.serializableEventNodeList.Add(eventNode);

                            var node = new TextNode(eventNode, NodeColor.Green);
                            nodes.Add(node.Id, node);
                            //inputMonoScript = null;


                        }
                        else
                        {
                            ShowNotification(new GUIContent("This is not EventClip."));
                        }



                        //var eventNode = new EventController.SerializableEventNode(inputEventClip);
                        //currentLayer.serializableEventNodeList.Add(eventNode);

                        //var node = new TextNode(eventNode, NodeColor.Green);
                        //nodes.Add(node.Id, node);
                        //inputEventClip = null;
                    }
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                EditorGUILayout.EndVertical();


                EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Node");
                EditorGUILayout.EndHorizontal();

                var selectedNode = NodeManager.selected;

                // 前回のNodeと異なるとき
                if (selectedNode != selectedNodePrev)
                {

                    GUI.FocusControl("");



                    selectedNodePrev = selectedNode;
                }

                if (selectedNode != null)
                {
                    if (NodeManager.message == "Edit")
                    {

                        EditorGUILayout.BeginHorizontal();
                        selectedNode.eventNode.name = EditorGUILayout.TextField("Name", selectedNode.eventNode.name);
                        EditorGUILayout.EndHorizontal();


                        // 取り外し不可能なEventClipの場合は差し替えを許さない
                        if (selectedNode.eventNode.eventClip is IUndetachableEventClip)
                        {
                            GUI.enabled = false;
                        }
                        EditorGUILayout.BeginHorizontal();



                        // Nodeに割り当てるClipの選択
                        {
                            var nextEventClip = EditorGUILayout.ObjectField(
                                "EventClip", selectedNode.eventNode.eventClip, typeof(BaseEventClip), true, GUILayout.ExpandWidth(true)) as BaseEventClip;

                            // 前回のCLipと異なるとき
                            if (nextEventClip != selectedNode.eventNode.eventClip)
                            {


                                // NullからClip
                                if (selectedNode.eventNode.eventClip == null && nextEventClip != null)
                                {
                                    if (nextEventClip.eventController == null)
                                    {
                                        nextEventClip.eventController = eventControllerUnderEditing;
                                        //nextEventClip.layer = currentLayer;
                                        selectedNode.eventNode.eventClip = nextEventClip;
                                    }
                                    else
                                    {
                                        ShowNotification(new GUIContent("This EventClip has already been assigned. "));
                                    }
                                }

                                // Clipから別のClip
                                else if (selectedNode.eventNode.eventClip != null && nextEventClip != null)
                                {
                                    if (nextEventClip.eventController == null)
                                    {
                                        selectedNode.eventNode.eventClip.eventController = null;

                                        nextEventClip.eventController = eventControllerUnderEditing;
                                        //nextEventClip.layer = currentLayer;
                                        selectedNode.eventNode.eventClip = nextEventClip;
                                    }
                                    else
                                    {
                                        ShowNotification(new GUIContent("This EventClip has already been assigned. "));
                                    }
                                }

                                // ClipからNull
                                else if (selectedNode.eventNode.eventClip != null && nextEventClip == null)
                                {
                                    selectedNode.eventNode.eventClip.eventController = null;
                                    selectedNode.eventNode.eventClip = nextEventClip;
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        GUI.enabled = true;

                        //---EventClipのプロパティーを表示, 設定--------------------------------------------

                        //Game実行中でも編集可能
                        GUI.enabled = true;

                        eventClipSettingScroll = EditorGUILayout.BeginScrollView(eventClipSettingScroll);
                        selectedEventClip = selectedNode.eventNode.eventClip;

                        if (selectedEventClip != null)
                        {
                            Selection.activeGameObject = selectedEventClip.gameObject;

                            //タイトル
                            //eventClipFoldout = EditorGUILayout.InspectorTitlebar(eventClipFoldout, selectedEventClip);

                            //SerializedObject target = null;
                            //SerializedProperty iterator = null;
                            //if (selectedEventClip != selectedEventClipPrev)
                            //{
                            //    target = new SerializedObject(selectedEventClip);
                            //    iterator = target.GetIterator();
                            //}

                            ////SerializedObjectとproperyが存在するとき
                            //if (target != null && iterator != null)
                            //{
                            //    if (eventClipFoldout && iterator.NextVisible(true))
                            //    {
                            //        EditorGUI.indentLevel++;

                            //        //再帰的にすべてのプロパティーを表示する
                            //        do
                            //        {
                            //            eventClipPropertyDrawer.DrawSerializedProperty(iterator);
                            //        }
                            //        while (iterator.NextVisible(false));

                            //        EditorGUI.indentLevel--;
                            //    }

                            //    //編集内容を保存
                            //    target.ApplyModifiedProperties();
                            //}
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

                EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Parameters");

                // Game実行中はパラメータの追加は認めない
                if (EditorApplication.isPlaying)
                {
                    GUI.enabled = false;
                }
                if (GUILayout.Button(string.Empty, GUILayout.Width(10)))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Add/Int"), false, AddInt, null);
                    menu.AddItem(new GUIContent("Add/Float"), false, AddFloat, null);
                    menu.AddItem(new GUIContent("Add/Bool"), false, AddBool, null);
                    menu.AddItem(new GUIContent("Add/Trigger"), false, AddTrigger, null);
                    menu.ShowAsContext();
                }
                GUI.enabled = true;


                EditorGUILayout.EndHorizontal();


                parameterListScroll = EditorGUILayout.BeginScrollView(parameterListScroll);

                UpdateEventCtrlParam();

                EditorGUILayout.EndScrollView();

                EditorGUILayout.EndVertical();
                // End パラメータ表示 -------------------------------------------------------------------------------------

                //---Conditions表示--------------------------------------------------------


                EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

                UpdateEventCtrlConditions();

                EditorGUILayout.EndVertical();
                //---------------------------------------------------------------------------

            }
            EditorGUILayout.EndVertical();
            // End SideArea ----------------


            //---グラフエリア内, 何もないところで右クリック選択解除-------------------------------------------------------------
            if (MouseIsInGraphArea && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                NodeManager.selected = null;
                ConnectorManager.selected = null;



                GUI.FocusControl("");


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

            serializedCurrentEventCtrl = new SerializedObject(eventControllerUnderEditing);
            serializedCurrentLayerList = serializedCurrentEventCtrl.FindProperty("layerList");
            serializedParamList = serializedCurrentEventCtrl.FindProperty("parameterManager").FindPropertyRelative("serializableParameterList");
            //Debug.Log(serializedParamList.name);

            if (eventControllerUnderEditing.layerList.Count > 0)
            {
                currentLayer = eventControllerUnderEditing.layerList[0];
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
                var node = new TextNode(eventNode, NodeColor.Green, eventNode.rect);
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


        /// <summary>
        /// パラメータ更新
        //  ゲーム実行中では, パラメータの追加削除およびパラメータ名の変更は認めない.
        /// </summary>
        void UpdateEventCtrlParam()
        {
            for (int i = 0; i < eventControllerUnderEditing.parameterManager.serializableParameterList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var param = eventControllerUnderEditing.parameterManager.serializableParameterList[i];

                serializedCurrentEventCtrl.Update();
                var serializedParam = serializedParamList.GetArrayElementAtIndex(i);
                SerializedProperty nameProperty = serializedParam.FindPropertyRelative("name");
                SerializedProperty intProperty = serializedParam.FindPropertyRelative("valInt");
                SerializedProperty floatProperty = serializedParam.FindPropertyRelative("valFloat");
                SerializedProperty boolProperty = serializedParam.FindPropertyRelative("valBool");


                GUI.enabled = !EditorApplication.isPlaying;
                nameProperty.stringValue = EditorGUILayout.TextField(nameProperty.stringValue, GUILayout.Width(100));
                GUI.enabled = true;


                switch (param.type)
                {
                    case EventControllerParameter.Type.Int:
                        GUILayout.Label("(Int)", GUILayout.Width(80));
                        intProperty.intValue = EditorGUILayout.IntField(intProperty.intValue);
                        break;

                    case EventControllerParameter.Type.Float:

                        GUILayout.Label("(Float)", GUILayout.Width(80));
                        floatProperty.floatValue = EditorGUILayout.FloatField( floatProperty.floatValue);
                        break;

                    case EventControllerParameter.Type.Bool:

                        GUILayout.Label("(Bool)", GUILayout.Width(80));
                        boolProperty.boolValue = EditorGUILayout.Toggle(boolProperty.boolValue);
                        break;

                    case EventControllerParameter.Type.Trigger:

                        GUILayout.Label("(Trigger)", GUILayout.Width(80));
                        boolProperty.boolValue = EditorGUILayout.Toggle( boolProperty.boolValue);
                        break;
                }


                GUI.enabled = !EditorApplication.isPlaying;
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
                GUI.enabled = true;



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
                    foreach (var param in eventControllerUnderEditing.parameterManager.serializableParameterList)
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
                    condition.parameterName = EditorGUILayout.TextField(condition.parameterName);
                    switch (condition.parameterType)
                    {
                        case EventControllerParameter.Type.Int:
                            condition.compareOption = (CompareOptions)EditorGUILayout.EnumPopup(condition.compareOption);
                            condition.judgeInt = EditorGUILayout.IntField(condition.judgeInt);
                            break;

                        case EventControllerParameter.Type.Float:
                            condition.compareOption = (CompareOptions)EditorGUILayout.EnumPopup(condition.compareOption);
                            condition.judgeFloat = EditorGUILayout.FloatField(condition.judgeFloat);
                            break;

                        case EventControllerParameter.Type.Bool:
                            condition.judgeBool = EditorGUILayout.Toggle(condition.judgeBool);
                            break;

                        case EventControllerParameter.Type.Trigger:
                            break;
                    }


                    if (GUILayout.Button(string.Empty, GUILayout.Width(10)))
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(new GUIContent("Delete"), false, DeleteCondition, condition);
                        menu.AddSeparator("");
                        foreach (var param in eventControllerUnderEditing.parameterManager.serializableParameterList)
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
                var transitions = new List<SerializableTransition>();
                foreach (var junction in node.junctionList)
                {
                    //Exit部分のジャンクションを対象とする
                    if (junction.side == JunctionSide.Right)
                    {
                        var connector = ConnectorManager.GetConnector(node, junction);
                        if (connector != null)
                        {
                            //
                            var transition = new SerializableTransition(connector.conditions, currentLayer.serializableEventNodeList.IndexOf(connector.EndNode.eventNode));
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
            var param = (EventControllerParameter)obj;

            eventControllerUnderEditing.parameterManager.serializableParameterList.Remove(param);
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
            eventControllerUnderEditing.parameterManager.serializableParameterList.Add(new EventControllerParameter("NewInt", EventControllerParameter.Type.Int));
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
            eventControllerUnderEditing.parameterManager.serializableParameterList.Add(new EventControllerParameter("NewFloat", EventControllerParameter.Type.Float));
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
            eventControllerUnderEditing.parameterManager.serializableParameterList.Add(new EventControllerParameter("NewBool", EventControllerParameter.Type.Bool));
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
            eventControllerUnderEditing.parameterManager.serializableParameterList.Add(new EventControllerParameter("NewTrigger", EventControllerParameter.Type.Trigger));
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
            var param = obj as EventControllerParameter;

            if (ConnectorManager.selected != null)
            {
                var conditions = ConnectorManager.selected.conditions;

                conditions.Add(new Condition(param.name, param.type));
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
            var condition = obj as Condition;
            if (ConnectorManager.selected != null)
            {
                var conditions = ConnectorManager.selected.conditions;

                conditions.Remove(condition);
            }
        }


        //---Nodeに対する処理-----------------------------------------------------------------------------------------

        void DeleteEventNode(SerializableEventNode eventNodeToDelete)
        {

            if (eventNodeToDelete.eventClip != null)
            {

                // 削除対象EventClipの所属情報(EventController)を初期化.
                eventNodeToDelete.eventClip.eventController = null;


                // Nodeから取り外し不可能なClipがある場合は, そのClipがついているGameObjectも削除する.
                if (eventNodeToDelete.eventClip is IUndetachableEventClip)
                {
                    GameObject.DestroyImmediate(eventNodeToDelete.eventClip.gameObject);
                }

            }
        }

        void DeleteNode(Node nodeToRemoved)
        {
            // 削除対象に接続されている遷移を削除
            foreach (var position in nodeToRemoved.junctionList)
            {
                nodeToRemoved.DeleteTransition(position);
            }

            // Editor上にある削除対象のNodeを検索
            int removedKey = -1;
            foreach (var node in nodes)
            {
                if (node.Value == nodeToRemoved)
                {
                    removedKey = node.Key;
                }
            }

            DeleteEventNode(nodeToRemoved.eventNode);

            // LayerからEventNodeを削除
            currentLayer.serializableEventNodeList.Remove(nodeToRemoved.eventNode);
            if (removedKey != -1)
            {
                nodes.Remove(removedKey);
            }
        }

        Node FindNode(SerializableEventNode eventNode)
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
            if (eventControllerUnderEditing.layerList.Count > 0)
            {
                name = "NewLayer";
            }
            else
            {
                name = "BaseLayer";
            }
            eventControllerUnderEditing.layerList.Add(new Layer(name));
        }

        void DeleteLayer(object obj)
        {
            Layer layer = obj as Layer;

            if (currentLayer == layer)
            {
                currentLayer = null;
            }

            foreach (var eventNode in layer.serializableEventNodeList)
            {
                DeleteEventNode(eventNode);
            }

            eventControllerUnderEditing.layerList.Remove(layer);
        }
    }




}

