using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private LevelDesignGraphView targetGraphView;
    private LevelDesignContainer containerCache;

    private List<Edge> edges => targetGraphView.edges.ToList();
    private List<LevelDesignNode> nodes => targetGraphView.nodes.ToList().Cast<LevelDesignNode>().ToList();
    public static GraphSaveUtility GetInstance(LevelDesignGraphView targetLevelDesignGraphView)
    {
        return new GraphSaveUtility
        {
            targetGraphView = targetLevelDesignGraphView
        };
    }

    public void SaveGraph(string fileName)
    {
        if (!edges.Any()) 
            return;

        LevelDesignContainer levelDesignContainer = ScriptableObject.CreateInstance<LevelDesignContainer>();

        Edge[] connectedPorts = edges.Where(x => x.input.node != null).ToArray();
        for(int i=0; i< connectedPorts.Length; i++)
        {
            LevelDesignNode outputNode = connectedPorts[i].output.node as LevelDesignNode;
            LevelDesignNode inputNode = connectedPorts[i].input.node as LevelDesignNode;
            levelDesignContainer.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = outputNode.GUID,
                PortName = connectedPorts[i].output.portName,
                TargetNodeGuid = inputNode.GUID
            });
        }

        foreach (LevelDesignNode node in nodes/*.Where(node => !node.EntryPoint)*/)
        {
            if(node.GetType() == typeof(RoomNode) )
            {
                RoomNode roomNode = (RoomNode)node;
                levelDesignContainer.RoomNodeData.Add(new RoomNodeData
                {
                    Guid = roomNode.GUID,
                    RoomName = roomNode.RoomName,
                    RoomPrefab = roomNode.RoomPrefab,
                    IsEndingPoint = roomNode.IsEndingPoint,
                    Position = roomNode.GetPosition().position
                });
            }
            else if (node.GetType() == typeof(EnemyNode))
            {
                EnemyNode enemyNode = (EnemyNode)node;

                levelDesignContainer.EnemyNodeData.Add(new EnemyNodeData
                {
                    Guid = enemyNode.GUID,
                    EnemyType = enemyNode.EnemyType,
                    Position = enemyNode.GetPosition().position
                });
            }
            else if(node.GetType() == typeof(LevelNode))
            {
                LevelNode levelNode = (LevelNode)node;

                levelDesignContainer.LevelNodeData = new LevelNodeData
                {
                    Guid = levelNode.GUID,
                    Position = levelNode.GetPosition().position,
                    LevelName = levelNode.LevelName
                };
            }
        }

        //Creates folder if it doesn't exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources/GraphLevels"))
            AssetDatabase.CreateFolder("Assets/Resources", "GraphLevels");

        //saving data without reference lost
        string path = $"Assets/Resources/GraphLevels/{fileName}.asset";
        LevelDesignContainer outputLevelDesignContainer = AssetDatabase.LoadMainAssetAtPath(path) as LevelDesignContainer;
        if(outputLevelDesignContainer != null)
        {
            EditorUtility.CopySerialized(levelDesignContainer, outputLevelDesignContainer);
            ((UnityEngine.Object)outputLevelDesignContainer).name = fileName; 
        }
        else
        {
            AssetDatabase.CreateAsset(levelDesignContainer, $"Assets/Resources/GraphLevels/{fileName}.asset");
        }
        AssetDatabase.SaveAssets();
    }

    public void LoadGraph(string fileName)
    {
        containerCache = Resources.Load<LevelDesignContainer>($"GraphLevels/{fileName}");
        if(containerCache == null)
        {
            EditorUtility.DisplayDialog("File Not Found", "Target level design graph file does not exists!", "OK");
            return;
        }

        ClearGraph();
        CreateNodes();
        ConnectNodes();
    }

    private void ConnectNodes()
    {
        for(int i=0; i<nodes.Count; i++)
        {
            List<NodeLinkData> connections = containerCache.NodeLinks.Where(x => x.BaseNodeGuid == nodes[i].GUID).ToList();
            for(int j=0; j< connections.Count; j++)
            {
                string targetNodeGuid = connections[j].TargetNodeGuid;
                LevelDesignNode targetNode = nodes.FirstOrDefault(x => x.GUID == targetNodeGuid);
                if(targetNode != null)
                {
                    if(nodes[i].EntryPoint == true)
                    {
                        targetNode.capabilities &= ~Capabilities.Deletable;
                        LinkNodes(nodes[i].outputContainer[0].Q<Port>(), (Port)targetNode.inputContainer[0], nodes[i].EntryPoint);
                    } 
                    else if(targetNode.GetType() == typeof(RoomNode))
                    {
                        switch(connections[j].PortName) // not the gratest idea to check port by its name
                        {
                            case "Top":
                                LinkNodes(nodes[i].outputContainer[0].Q<Port>(), (Port)targetNode.inputContainer[0], nodes[i].EntryPoint);
                                break;
                            case "Right":
                                LinkNodes(nodes[i].outputContainer[1].Q<Port>(), (Port)targetNode.inputContainer[0], nodes[i].EntryPoint);
                                break;
                            case "Bottom":
                                LinkNodes(nodes[i].outputContainer[2].Q<Port>(), (Port)targetNode.inputContainer[0], nodes[i].EntryPoint);
                                break;
                        }     
                    }
                    else if (targetNode.GetType() == typeof(EnemyNode))
                        LinkNodes(nodes[i].outputContainer[3].Q<Port>(), (Port)targetNode.inputContainer[0], nodes[i].EntryPoint);

                    if (containerCache.RoomNodeData.FirstOrDefault(x => x.Guid == targetNodeGuid) != null)
                        targetNode.SetPosition(new Rect(containerCache.RoomNodeData.FirstOrDefault(x => x.Guid == targetNodeGuid).Position, targetGraphView.DefaultNodeSize));
                    else if (containerCache.EnemyNodeData.FirstOrDefault(x => x.Guid == targetNodeGuid) != null)
                        targetNode.SetPosition(new Rect(containerCache.EnemyNodeData.FirstOrDefault(x => x.Guid == targetNodeGuid).Position, targetGraphView.DefaultNodeSize));
                } 
            }
        }
    }

    private void LinkNodes(Port output, Port input, bool isEntryPoint)
    {
        Edge tempEdge = new Edge
        {
            output = output,
            input = input
        };

        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);

        if(isEntryPoint) // special case for entry point to make it not removable
        {
            tempEdge.input.pickingMode = PickingMode.Ignore;
            tempEdge.output.pickingMode = PickingMode.Ignore;
            tempEdge.pickingMode = PickingMode.Ignore;

            tempEdge.capabilities &= ~Capabilities.Deletable;
        }

        targetGraphView.Add(tempEdge);
    }

    private void CreateNodes()
    {
        LevelNode levelNode = targetGraphView.GenerateEntryPointNode(containerCache.LevelNodeData);
        levelNode.GUID = containerCache.NodeLinks[0].BaseNodeGuid;
        targetGraphView.AddElement(levelNode);

        foreach (RoomNodeData nodeData in containerCache.RoomNodeData)
        {
            RoomNode tempNode = targetGraphView.CreateRoomNode(nodeData, nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            targetGraphView.AddElement(tempNode);

            List<NodeLinkData> nodePorts = containerCache.NodeLinks.Where(x => x.BaseNodeGuid == nodeData.Guid).ToList();
        }

        foreach (EnemyNodeData nodeData in containerCache.EnemyNodeData)
        {
            EnemyNode tempNode = targetGraphView.CreateEnemyNode(nodeData, nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            targetGraphView.AddElement(tempNode);
        }
    }

    private void ClearGraph()
    {
        foreach(LevelDesignNode node in nodes)
        {
            //Remove edges that connected to this node
            edges.Where(x => x.input.node == node).ToList()
                .ForEach(edge => targetGraphView.RemoveElement(edge));

            //Then remove the node
            targetGraphView.RemoveElement(node);
        }
    }
}
