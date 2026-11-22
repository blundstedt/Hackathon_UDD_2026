using UnityEngine;

[ExecuteAlways]
public sealed class TowerFloorPanel : MonoBehaviour
{
    [SerializeField, Min(0)] private int highestUnlockedFloor = 1;
    [SerializeField] private Renderer[] digits;
    [SerializeField] private Material illuminated;
    [SerializeField] private Material inactive;

    // The progression system can call this after loading or completing a level.
    public void SetHighestUnlockedFloor(int floor)
    {
        highestUnlockedFloor = Mathf.Clamp(floor, 0, Mathf.Max(0, (digits?.Length ?? 0) - 1));
        Refresh();
    }

    private void OnEnable() => Refresh();

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += RefreshIfLoaded;
#endif
    }

#if UNITY_EDITOR
    private void RefreshIfLoaded()
    {
        if (this && gameObject.scene.IsValid())
            Refresh();
    }
#endif

    private void Refresh()
    {
        if (digits == null || !illuminated || !inactive)
            return;

        for (int i = 0; i < digits.Length; i++)
        {
            if (digits[i])
                digits[i].sharedMaterial = i <= highestUnlockedFloor ? illuminated : inactive;
        }
    }
}
