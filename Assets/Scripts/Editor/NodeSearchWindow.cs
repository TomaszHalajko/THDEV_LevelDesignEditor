using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private LevelDesignGraphView graphView;
    private EditorWindow window;
    private Texture2D indentationIcon;

    public void Init(LevelDesignGraphView graphView, EditorWindow window)
    {
        this.graphView = graphView;
        this.window = window;

        //Indetation hack for search window as a transparent icon
        indentationIcon = new Texture2D(1, 1);
        indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0)); // alpha to 0 to make it invisible obv.
        indentationIcon.Apply();
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        List<SearchTreeEntry> tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Make some nodes"), 0),
            new SearchTreeEntry(new GUIContent("Room node", indentationIcon))
            {
                userData = new RoomNode(), level = 1
            },
            new SearchTreeEntry(new GUIContent("Enemy node", indentationIcon))
            {
                userData = new EnemyNode(), level = 1
            },
        };
        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        Vector2 worldMousePosition = window.rootVisualElement.ChangeCoordinatesTo(window.rootVisualElement.parent, context.screenMousePosition - window.position.position);

        Vector2 localMousePosition = graphView.contentViewContainer.WorldToLocal(worldMousePosition);

        switch (SearchTreeEntry.userData)
        {
            case RoomNode roomNode:
                graphView.CreateNode("Room Node", LevelDesignGraphView.NodeType.Room, localMousePosition);
                return true;
            case EnemyNode enemyNode:
                graphView.CreateNode("Required Ingredient Node", LevelDesignGraphView.NodeType.Enemies, localMousePosition);
                return true;
            default: 
                return false;
        }
    }
}
