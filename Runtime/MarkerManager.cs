using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "AudioEditor/MarkerManager")]
public class MarkerManager : SerializedScriptableObject
{
    public const int ParagraphState = 0;

    public event Action<int, string> OnMarkerReached;

    [Header("Settings")]
    public List<string> CharacterNames;

    [Tooltip("Je Figur die Ausdrücke, die im Marker zur Auswahl stehen. State ist der Wert, " +
             "der an den expState-Parameter im Animator geht.")]
    public Dictionary<string, List<Expression>> CharacterExpressions = DefaultCharacterExpressions();

    [Header("Debug")]
    public int lastPlayheadSample = -1;
    public Dictionary<AudioClip, List<Marker>> clipsToMarkers = new();
    public int nextId = 1;

    public int AddMarker(AudioClip clip, int sample)
    {
        if (!clipsToMarkers.TryGetValue(clip, out var markers))
        {
            markers = new List<Marker>();
            clipsToMarkers[clip] = markers;
        }
        var marker = new Marker(nextId++, sample);
        markers.Add(marker);
    #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
    #endif
        return marker.Id;
    }

    public void RemoveMarker(AudioClip clip, int id)
    {
        if (clipsToMarkers.TryGetValue(clip, out var markers))
        {
            markers.RemoveAll(m => m.Id == id);
        }
    #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
    #endif
    }
        
    public void RemoveMarkerBySample(AudioClip clip, int sample)
    {
        if (clipsToMarkers.TryGetValue(clip, out var markers))
        {
            markers.RemoveAll(m => m.Sample == sample);
        }
    #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
    #endif
    }

    public List<int> GetMarkerPositions(AudioClip clip, int state)
    {
        if (clipsToMarkers.TryGetValue(clip, out var markers))
            return markers.Where(m => m.Type == state).Select(m => m.Sample).ToList();
        return new List<int>();
    }

    /// <summary>
    /// Die Ausdrücke, die für diese Figur zur Auswahl stehen. Leere Liste, wenn die Figur
    /// nicht eingetragen ist - dann bleibt nur der bereits gesetzte Wert des Markers.
    /// </summary>
    public List<Expression> GetExpressions(string characterName)
    {
        if (characterName != null
            && CharacterExpressions != null
            && CharacterExpressions.TryGetValue(characterName, out var expressions)
            && expressions != null)
            return expressions;

        return new List<Expression>();
    }

    [Button("Standard-Ausdrücke wiederherstellen")]
    public void RestoreDefaultExpressions()
    {
        CharacterExpressions = DefaultCharacterExpressions();
    #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
    #endif
    }

    private static Dictionary<string, List<Expression>> DefaultCharacterExpressions()
    {
        return new Dictionary<string, List<Expression>>
        {
            ["Marlene"] = new()
            {
                new Expression("Paragraph", ParagraphState),
                new Expression("neutral", 1),
                new Expression("angry", 2),
                new Expression("annoyed", 3),
                new Expression("happy", 4),
                new Expression("moved", 5),
                new Expression("sad", 6),
                new Expression("sarcasm", 7),
                new Expression("skeptical", 8),
                new Expression("smirky", 9),
                new Expression("thoughtful", 10),
            },
            ["Hilde"] = new()
            {
                new Expression("Neutral", 1),
                new Expression("critical", 2),
                new Expression("curious", 3),
                new Expression("laughing", 4),
                new Expression("sad", 5),
                new Expression("sigh", 6),
                new Expression("smile", 7),
                new Expression("sartled", 8),
            },
            ["Paul"] = new()
            {
                new Expression("Neutral", 1),
                new Expression("angry", 2),
                new Expression("awkward", 3),
                new Expression("critical", 4),
                new Expression("sad", 5),
                new Expression("smirk", 6),
                new Expression("sulky", 7),
                new Expression("wink", 8),
            },
        };
    }

    public List<Marker> GetMarkers(AudioClip clip)
    {
        if (clipsToMarkers.TryGetValue(clip, out var markers))
            return markers;
            
        return new List<Marker>();
    }

    public void CheckPlayhead(AudioClip clip, int playheadSample)
    {
        if (clipsToMarkers.TryGetValue(clip, out var markers))
        {
            foreach (var marker in markers)
            {
                if (lastPlayheadSample < marker.Sample && playheadSample >= marker.Sample)
                {
                    OnMarkerReached?.Invoke(marker.Type, marker.CharacterToAnimate);
                    // Debug.Log("Marker ID reached: " + marker.Id);
                }
            }
        }
        lastPlayheadSample = playheadSample;
    }

    public void ResetPlayheadCheck()
    {
        lastPlayheadSample = -1;
    }

    public bool ExistsMarkerAtSample(AudioClip clip, int sample)
    {
        if (clipsToMarkers.TryGetValue(clip, out var markers))
        {
            var marker = markers.Find(m => m.Sample == sample);
            return marker != null;
        }
        return false;
    }

    public List<float> GetParagraphMarkerTimespans(AudioClip clip)
    {
        var result = new List<float>();
        if (clip == null)
        {
            Debug.LogWarning("Tried to get Paragraph markers but audio clip was null! Returning empty list");
            return result;
        }
        var paragraphPositions = GetMarkerPositions(clip, ParagraphState).OrderBy(s => s).ToList();

        if (paragraphPositions.Count == 0)
        {
            result.Add(clip.length);
            return result;
        }
        
        int sampleCount = clip.samples;
        float frequency = clip.frequency;
        int currentSample = 0;
        
        for (int i = 0; i < paragraphPositions.Count; i++)
        {
            int nextSample = paragraphPositions[i];
            AddSecondsUntilNextToList(nextSample, currentSample, frequency, result);
            currentSample = nextSample;
        }
        
        AddSecondsUntilNextToList(sampleCount, currentSample, frequency, result);
        
        return result;
    }

    private void AddSecondsUntilNextToList(int nextSample, int currentSample, float frequency, List<float> result)
    {
        float secondsUntilNext = (nextSample - currentSample) / frequency;
        result.Add(secondsUntilNext);
    }

    /// <summary>
    /// Ein auswählbarer Ausdruck einer Figur. <see cref="State"/> ist der Wert, der an den
    /// expState-Parameter im Animator der Figur geht - <see cref="ParagraphState"/> bedeutet
    /// "keine Mimik, nur Absatzmarke".
    /// </summary>
    [Serializable]
    public class Expression
    {
        public string Name;
        public int State;

        public Expression() { }

        public Expression(string name, int state)
        {
            Name = name;
            State = state;
        }
    }

    [Serializable]
    public class Marker
    {
        public readonly int Id;
        public readonly int Sample;
        public int Type = ParagraphState;
        public string CharacterToAnimate;
        public Marker(int id, int sample)
        {
            Id = id;
            Sample = sample;
        }
    }
}