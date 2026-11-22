using Hackaton.Player;
using UnityEngine;

namespace Hackaton.Progression
{
    /// <summary>Entrada del piso para un jugador local, con su equipamiento de la partida.</summary>
    public sealed class FloorSpawn : MonoBehaviour
    {
        [SerializeField] private PlayerView defaultAvatar;
        [SerializeField] private string playerId = "local";

        private void Start()
        {
            var equipped = FloorSession.Instance.Equipment(playerId);
            var staging = new GameObject("Aparición");
            staging.SetActive(false);
            var avatar = Instantiate(equipped ? equipped : defaultAvatar, staging.transform);
            avatar.GetComponent<FloorPlayer>().SetIdentity(playerId);
            avatar.transform.SetPositionAndRotation(transform.position, transform.rotation);
            avatar.transform.SetParent(null, true);
            Destroy(staging);
        }
    }
}
