// 직접 벌목할 수 있는 나무. 정해진 횟수만큼 치면 쓰러지고, 시간이 지나면 다시 자란다(저장하지 않음).
using UnityEngine;

public class LobbyTree : MonoBehaviour
{
    [Tooltip("쓰러지면 숨길 겉모습(자리표시 도형)")]
    [SerializeField] private GameObject visual;

    private int hitsLeft;
    private float regrowAt;

    public bool IsFelled => hitsLeft <= 0;
    public int HitsLeft => hitsLeft;
    public float RegrowRemaining => Mathf.Max(0f, regrowAt - Time.time);

    private ForestSettings Settings => LobbyManager.Instance != null ? LobbyManager.Instance.Config.forest : null;

    private void Start()
    {
        hitsLeft = Settings?.hitsPerTree ?? 3;
    }

    private void Update()
    {
        if (IsFelled && Time.time >= regrowAt)
        {
            hitsLeft = Settings?.hitsPerTree ?? 3;
            SetVisible(true);
        }
    }

    public bool TryHit()
    {
        if (IsFelled)
            return false;

        hitsLeft--;
        if (IsFelled)
        {
            regrowAt = Time.time + (Settings?.regrowSeconds ?? 30f);
            SetVisible(false);
        }
        return true;
    }

    private void SetVisible(bool visible)
    {
        if (visual != null)
            visual.SetActive(visible);
    }

    public void SetVisual(GameObject target) => visual = target;
}
