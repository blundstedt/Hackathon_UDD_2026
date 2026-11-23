using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hackaton.Multiplayer
{
    [DefaultExecutionOrder(-500)]
    public sealed class SessionMenu : MonoBehaviour
    {
        public enum Page { Home, Slots, Join, Options, Pause, Confirm, Hidden }
        [SerializeField] private CampaignRoom room;
        [SerializeField] private GameObject background;
        [SerializeField] private GameObject[] pages;
        [SerializeField] private Text[] slotInfo, slotActions;
        [SerializeField] private Button[] openSlot, deleteSlot;
        [SerializeField] private InputField joinCode;
        [SerializeField] private Text roomCode, confirmText, status, mode;
        [SerializeField] private AudioSource click;
        private Page page, previous;
        private Action confirmed;
        private bool local;
        private static bool menuOpen;
        public static bool CapturesInput => menuOpen || (CampaignRoom.Current && CampaignRoom.Current.IntroBlocking);

        private void Start()
        {
            room.Message += Message;
            SceneManager.sceneLoaded += SceneLoaded;
            LocalGameSettings.Apply(true);
            Show(Page.Home);
        }
        private void OnDestroy()
        {
            if (room) room.Message -= Message;
            SceneManager.sceneLoaded -= SceneLoaded;
        }
        private void SceneLoaded(Scene scene, LoadSceneMode load)
        {
            if (scene.name == CampaignRoom.Lobby) room.CoverIntro();
            Show(scene.name == CampaignRoom.Menu ? Page.Home : Page.Hidden);
            if (room.LastMessage != null) Message(room.LastMessage);
        }
        public void Message(string value)
        {
            status.text = value ?? "";
            status.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }
        private void Show(Page next)
        {
            page = next;
            menuOpen = next != Page.Hidden;
            background.SetActive(SceneManager.GetActiveScene().name == CampaignRoom.Menu);
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == (int)next);
            Cursor.lockState = CapturesInput ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = CapturesInput;
            Message(null);
            if (next == Page.Slots) RefreshSlots();
            if (next == Page.Pause) roomCode.text = string.IsNullOrEmpty(room.Code) ? "Partida local" : room.Code;
        }

        private void RefreshSlots()
        {
            mode.text = local ? "Modo · Sin conexión" : "Modo · En línea";
            for (int i = 0; i < 3; i++)
            {
                int slot = i + 1;
                try
                {
                    var data = room.Slots.Read(slot);
                    slotInfo[i].text = data == null ? "Vacía" : $"Piso {data.HighestFloor}   ·   {new DateTime(data.savedUtc, DateTimeKind.Utc).ToLocalTime():dd/MM/yyyy HH:mm}";
                    slotActions[i].text = data == null ? "Crear partida" : "Continuar";
                    openSlot[i].interactable = true;
                    deleteSlot[i].interactable = data != null;
                }
                catch (Exception e)
                {
                    slotInfo[i].text = e.Message;
                    openSlot[i].interactable = false;
                    deleteSlot[i].interactable = true;
                }
            }
        }
        public void Sound() { if (click && click.clip) click.PlayOneShot(click.clip, .38f); }
        public void HostPage() { if (!room.Busy) Show(Page.Slots); }
        public void JoinPage() { if (!room.Busy) { Show(Page.Join); joinCode.ActivateInputField(); } }
        public void SettingsPage() { if (!room.Busy) { previous = page; Show(Page.Options); } }
        public void ToggleLocal() { if (!room.Busy) { local = !local; RefreshSlots(); } }
        public void PlaySlot(int slot) { if (!room.Busy) room.Host(slot, local); }
        public void Join() { if (!room.Busy) room.Join(joinCode.text); }
        public void DeleteSlot(int slot)
        {
            if (room.Busy) return;
            Confirm($"¿Eliminar la partida {slot} y todo su progreso?", () =>
            {
                try { room.Slots.Delete(slot); Show(Page.Slots); }
                catch (Exception e) { Show(Page.Slots); Message(e.Message); }
            });
        }
        public void Leave() => Confirm(room.OwnsSave ? "¿Guardar y cerrar la partida para todo el grupo?" :
            "¿Desconectarte y volver al menú?", () => room.Leave());
        private void Confirm(string question, Action action)
        {
            if (room.Busy) return;
            previous = page;
            confirmed = action;
            confirmText.text = question;
            Show(Page.Confirm);
        }
        public void Accept()
        {
            if (room.Busy) return;
            var action = confirmed;
            confirmed = null;
            action?.Invoke();
        }
        public void CopyCode()
        {
            if (string.IsNullOrEmpty(room.Code)) { Message("Estás jugando sin conexión."); return; }
            GUIUtility.systemCopyBuffer = room.Code;
            Message("Código copiado");
        }
        public void Resume() { if (!room.Busy) Show(Page.Hidden); }
        public void Back()
        {
            if (room.Busy) return;
            confirmed = null;
            Show(page == Page.Options || page == Page.Confirm ? previous : Page.Home);
        }
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void Update()
        {
            if (room.IntroBlocking || room.Busy || Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            if (page == Page.Hidden) Show(Page.Pause);
            else if (page == Page.Pause) Resume();
            else if (page != Page.Home) Back();
        }
    }
}
