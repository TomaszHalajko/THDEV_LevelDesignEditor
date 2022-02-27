using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Enums;

public class RoomNode : LevelDesignNode
{
    public string RoomName;
    public GameObject RoomPrefab;
    public bool IsEndingPoint;
}
