using System.Collections;
using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Levels
{
    public sealed class ForestChest : FloorInteraction
    {
        [SerializeField] private ForestArena arena;
        [SerializeField] private Transform lid;
        private bool opened;
        public override string Prompt(FloorSession session) => opened ? "Regresa al lobby por el ascensor" :
            arena.CanClaim ? "[E] Abrir el cofre del claro" : "Derrota a los enemigos restantes";
        public override string Interact(FloorPlayer player)
        {
            var session=FloorSession.Instance;
            if (opened || !arena.CanClaim) return Prompt(session);
            var result=session.CompleteCurrent(player);
            if (session.Progress.State(session.Current.number)==FloorState.Completed)
            {
                opened=true;
                StartCoroutine(Open());
                arena.GetComponent<ForestHud>().Completed();
            }
            return result;
        }
        private IEnumerator Open()
        {
            for (float t=0;t<1;t+=Time.deltaTime*1.8f)
            {
                lid.localRotation=Quaternion.Euler(-96*Mathf.SmoothStep(0,1,t),0,0);
                yield return null;
            }
            lid.localRotation=Quaternion.Euler(-96,0,0);
        }
    }
}

