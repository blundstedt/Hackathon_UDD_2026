using UnityEngine;

namespace Hackaton.Levels
{
    internal static class ForestRune
    {
        public static LineRenderer Create(Transform parent, Vector3 centre, float radius, Material material, Color tint)
        {
            var node = new GameObject("Círculo de invocación");
            node.transform.SetParent(parent,false);
            var ring = node.AddComponent<LineRenderer>();
            ring.sharedMaterial = material;
            ring.startColor = ring.endColor = tint;
            ring.loop = true;
            ring.widthMultiplier = .075f;
            ring.positionCount = 32;
            for (int i=0; i<ring.positionCount; i++)
            {
                float a=2*Mathf.PI*i/ring.positionCount;
                Vector3 p=centre+new Vector3(Mathf.Cos(a)*radius,.07f,Mathf.Sin(a)*radius);
                if (UnityEngine.AI.NavMesh.SamplePosition(p,out var floor,.9f,UnityEngine.AI.NavMesh.AllAreas))
                    p.y=floor.position.y+.09f;
                ring.SetPosition(i,p);
            }
            return ring;
        }
    }
}

