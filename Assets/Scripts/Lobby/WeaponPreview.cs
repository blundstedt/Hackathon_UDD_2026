using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Hackaton.Lobby
{
    [RequireComponent(typeof(RawImage))]
    public sealed class WeaponPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private WeaponOffer offer;
        private int slot;
        private bool hovered;
        private float speed;
        private GameObject stage;
        private Transform turntable;
        private Camera camera3D;
        private RenderTexture texture;

        internal void Configure(WeaponOffer data, int index) { offer = data; slot = index; }
        public void OnPointerEnter(PointerEventData data) => hovered = true;
        public void OnPointerExit(PointerEventData data) => hovered = false;

        private void LateUpdate()
        {
            if (offer == null || !offer.previewModel) return;
            if (!stage) { Build(); camera3D.Render(); }
            speed = Mathf.MoveTowards(speed, hovered ? 38f : 0f, 130f * Time.unscaledDeltaTime);
            if (speed == 0) return;
            turntable.Rotate(Vector3.up, speed * Time.unscaledDeltaTime, Space.World);
            camera3D.Render();
        }

        private void Build()
        {
            stage = new GameObject("Preview · " + offer.displayName);
            stage.transform.position = new Vector3(3000 + slot * 12, -2000, 3000);
            turntable = new GameObject("Modelo").transform;
            turntable.SetParent(stage.transform, false);
            var model = Instantiate(offer.previewModel, turntable);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0, offer.previewYaw, 0) * model.transform.localRotation;
            const int previewLayer = 5; // UI. All preview geometry is outside the playable world.
            var parts = model.GetComponentsInChildren<Renderer>();
            var bounds = parts[0].bounds;
            foreach (var part in parts)
            {
                part.gameObject.layer = previewLayer;
                part.shadowCastingMode = ShadowCastingMode.Off;
                part.receiveShadows = false;
                if (offer.previewMaterial) part.sharedMaterial = offer.previewMaterial;
                bounds.Encapsulate(part.bounds);
            }
            model.transform.position += turntable.position - bounds.center;
            turntable.localScale = Vector3.one / Mathf.Max(.001f, bounds.size.magnitude);
            camera3D = new GameObject("Cámara").AddComponent<Camera>();
            camera3D.enabled = false;
            camera3D.transform.SetParent(stage.transform, false);
            camera3D.transform.localPosition = new Vector3(0, .38f, -2);
            camera3D.transform.LookAt(turntable.position);
            camera3D.orthographic = true;
            camera3D.orthographicSize = .59f;
            camera3D.nearClipPlane = .1f;
            camera3D.farClipPlane = 4f;
            camera3D.cullingMask = 1 << previewLayer;
            camera3D.clearFlags = CameraClearFlags.SolidColor;
            camera3D.backgroundColor = new Color32(42, 32, 54, 255);
            camera3D.allowHDR = false;
            var light = new GameObject("Luz de vitrina").AddComponent<Light>();
            light.transform.SetParent(stage.transform, false);
            light.transform.localPosition = new Vector3(-.6f, .8f, -.9f);
            light.type = LightType.Point;
            light.range = 3;
            light.intensity = 3;
            light.cullingMask = 1 << previewLayer;
            texture = new RenderTexture(480, 288, 16) { name = offer.id, antiAliasing = 2 };
            camera3D.targetTexture = texture;
            GetComponent<RawImage>().texture = texture;
        }

        private void OnDisable()
        {
            hovered = false;
            speed = 0;
            if (stage) Destroy(stage);
            if (texture)
            {
                camera3D.targetTexture = null;
                GetComponent<RawImage>().texture = null;
                texture.Release();
                Destroy(texture);
            }
        }
    }
}
