using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "AudioEditor/MarkerManager")]
public class MarkerManager : SerializedScriptableObject
{
    public event Action<MarkerType> OnMarkerReached;
        
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

    public List<int> GetMarkerPositions(AudioClip clip, MarkerType type)
    {
        if (clipsToMarkers.TryGetValue(clip, out var markers))
            return markers.Where(m => m.Type == type).Select(m => m.Sample).ToList();
        return new List<int>();
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
                    OnMarkerReached?.Invoke(marker.Type);
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
        var paragraphPositions = GetMarkerPositions(clip, MarkerType.Paragraph).OrderBy(s => s).ToList();

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

    public enum MarkerType
    {
        Paragraph,
        Neutral,
        Angry,
        Annoyed,
        Happy,
        Moved,
        Sad,
        Sarcastic,
        Skeptical,
        Smirky,
        Thoughtful
    }
        
    [Serializable]
    public class Marker
    {
        public readonly int Id;
        public readonly int Sample;
        public MarkerType Type = MarkerType.Paragraph;
        public Marker(int id, int sample)
        {
            Id = id;
            Sample = sample;
        }
    }
}