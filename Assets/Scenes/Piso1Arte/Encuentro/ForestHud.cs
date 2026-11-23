using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Levels
{
    public sealed class ForestHud : MonoBehaviour
    {
        [SerializeField] private Font font;
        private Text title, subtitle, remaining, bossTitle, marker;
        private Image bossFill;
        private RectTransform bossTrack;
        private ForestEnemy boss;
        private Transform chest;
        private int lastHealth = -1, lastDistance = -1;

        private void Awake()
        {
            var canvas = new GameObject("HUD del claro",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            canvas.transform.SetParent(transform,false);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720);
            var corner=new Vector2(0,1);
            var left = Panel(canvas.transform,"Oleadas",corner,new Vector2(18,-16),new Vector2(350,87),new Color(.04f,.08f,.09f,.84f));
            title=Label(left,"Oleada",new Vector2(12,-2),26);
            subtitle=Label(left,"Objetivo",new Vector2(12,-34),17);
            remaining=Label(left,"Restantes",new Vector2(12,-56),17);
            remaining.color=new Color(.66f,1,.7f);
            bossTrack=Panel(canvas.transform,"Vida del jefe",new Vector2(.5f,1),new Vector2(0,-20),new Vector2(380,36),new Color(.07f,.06f,.1f,.9f));
            var bar=Panel(bossTrack,"Salud",Vector2.zero,Vector2.zero,new Vector2(380,6),new Color(.65f,.95f,.28f));
            bossFill=bar.GetComponent<Image>();
            bossTitle=Label(bossTrack,"Coloso",new Vector2(9,0),21);
            bossTrack.gameObject.SetActive(false);
            marker=Label(left,"Cofre",new Vector2(12,-91),19);
            marker.color=new Color(.67f,1,.89f);
        }

        private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));
            var rect=(RectTransform)go.transform;
            rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=anchor;
            rect.sizeDelta=size;
            rect.anchoredPosition=pos;
            var image=go.GetComponent<Image>();
            image.color=color;
            image.raycastTarget=false;
            return rect;
        }

        private Text Label(RectTransform parent,string name,Vector2 pos,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Outline));
            var rect=(RectTransform)go.transform;
            rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.sizeDelta=new Vector2(parent.sizeDelta.x-12,size+12);
            rect.anchoredPosition=pos;
            var text=go.GetComponent<Text>();
            text.font=font;
            text.fontSize=size;
            text.verticalOverflow=VerticalWrapMode.Overflow;
            text.alignment=TextAnchor.MiddleLeft;
            text.raycastTarget=false;
            text.text="";
            return text;
        }

        public void SetStatus(string heading,string detail) { title.text=heading; subtitle.text=detail; }
        public void SetRemaining(int count) => remaining.text=count>0 ? $"Enemigos restantes   {count}" : "";
        public void TrackBoss(ForestEnemy enemy) { boss=enemy; bossTrack.gameObject.SetActive(true); }
        public void MarkChest(Transform target) => chest=target;
        public void Completed()
        {
            chest=null;
            marker.text="";
            SetStatus("Piso 1 completado","Vuelve al lobby y entrega la llave");
        }
        private void LateUpdate()
        {
            if (boss && boss.Health!=lastHealth)
            {
                lastHealth=boss.Health;
                bossFill.rectTransform.sizeDelta=new Vector2(380f*boss.Health/boss.FullHealth,6);
                bossTitle.text=$"Coloso de Raíces    {boss.Health} / {boss.FullHealth}";
                bossTrack.gameObject.SetActive(boss.Health>0);
            }
            if (chest && Camera.main is Camera cam)
            {
                int meters=Mathf.RoundToInt(Vector3.Distance(cam.transform.position,chest.position));
                if (meters!=lastDistance) { marker.text=$"Cofre del claro   ·   {meters} m"; lastDistance=meters; }
            }
        }
    }
}

