using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Enums;

[Serializable]
public class RoomNodeData
{
    public string Guid;
    public string RoomName;
    public GameObject RoomPrefab;
    public bool IsEndingPoint;

    public Vector2 Position;
}
