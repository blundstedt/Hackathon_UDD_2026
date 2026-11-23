using UnityEngine;

namespace Hackaton.Multiplayer
{
    [DefaultExecutionOrder(-900)]
    public sealed class SceneSoloAvatar : MonoBehaviour
    {
        private void Awake()
        {
            if (CampaignRoom.Current && CampaignRoom.Current.IsOnline) gameObject.SetActive(false);
        }
    }
}
