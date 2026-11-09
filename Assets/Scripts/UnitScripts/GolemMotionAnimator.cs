// 골렘의 스프라이트 애니메이션(대기·걷기·공격·피격)을 바라보는 방향에 맞춰 재생한다.
// 방향은 7개(정면, 앞쪽 대각선 ±45°, 옆 ±90°, 뒤쪽 대각선 ±135°)이고, 뒷모습(180°)은 뒤쪽 대각선으로 대신한다.
// Animator 상태 이름은 "<모션>_<방향>"이다(에디터 메뉴 Tools/Wild Golem/우편함 골렘 스프라이트 가져오기가 만든다).
// 아직 납품되지 않은 방향은 가까운 방향으로 대신 재생한다.
// 대기·걷기 상태가 하나도 없는 골렘(아직 가져오지 않은 골렘)은 기존 isMoving·isLeft 전환을 그대로 쓴다.
using System.Collections.Generic;
using UnityEngine;

public class GolemMotionAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    // 임시값: 납품 시트의 left/right가 화면 기준과 반대로 보이면 켠다. Play 모드에서 확인 후 정한다.
    [SerializeField] private bool swapLeftRight = false;

    // 위치 변화가 없는 프레임이 이 시간보다 길어야 멈춘 것으로 본다.
    // Rigidbody.MovePosition은 물리 단계에서 반영되어 매 프레임 위치가 바뀌지 않기 때문이다(PartyController의 animationStopDelay와 같은 값).
    [SerializeField] private float stopDelay = 0.1f;

    private const string IdleMotion = "idle";
    private const string WalkMotion = "walk";
    private const string AttackMotion = "attack";
    private const string HitMotion = "hit";

    // 각도 구간(45° 단위) → 방향 이름. 각도 0은 카메라 쪽(−Z), +는 화면 오른쪽(+X)이다.
    private static readonly Dictionary<int, string> DirectionBySector = new Dictionary<int, string>
    {
        { 0, "front" },
        { 1, "right" },
        { 2, "side_right" },
        { 3, "back_right" },
        { 4, "back_right" },
        { -1, "left" },
        { -2, "side_left" },
        { -3, "back_left" },
        { -4, "back_left" },
    };

    // 해당 방향 상태가 없을 때 차례로 시도할 방향
    private static readonly Dictionary<string, string[]> Fallbacks = new Dictionary<string, string[]>
    {
        { "front", new[] { "front" } },
        { "left", new[] { "left", "front" } },
        { "right", new[] { "right", "front" } },
        { "side_left", new[] { "side_left", "left", "front" } },
        { "side_right", new[] { "side_right", "right", "front" } },
        { "back_left", new[] { "back_left", "side_left", "left", "front" } },
        { "back_right", new[] { "back_right", "side_right", "right", "front" } },
    };

    private static readonly Dictionary<string, int> StateHashes = new Dictionary<string, int>();
    private static readonly HashSet<int> OneShotHashes = new HashSet<int>();
    private static readonly HashSet<int> WalkHashes = new HashSet<int>();

    static GolemMotionAnimator()
    {
        foreach (string direction in Fallbacks.Keys)
        {
            OneShotHashes.Add(StateHash($"{AttackMotion}_{direction}"));
            OneShotHashes.Add(StateHash($"{HitMotion}_{direction}"));
            WalkHashes.Add(StateHash($"{WalkMotion}_{direction}"));
        }
    }

    private Vector3 lastFacing = Vector3.back;
    private Vector3 previousPosition;
    private float lastMoveTime = float.NegativeInfinity;

    // 자신 또는 부모(파티원은 Player의 자식)에 있는 PlayerMovement. 있으면 입력 기준 이동 방향을 쓴다.
    private PlayerMovement mover;
    private bool moverResolved;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        previousPosition = transform.position;
    }

    // 파티 편성(PartyController)이 실행 중에 Player 아래로 옮기므로 부모가 바뀌면 다시 찾는다.
    private void OnTransformParentChanged() => moverResolved = false;

    private void LateUpdate()
    {
        if (!moverResolved)
        {
            mover = GetComponentInParent<PlayerMovement>();
            moverResolved = true;
        }

        bool isMoving;

        if (mover != null)
        {
            // 벽에 막혀 밀려나는 움직임에 방향이 흔들리지 않도록, 실제 위치 변화가 아니라 입력 기준 이동 방향을 쓴다.
            isMoving = mover.IsMoving;

            if (isMoving)
                lastFacing = mover.LastMoveDirection;
        }
        else
        {
            // Player와 무관하게 움직이는 골렘은 위치 변화로 판단한다.
            Vector3 movement = transform.position - previousPosition;
            movement.y = 0f;

            if (movement.sqrMagnitude > 0.000001f)
            {
                lastFacing = movement.normalized;
                lastMoveTime = Time.time;
            }

            isMoving = Time.time - lastMoveTime <= stopDelay;
        }

        previousPosition = transform.position;
        UpdateLocomotion(isMoving);
    }

    public void PlayAttack() => Play(AttackMotion, lastFacing);

    public void PlayAttack(Vector3 worldDirection) => Play(AttackMotion, worldDirection);

    public void PlayHit() => Play(HitMotion, lastFacing);

    // 공격·피격이 끝난 뒤나 이동 상태·방향이 바뀌면 대기·걷기 상태로 바꾼다.
    private void UpdateLocomotion(bool isMoving)
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            return;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);

        // 공격·피격은 끝까지 재생한다. 끝나면 Animator가 기본 상태(Idle)로 돌려보내고, 그다음 프레임부터 여기서 다시 고른다.
        if (OneShotHashes.Contains(current.shortNameHash) && current.normalizedTime < 1f)
            return;

        string motion = isMoving ? WalkMotion : IdleMotion;

        if (!TryFindState(motion, DirectionName(lastFacing), out int hash))
            return;

        if (current.shortNameHash == hash)
            return;

        // 걷는 중에 방향만 바뀌면 발걸음 위치(재생 진행도)를 이어 간다.
        float startTime = isMoving && WalkHashes.Contains(current.shortNameHash) ? current.normalizedTime % 1f : 0f;
        animator.Play(hash, 0, startTime);

        // LateUpdate에서 바꾼 상태를 이번 프레임 화면에 바로 반영한다. 기존 Idle 그림이 한 프레임 보이는 것을 막는다.
        animator.Update(0f);
    }

    private void Play(string motion, Vector3 worldDirection)
    {
        if (animator == null)
            return;

        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude > 0.000001f)
            lastFacing = worldDirection.normalized;

        if (TryFindState(motion, DirectionName(lastFacing), out int hash))
        {
            animator.Play(hash, 0, 0f);
            return;
        }

        Debug.LogWarning($"'{name}'의 Animator에 {motion} 상태가 없습니다. 스프라이트 가져오기 메뉴를 실행했는지 확인하세요.", this);
    }

    private bool TryFindState(string motion, string direction, out int hash)
    {
        foreach (string candidate in Fallbacks[direction])
        {
            hash = StateHash($"{motion}_{candidate}");

            if (animator.HasState(0, hash))
                return true;
        }

        hash = 0;
        return false;
    }

    private string DirectionName(Vector3 facing)
    {
        float x = swapLeftRight ? -facing.x : facing.x;
        float angle = Mathf.Atan2(x, -facing.z) * Mathf.Rad2Deg;
        int sector = Mathf.RoundToInt(angle / 45f);
        return DirectionBySector[Mathf.Clamp(sector, -4, 4)];
    }

    private static int StateHash(string stateName)
    {
        if (!StateHashes.TryGetValue(stateName, out int hash))
        {
            hash = Animator.StringToHash(stateName);
            StateHashes[stateName] = hash;
        }

        return hash;
    }
}
