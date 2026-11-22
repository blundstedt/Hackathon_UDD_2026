using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Progression
{
    /// <summary>Presentación persistente de la espera, la interacción y el fundido entre escenas.</summary>
    public sealed class FloorTravelScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup curtain;
        [SerializeField] private Text prompt;
        [SerializeField, Min(0.01f)] private float fadeSeconds = 0.22f;
        private string feedback, waiting;
        private float feedbackUntil;

        public void Notice(string message)
        {
            feedback = message;
            feedbackUntil = Time.unscaledTime + 2.4f;
        }

        public void WaitMessage(string message) => waiting = message;
        public void Aim(string hint)
        {
            string value = waiting ?? (Time.unscaledTime < feedbackUntil ? feedback : hint);
            if (prompt.text != value) prompt.text = value ?? "";
        }

        private IEnumerator Fade(float target)
        {
            while (!Mathf.Approximately(curtain.alpha, target))
            {
                curtain.alpha = Mathf.MoveTowards(curtain.alpha, target, Time.unscaledDeltaTime / fadeSeconds);
                yield return null;
            }
        }

        internal IEnumerator Cover()
        {
            feedbackUntil = 0;
            waiting = null;
            prompt.text = "";
            curtain.gameObject.SetActive(true);
            yield return Fade(1);
            yield return null;
        }

        internal IEnumerator Reveal()
        {
            yield return Fade(0);
            curtain.gameObject.SetActive(false);
        }
    }
}
