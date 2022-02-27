using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Enums;

[Serializable]
public class EnemyNodeData
{
    public string Guid;

    public EnemyType EnemyType;

    public Vector2 Position;
}
