// 거점 안의 상호작용 지점(건물·상자·숲·게이트·나무). 플레이어가 트리거 안에 들어오면 UI가 상호작용 버튼을 띄운다.
using System.Collections.Generic;
using UnityEngine;

public enum LobbySiteKind { Building, IdleChest, Research, Training, ForestAssign, Tree, Gate }

[RequireComponent(typeof(Collider))]
public class LobbySite : MonoBehaviour
{
    [SerializeField] private LobbySiteKind kind;
    [Tooltip("kind가 Building일 때 BuildingDefinition.id")]
    [SerializeField] private string buildingId;
    [SerializeField] private string displayName;
    [Tooltip("이름표 높이(월드 단위)")]
    [SerializeField] private float labelHeight = 2.2f;

    public LobbySiteKind Kind => kind;
    public string BuildingId => buildingId;
    public string DisplayName => displayName;
    public Vector3 LabelPosition => transform.position + Vector3.up * labelHeight;

    // 씬의 모든 지점(이름표용)과 플레이어가 안에 있는 지점.
    public static readonly List<LobbySite> All = new List<LobbySite>();
    public static readonly List<LobbySite> PlayerInside = new List<LobbySite>();

    public void Setup(LobbySiteKind kind, string buildingId, string displayName, float labelHeight)
    {
        this.kind = kind;
        this.buildingId = buildingId;
        this.displayName = displayName;
        this.labelHeight = labelHeight;
    }

    private void OnEnable() => All.Add(this);

    private void OnDisable()
    {
        All.Remove(this);
        PlayerInside.Remove(this);
    }

    // 파티원은 Player의 자식 콜라이더라 GetComponentInParent로 Player를 찾는다.
    private static bool IsPlayer(Collider other) => other.GetComponentInParent<PlayerMovement>() != null;

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other) && !PlayerInside.Contains(this))
            PlayerInside.Add(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
            PlayerInside.Remove(this);
    }
}
