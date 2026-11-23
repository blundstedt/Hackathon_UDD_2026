using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Multiplayer
{
    public sealed class OptionsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject[] categories;
        [SerializeField] private Text[] values;
        public void Category(int index)
        {
            for (int i = 0; i < categories.Length; i++) categories[i].SetActive(i == index);
        }
        // Positive indices increase the value, negative indices decrease it (one based).
        public void Step(int command) => LocalGameSettings.Step((LocalGameSettings.Setting)(Mathf.Abs(command) - 1), System.Math.Sign(command));
        private void OnEnable()
        {
            LocalGameSettings.Changed += Refresh;
            Refresh();
        }
        private void OnDisable() => LocalGameSettings.Changed -= Refresh;
        private void Refresh()
        {
            for (int i = 0; i < values.Length; i++) values[i].text = LocalGameSettings.Label((LocalGameSettings.Setting)i);
        }
    }
}
