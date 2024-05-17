using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using UnityEngine;
using System.Xml.Serialization;
using System;
using System.Xml.Linq;
using UnityEditor;
using System.Linq;
using UnityEditor.Animations;

public class Importer : MonoBehaviour
{
    public string xmlFileName = "PathToYourFileHere";
    public SpriteSheetImporter mySpriteSheetImporter;
    public Sprite[] sprites;

    private void Start()
    {
        mySpriteSheetImporter = ImportFromXML(xmlFileName);

        if (sprites == null || sprites.Length < 1)
        {
            // Load spritesheet by xml here
            sprites = Resources.LoadAll<Sprite>(mySpriteSheetImporter.SpriteSheetFileName);
        }
        if (sprites.Length < 1)
        {
            Debug.LogError("Importer SpriteSheet not loaded correctly.");
        }
        CreateAnimationControllerFromXML(xmlFileName);
        Debug.Log(mySpriteSheetImporter.EnemyName);
    }

    public SpriteSheetImporter ImportFromXML(string filePath)
    {
        var serializer = new XmlSerializer(typeof(SpriteSheetImporter));
        using (var reader = new StreamReader(filePath))
        {
            return (SpriteSheetImporter)serializer.Deserialize(reader);
        }
    }

    public void CreateAnimationControllerFromXML(string xmlFilePath)
    {
        if (!File.Exists(xmlFilePath))
        {
            Debug.LogError("XML file does not exist: " + xmlFilePath);
            return;
        }

        XDocument doc = XDocument.Load(xmlFilePath);
        string spriteSheetFileName = doc.Root.Attribute("SpriteSheetFileName")?.Value;
        if (string.IsNullOrEmpty(spriteSheetFileName) || Resources.Load<Texture2D>(spriteSheetFileName) == null)
        {
            Debug.LogError("Spritesheet not found: " + spriteSheetFileName);
            return;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(spriteSheetFileName);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError("No sprites loaded from spritesheet: " + spriteSheetFileName);
            return;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/MyAnimatorController.controller");
        if (controller == null)
        {
            Debug.LogError("Failed to create Animator Controller at specified path.");
            return;
        }

        // Ensure there is at least one layer
        if (controller.layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
        }

        // Add parameters
        var parameters = doc.Root.Element("Parameters")?.Elements("Parameter");
        if (parameters == null)
        {
            Debug.LogError("No parameters found in XML.");
            return;
        }

        foreach (var param in parameters)
        {
            var paramName = param.Attribute("Name")?.Value;
            var paramType = param.Attribute("Type")?.Value;
            if (string.IsNullOrEmpty(paramName) || string.IsNullOrEmpty(paramType))
            {
                Debug.LogError("Parameter name or type is missing.");
                continue;
            }

            switch (paramType)
            {
                case "Float":
                    controller.AddParameter(paramName, AnimatorControllerParameterType.Float);
                    break;
                case "Bool":
                    controller.AddParameter(paramName, AnimatorControllerParameterType.Bool);
                    break;
                case "Trigger":
                    controller.AddParameter(paramName, AnimatorControllerParameterType.Trigger);
                    break;
                default:
                    Debug.LogError($"Unknown parameter type: {paramType}");
                    break;
            }
        }

        // Dictionary to hold state names and their corresponding animator states
        Dictionary<string, AnimatorState> animatorStates = new Dictionary<string, AnimatorState>();

        // Add states and animations
        var states = doc.Root.Element("States")?.Elements("State");
        if (states == null)
        {
            Debug.LogError("No states found in XML.");
            return;
        }

        foreach (var state in states)
        {
            var stateName = state.Attribute("Name")?.Value;
            var startFrame = state.Attribute("StartingFrame")?.Value;
            var endFrame = state.Attribute("EndingFrame")?.Value;

            if (string.IsNullOrEmpty(stateName) || string.IsNullOrEmpty(startFrame) || string.IsNullOrEmpty(endFrame))
            {
                Debug.LogError("State name or frame range is missing.");
                continue;
            }

            AnimationClip clip = new AnimationClip();
            clip.frameRate = 10; // Set frame rate (10 fps for example)

            // Create keyframes for sprite animation
            ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[int.Parse(endFrame) - int.Parse(startFrame) + 1];
            for (int i = int.Parse(startFrame); i <= int.Parse(endFrame); i++)
            {
                keyFrames[i - int.Parse(startFrame)] = new ObjectReferenceKeyframe
                {
                    time = (i - int.Parse(startFrame)) / clip.frameRate,
                    value = sprites[i]
                };
            }

            // Apply the keyframes to the animation clip
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keyFrames);

            var animatorState = controller.layers[0].stateMachine.AddState(stateName);
            animatorState.motion = clip;

            // Add the state to the dictionary
            animatorStates[stateName] = animatorState;
        }

        // Add transitions based on XML
        foreach (var state in states)
        {
            var stateName = state.Attribute("Name")?.Value;
            if (string.IsNullOrEmpty(stateName) || !animatorStates.TryGetValue(stateName, out var animatorState))
            {
                Debug.LogError($"State '{stateName}' not found in animator states dictionary.");
                continue;
            }

            var transitions = state.Element("Transitions")?.Elements("Transition");
            if (transitions == null)
            {
                Debug.LogWarning($"No transitions found for state '{stateName}'.");
                continue;
            }

            foreach (var transition in transitions)
            {
                var toState = transition.Attribute("To")?.Value;
                var condition = transition.Attribute("Condition")?.Value;

                if (string.IsNullOrEmpty(toState) || string.IsNullOrEmpty(condition))
                {
                    Debug.LogError("Transition target state or condition is missing.");
                    continue;
                }

                if (animatorStates.TryGetValue(toState, out var targetState))
                {
                    var animatorTransition = animatorState.AddTransition(targetState);
                    animatorTransition.AddCondition(AnimatorConditionMode.If, 0, condition);
                }
                else
                {
                    Debug.LogError($"Target state '{toState}' not found for transition from state '{stateName}'.");
                }
            }
        }
    }
}

[XmlRoot("SpriteSheetImporter")]
public class SpriteSheetImporter
{
    [XmlAttribute("EnemyName")]
    public string EnemyName;
    [XmlAttribute("SpriteSheetFileName")]
    public string SpriteSheetFileName;
    [XmlArray("Parameters"), XmlArrayItem("Parameter")]
    public List<Parameter> Parameters;
    [XmlArray("States"), XmlArrayItem("State")]
    public List<State> States;
}

public class Parameter
{
    [XmlAttribute("Name")]
    public string Name;
    [XmlAttribute("Type")]
    public string Type;
}

public class State
{
    [XmlAttribute("Name")]
    public string Name;
    [XmlAttribute("StartingFrame")]
    public int StartingFrame;
    [XmlAttribute("EndingFrame")]
    public int EndingFrame;
    [XmlArray("Transitions"), XmlArrayItem("Transition")]
    public List<Transition> Transitions;
}

public class Transition
{
    [XmlAttribute("To")]
    public string To;
    [XmlAttribute("Condition")]
    public string Condition;
}

