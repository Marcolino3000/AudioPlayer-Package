using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Editor.AudioEditor
{
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
            return marker.Id;
        }

        public void RemoveMarker(AudioClip clip, int id)
        {
            if (clipsToMarkers.TryGetValue(clip, out var markers))
            {
                markers.RemoveAll(m => m.Id == id);
            }
        }
        
        public void RemoveMarkerBySample(AudioClip clip, int sample)
        {
            if (clipsToMarkers.TryGetValue(clip, out var markers))
            {
                markers.RemoveAll(m => m.Sample == sample);
            }
        }

        public List<int> GetMarkerPositions(AudioClip clip)
        {
            if (clipsToMarkers.TryGetValue(clip, out var markers))
                return markers.Select(m => m.Sample).ToList();
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
                        OnMarkerReached?.Invoke(marker.Expression);
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
        
        public enum MarkerType
        {
            Paragraph,
            Neutral,
            Happy,
            Annoyed
        }
        
        [Serializable]
        public class Marker
        {
            public readonly int Id;
            public readonly int Sample;
            public MarkerType Expression = MarkerType.Paragraph;
            public Marker(int id, int sample)
            {
                Id = id;
                Sample = sample;
            }
        }
    }
    

}
