using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelDesignContainer : ScriptableObject
{
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
    public List<RoomNodeData> RoomNodeData = new List<RoomNodeData>();
    public List<EnemyNodeData> EnemyNodeData = new List<EnemyNodeData>();
    public LevelNodeData LevelNodeData;
}
