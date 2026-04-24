using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewGhostData", menuName = "Racing/GhostData")]
public class GhostData : ScriptableObject
{
    [System.Serializable]
    public struct GhostFrame
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public List<GhostFrame> frames = new List<GhostFrame>();

    public void Clear() => frames.Clear();
    public void AddFrame(Vector3 pos, Quaternion rot) => frames.Add(new GhostFrame { position = pos, rotation = rot });
}