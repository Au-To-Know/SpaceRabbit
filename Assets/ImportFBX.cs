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
using System.Xml;
public class ImporterFBX : MonoBehaviour
{
    public string xmlFileName =  "Assets/TestNewEnemy.xml";
    public FbxAnimationImporter myFbxAnimationImporter;
    public AnimationClip[] clips;

    private void Start() {

        myFbxAnimationImporter = ImportFromXML(xmlFileName);

        Debug.Log(myFbxAnimationImporter.EnemyName);
        Debug.Log(myFbxAnimationImporter.FbxName);
    }
    public FbxAnimationImporter ImportFromXML(string filePath)
    {
        var serializer = new XmlSerializer(typeof(FbxAnimationImporter));
        using (var reader = new StreamReader(filePath))
        {
            return (FbxAnimationImporter)serializer.Deserialize(reader);
        }
    }


    private void GenerateXMLTemplate()
    {
        FbxAnimationImporter myFbxAnimationImporter = new FbxAnimationImporter();

        myFbxAnimationImporter.EnemyName = "GenericEnemyName";
        myFbxAnimationImporter.FbxName = "Y Bot@Breathing Idle.fbx";

       foreach (AnimationClip animationClip in myFbxAnimationImporter.AnimationClipList)
        {
            
        }

        // Melee later

        

        var serializer = new XmlSerializer(typeof(SpriteSheetImporter));
        string path = "./assets/File.xml";

        var stream = new FileStream(path, FileMode.Create);
        serializer.Serialize(stream, mySpriteSheetImporter);
        stream.Close();

    }
}





[XmlRoot("FbxAnimationImporter")]
public class FbxAnimationImporter
{    
    [XmlAttribute("EnemyName")]
    public string EnemyName;
    [XmlAttribute("FbxName")]
    public string FbxName;
    [XmlArray("Parameters"), XmlArrayItem("Parameter")]
    public List<Parameter> Parameters;
    [XmlArray("States"), XmlArrayItem("State")]
    public List<State> States;
    [XmlAttribute("AnimationClipList")]
    public List<AnimationClip> AnimationClipList;
}

// public class Parameter
// {
//     [XmlAttribute("Name")]
//     public string Name;
//     [XmlAttribute("Type")]
//     public string Type;
// }

// public class State
// {
//     [XmlAttribute("Name")]
//     public string Name;
//     public List<Transition> Transitions;
// }

// public class Transition
// {
//     [XmlAttribute("To")]
//     public string To;
//     [XmlAttribute("Condition")]
//     public string Condition;
// }

