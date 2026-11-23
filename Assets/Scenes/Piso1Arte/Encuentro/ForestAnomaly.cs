using UnityEngine;

namespace Hackaton.Levels
{
    public sealed class ForestAnomaly : MonoBehaviour
    {
        private Vector3 rest;
        private float born;

        private void Awake() => rest=transform.localPosition;
        private void OnEnable() => born=Time.time;
        private void Update()
        {
            float t=Time.time-born;
            float shock=Mathf.Max(0,1-t/1.5f);
            transform.localPosition=rest+new Vector3(.14f*shock*Mathf.Sin(t*52),.09f*Mathf.Sin(t*2),.08f*shock*Mathf.Cos(t*58));
            transform.localScale=Vector3.one*(1+.018f*Mathf.Sin(t*3.6f));
        }
    }
}

