using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelDesignGraph : EditorWindow
{
    private LevelDesignGraphView graphView;
    private string fileName = "New Level";

    [MenuItem("THdev/Level Design Graph")]
    public static void OpenLevelDesignGraphWindow()
    {
        LevelDesignGraph window = GetWindow<LevelDesignGraph>();
        window.titleContent = new GUIContent(text: "Level Design Graph");
    }

    private void OnEnable()
    {
        ConstructLevelDesignGraph();
        GenerateToolbar();
        GenerateMiniMap();
    }

    private void GenerateMiniMap()
    {
        MiniMap miniMap = new MiniMap { anchored = true };
        miniMap.SetPosition(new Rect(10, 30, 200, 140));
        graphView.Add(miniMap);
    }

    private void ConstructLevelDesignGraph()
    {
        graphView = new LevelDesignGraphView(this)
        {
            name = "Level Design Graph"
        };

        graphView.LevelDesignGraphSerializedObject = this;

        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
    }

    private void GenerateToolbar()
    {
        Toolbar toolbar = new Toolbar();

        TextField fileNameTextField = new TextField("File Name:");
        fileNameTextField.SetValueWithoutNotify(fileName);
        fileNameTextField.MarkDirtyRepaint();
        fileNameTextField.RegisterValueChangedCallback(evt => fileName = evt.newValue);
        toolbar.Add(fileNameTextField);

        toolbar.Add(new Button(() => RequestDataOperation(true)) { text = "Save Data" });
        toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "Load Data" });

        rootVisualElement.Add(toolbar);
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(graphView);
    }

    private void RequestDataOperation(bool save)
    {
        if(string.IsNullOrEmpty(fileName))
        {
            EditorUtility.DisplayDialog("Invalid file name!", "Please enter a valid file name.", "OK");
            return;
        }

        GraphSaveUtility saveUtility = GraphSaveUtility.GetInstance(graphView);
        if (save)
        {
            string guid = AssetDatabase.AssetPathToGUID($"Assets/Resources/GraphLevels/{fileName}.asset");
            if (!string.IsNullOrEmpty(guid))
            {
                if (EditorUtility.DisplayDialog("Confirm save operation", $"File {fileName} already exists. Do you want to override it?", "Yes", "No"))
                {
                    saveUtility.SaveGraph(fileName);
                }
                else
                    return;
            }
            else
                saveUtility.SaveGraph(fileName);
        }         
        else
            saveUtility.LoadGraph(fileName);
    }
}
