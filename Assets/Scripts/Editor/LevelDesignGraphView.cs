using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelDesignGraphView : GraphView
{
    public readonly Vector2 DefaultNodeSize = new Vector2(150, 200);

    public LevelDesignGraph LevelDesignGraphSerializedObject;

    private NodeSearchWindow searchWindow;

    private Edge startEdgeLink;

    private List<LevelDesignNode> copyCacheGraphElements;

    private StyleSheet nodeStyleSheet;

    public enum NodeType
    {
        Room = 0,
        Enemies = 1,
        Level = 2
    }

    public LevelDesignGraphView(EditorWindow editorWindow)
    {
        copyCacheGraphElements = new List<LevelDesignNode>();

        styleSheets.Add(Resources.Load<StyleSheet>("LevelDesignGraph"));
        nodeStyleSheet = Resources.Load<StyleSheet>("Node");

        GridBackground gridBackground = new GridBackground();
        gridBackground.StretchToParentSize();
        Insert(0, gridBackground);
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        //setting some basic nodes and links
        GenerateNewGraph();
        
        AddSearchWindow(editorWindow);

        serializeGraphElements += CutCopyOperation;
        canPasteSerializedData += AllowPaste;
        unserializeAndPaste += OnPaste;
    }

    private string CutCopyOperation(IEnumerable<GraphElement> elements)
    {
        copyCacheGraphElements.Clear();
        for(int i=0; i< elements.Count(); i++)
        {
            if(elements.ElementAt(i) is LevelDesignNode)
            {
                copyCacheGraphElements.Add((LevelDesignNode)elements.ElementAt(i));
            }
        }
        
        return "Copied";
    }

    private void OnPaste(string operationName, string data)
    {
        ClearSelection();
        if(copyCacheGraphElements.Count > 0)
        {
            foreach(LevelDesignNode nodeData in copyCacheGraphElements)
            {
                if(nodeData is RoomNode)
                {
                    RoomNode rn = (RoomNode)nodeData;
                    RoomNodeData rnd = new RoomNodeData
                    {
                        RoomName = rn.RoomName,
                        RoomPrefab = rn.RoomPrefab,
                        IsEndingPoint = rn.IsEndingPoint,
                        Position = nodeData.GetPosition().position
                    };
                    RoomNode tempNode = CreateRoomNode(rnd, nodeData.GetPosition().position + new Vector2(100, 100)); // +offset
                    tempNode.GUID = nodeData.GUID;
                    AddToSelection(tempNode);
                    AddElement(tempNode);
                }
                else if (nodeData is EnemyNode)
                {
                    EnemyNodeData eND = new EnemyNodeData
                    {
                        EnemyType = ((EnemyNode)nodeData).EnemyType,
                        Position = nodeData.GetPosition().position
                    };
                    EnemyNode tempNode = CreateEnemyNode(eND, nodeData.GetPosition().position + new Vector2(100, 100)); // +offset
                    tempNode.GUID = nodeData.GUID;
                    AddToSelection(tempNode);
                    AddElement(tempNode);
                }
            }
        }
    }

    private bool AllowPaste(string data)
    {
        //allow paste in level design graph
        return true;
    }

    private void GenerateNewGraph()
    {
        LevelNode startNode = GenerateEntryPointNode(new LevelNodeData() { LevelName = "Level" });
        RoomNode startRoomNode = CreateRoomNode(new RoomNodeData() { RoomName = "Start room" }, new Vector2(300, 200));
        startRoomNode.capabilities &= ~Capabilities.Deletable;
        AddElement(startNode);
        AddElement(startRoomNode);

        //make sure that new link is not removable
        startEdgeLink = new Edge
        {
            output = startNode.outputContainer[0].Q<Port>(),
            input = (Port)startRoomNode.inputContainer[0]
        };

        startEdgeLink.input.Connect(startEdgeLink);
        startEdgeLink.output.Connect(startEdgeLink);

        startEdgeLink.input.pickingMode = PickingMode.Ignore;
        startEdgeLink.output.pickingMode = PickingMode.Ignore;
        startEdgeLink.pickingMode = PickingMode.Ignore;

        startEdgeLink.capabilities &= ~Capabilities.Deletable;

        Add(startEdgeLink);
    }

    private void AddSearchWindow(EditorWindow editorWindow)
    {
        searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        searchWindow.Init(this, editorWindow);
        nodeCreationRequest = context =>
        SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        List<Port> compatiblePorts = new List<Port>();
        ports.ForEach((port) =>
        {
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
            {
                if( (startPort.ClassListContains("RoomPort") && port.ClassListContains("RoomPort") ) ||
                (startPort.ClassListContains("EnemyPort") && port.ClassListContains("EnemyPort")) )
                    compatiblePorts.Add(port);
            }
                
        });

        return compatiblePorts;
    }

    private Port GeneratePort(Node node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float)); //Arbitrary type
    }

    public LevelNode GenerateEntryPointNode(LevelNodeData levelNodeData)
    {
        LevelNode node = new LevelNode
        {
            title = levelNodeData.LevelName,
            LevelName = levelNodeData.LevelName,
            GUID = Guid.NewGuid().ToString(),
            EntryPoint = true
        };

        Port generatedPort = GeneratePort(node, Direction.Output);
        generatedPort.portName = "Starting room";
        generatedPort.AddToClassList("RoomPort");
        
        generatedPort.portColor = new Color(0.196f, 0.4f, 0.278f); //I'm adding color this way as I can't make it work through stylesheets
        node.outputContainer.Add(generatedPort);

        node.styleSheets.Add(nodeStyleSheet);

        //Level Name
        TextField textField = new TextField("Level name:");
        textField.RegisterValueChangedCallback(evt =>
        {
            node.LevelName = evt.newValue;
            node.title = evt.newValue;
        });
        textField.SetValueWithoutNotify(node.title);
        node.mainContainer.Add(textField);

        node.capabilities &= ~Capabilities.Movable;
        node.capabilities &= ~Capabilities.Deletable;

        node.RefreshExpandedState();
        node.RefreshPorts();

        node.SetPosition(new Rect(x: 0, y: 200, width: 100, height: 150));
        return node;
    }

    public void CreateNode(string nodeName, NodeType nodeType, Vector2 mousePosition)
    {
        if (nodeType == NodeType.Room)
            AddElement(CreateRoomNode(new RoomNodeData { RoomName = nodeName }, mousePosition));
        else if (nodeType == NodeType.Enemies)
            AddElement(CreateEnemyNode(new EnemyNodeData { }, mousePosition));
        else if (nodeType == NodeType.Level)
            AddElement(GenerateEntryPointNode(new LevelNodeData() { LevelName = nodeName }));
    }


    public RoomNode CreateRoomNode(RoomNodeData roomNodeData, Vector2 position)
    {
        RoomNode roomNode = new RoomNode
        {
            title = roomNodeData.RoomName,
            RoomName = roomNodeData.RoomName,
            RoomPrefab = roomNodeData.RoomPrefab,
            IsEndingPoint = roomNodeData.IsEndingPoint,
            GUID = Guid.NewGuid().ToString()
        };

        Port inputPort = GeneratePort(roomNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Room";
        inputPort.AddToClassList("RoomPort");
        inputPort.portColor = new Color(0.196f, 0.4f, 0.278f); //I'm adding color this way as I can't make it work through stylesheets
        roomNode.inputContainer.Add(inputPort);

        roomNode.styleSheets.Add(nodeStyleSheet);

        AddRoomPort(roomNode);
        AddEnemiesPort(roomNode);

        //Room Name
        TextField textField = new TextField("Room name:");
        textField.RegisterValueChangedCallback(evt =>
        {
            roomNode.RoomName = evt.newValue;
            roomNode.title = evt.newValue;
        });
        textField.SetValueWithoutNotify(roomNode.title);
        roomNode.mainContainer.Add(textField);

        //Room prefab
        ObjectField objectField = new ObjectField("Room prefab:") { objectType = typeof(GameObject), allowSceneObjects = false  };
        objectField.RegisterValueChangedCallback(evt =>
        {
            roomNode.RoomPrefab = (GameObject)evt.newValue;
        });
        objectField.SetValueWithoutNotify(roomNodeData.RoomPrefab);
        roomNode.mainContainer.Add(objectField);

        //Enemy
        Toggle enumField = new Toggle("Is ending point?");
        enumField.RegisterValueChangedCallback(evt =>
        {
            roomNode.IsEndingPoint = evt.newValue;
        });
        enumField.SetValueWithoutNotify(roomNode.IsEndingPoint);
        roomNode.mainContainer.Add(enumField);

        roomNode.RefreshExpandedState();
        roomNode.RefreshPorts();
        roomNode.SetPosition(new Rect(position, DefaultNodeSize));

        return roomNode;
    }

    public void AddRoomPort(RoomNode roomNode)
    {
        GenerateRoomPort(roomNode, "Top");
        GenerateRoomPort(roomNode, "Right");
        GenerateRoomPort(roomNode, "Bottom");

        roomNode.RefreshExpandedState();
        roomNode.RefreshPorts();
    }

    private void GenerateRoomPort(RoomNode roomNode, string portName)
    {
        Port generatedPort = GeneratePort(roomNode, Direction.Output, Port.Capacity.Single);

        generatedPort.portName = portName;
        generatedPort.AddToClassList("RoomPort");
        generatedPort.styleSheets.Add(nodeStyleSheet);
        generatedPort.portColor = new Color(0.196f, 0.4f, 0.278f);

        roomNode.outputContainer.Add(generatedPort);
    }

    public EnemyNode CreateEnemyNode(EnemyNodeData questNodeData, Vector2 position)
    {
        EnemyNode enemyNode = new EnemyNode
        {
            title = questNodeData.EnemyType.ToString(),
            EnemyType = questNodeData.EnemyType,
            GUID = Guid.NewGuid().ToString()
        };

        Port inputPort = GeneratePort(enemyNode, Direction.Input, Port.Capacity.Single);
        inputPort.portName = "Enemy";
        inputPort.AddToClassList("EnemyPort");
        inputPort.portColor = new Color(0.502f, 0, 0);
        enemyNode.inputContainer.Add(inputPort);

        enemyNode.styleSheets.Add(nodeStyleSheet);

        //Enemy
        EnumField enumField = new EnumField("Enemy");
        enumField.RegisterValueChangedCallback(evt =>
        {
            enemyNode.EnemyType = (Enums.EnemyType)evt.newValue;
            enemyNode.title = ((Enums.EnemyType)evt.newValue).ToString();
        });
        enumField.SetValueWithoutNotify(enemyNode.EnemyType);
        enumField.Init(enemyNode.EnemyType);
        enemyNode.mainContainer.Add(enumField);

        enemyNode.RefreshExpandedState();
        enemyNode.RefreshPorts();
        enemyNode.SetPosition(new Rect(position, DefaultNodeSize));

        return enemyNode;
    }

    public void AddEnemiesPort(RoomNode roomNode)
    {
        Port generatedPort = GeneratePort(roomNode, Direction.Output, Port.Capacity.Multi);

        generatedPort.portName = "Enemies";
        generatedPort.AddToClassList("EnemyPort");
        generatedPort.styleSheets.Add(nodeStyleSheet);
        generatedPort.portColor = new Color(0.502f, 0, 0);

        roomNode.outputContainer.Add(generatedPort);
        roomNode.RefreshExpandedState();
        roomNode.RefreshPorts();
    }
}
