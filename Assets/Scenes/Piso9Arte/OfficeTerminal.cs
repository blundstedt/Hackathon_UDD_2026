using Hackaton.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Levels
{
    public sealed class OfficeTerminal : FloorInteraction
    {
        OfficeDirector director;
        Text status, heading;
        Image bar;
        Transform leftDoor, rightDoor;
        Vector3 leftClosed, rightClosed;
        float opening;
        Text wallText;

        public void Install(OfficeDirector owner, Font type, Vector3 entry)
        {
            director = owner;
            leftClosed = entry + new Vector3(-1.48f,2.17f,0);
            rightClosed = entry + new Vector3(1.48f,2.17f,0);
            leftDoor = owner.Fx.Block("Hoja izquierda",leftClosed,new Vector3(2.96f,4.34f,.18f),new Color(.12f,.19f,.22f),true);
            rightDoor = owner.Fx.Block("Hoja derecha",rightClosed,new Vector3(2.96f,4.34f,.18f),new Color(.12f,.19f,.22f),true);
            var wall = new GameObject("Pantalla de mantenimiento",typeof(RectTransform),typeof(Canvas));
            wall.transform.SetParent(owner.transform,false);
            wall.transform.position = transform.position + Vector3.back * .25f;
            wall.transform.rotation = Quaternion.identity;
            wall.transform.localScale = Vector3.one * .007f;
            wall.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            wallText = Label(wall.transform,type,22,TextAnchor.MiddleCenter);
            Rect(wallText.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(225,95));

            var panel = new GameObject("Estado del turno",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            panel.transform.SetParent(owner.transform,false);
            var canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1;
            var scale = panel.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1280,720);
            scale.matchWidthOrHeight = .5f;
            heading = Label(panel.transform,type,18,TextAnchor.MiddleCenter);
            status = Label(panel.transform,type,24,TextAnchor.MiddleCenter);
            Rect(heading.rectTransform,new Vector2(.5f,1),new Vector2(0,-22),new Vector2(660,32));
            Rect(status.rectTransform,new Vector2(.5f,1),new Vector2(0,-52),new Vector2(880,46));
            heading.color = new Color(1,.62f,.72f);
            var rail = new GameObject("Integridad",typeof(RectTransform),typeof(Image));
            rail.transform.SetParent(panel.transform,false);
            Rect((RectTransform)rail.transform,new Vector2(.5f,1),new Vector2(0,-80),new Vector2(360,7));
            rail.GetComponent<Image>().color = new Color(.12f,.07f,.12f,.88f);
            var fill = new GameObject("Carga",typeof(RectTransform),typeof(Image));
            fill.transform.SetParent(rail.transform,false);
            bar = fill.GetComponent<Image>();
            bar.color = OfficeEffects.Magenta;
            bar.rectTransform.anchorMin = Vector2.zero;
            bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
        }

        static Text Label(Transform parent, Font type, int points, TextAnchor alignment)
        {
            var go = new GameObject("Indicador",typeof(RectTransform),typeof(Text),typeof(Shadow));
            go.transform.SetParent(parent,false);
            var label = go.GetComponent<Text>();
            label.font = type;
            label.fontSize = points;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.color = Color.white;
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(.025f,.01f,.04f,.95f);
            shadow.effectDistance = new Vector2(1.5f,-1.5f);
            return label;
        }

        static void Rect(RectTransform rect, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        public void Show(string message, int progress, OfficeActor boss)
        {
            heading.text = boss ? "EL CORRUPTOR   /   UNIDAD RECONSTRUIDA" : "PLANTA 09   /   TURNO DE NOCHE";
            status.text = message;
            wallText.text = "ASCENSOR 09\n" + progress + "%";
            bar.rectTransform.anchorMax = new Vector2(boss ? (float)boss.Health / boss.MaximumHealth : progress / 100f,1);
            bar.rectTransform.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (!director || director.Phase != OfficeDirector.Stage.Completed || opening >= 1) return;
            opening = Mathf.MoveTowards(opening,1,Time.deltaTime);
            leftDoor.position = leftClosed + Vector3.left * (3 * opening);
            rightDoor.position = rightClosed + Vector3.right * (3 * opening);
        }

        public override string Prompt(FloorSession session) => director.Phase == OfficeDirector.Stage.Preparation ?
            "[E] Reactivar el ascensor" : "Terminal de mantenimiento";
        public override string Interact(FloorPlayer player) => director.Activate(player);
    }
}
