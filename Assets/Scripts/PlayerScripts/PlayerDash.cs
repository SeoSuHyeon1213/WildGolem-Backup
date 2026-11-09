// 질주 모드: 게이지가 가득 차면 버튼이 나타나고, 버튼을 누르면 마지막 이동 방향으로 일직선 돌격한다.
// 돌격 중에는 최대 속도까지 점점 가속한 뒤 유지하며, 적이나 정면의 벽에 부딪히면
// 게이지를 모두 소모하고(적이면 피해를 준 뒤) 돌격을 끝낸다. 부서지는 구조물(DestructibleStructure)은 부수고 끝낸다.
using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [Header("속도")]
    [SerializeField] private float maxDashSpeed = 10f;   // 임시값: 가속이 멈추는 임계 속도
    [SerializeField] private float accelerationTime = 2f; // 임시값: 기본 속도에서 임계 속도까지 걸리는 시간(초)
    [SerializeField] private float maxDashDuration = 60f; // 아무것에도 부딪히지 않을 때 돌격이 끝나는 시간(초)

    [Header("게이지")]
    [SerializeField] private float maxGauge = 100f;       // 임시값
    [SerializeField] private float gaugePerHit = 10f;     // 임시값: 피해를 입힐 때마다 얻는 게이지
    [SerializeField] private float gaugePerKill = 20f;    // 임시값: 처치 시 추가로 얻는 게이지
    [SerializeField] private GameObject dashButton;       // 게이지가 가득 찼을 때만 표시
    // 테스트 전용: 켜면 게이지가 항상 가득 찬 것으로 본다(적이 없는 시험 씬에서 버튼만으로 반복 돌격). 실제 전투 씬에서는 끈다.
    [SerializeField] private bool infiniteGaugeForTest = false;

    [Header("충돌")]
    [SerializeField] private float dashDamage = 10f;      // 임시값: 돌격 중 적과 부딪혔을 때 피해
    [SerializeField] private float wallFacingThreshold = 0.5f; // 접촉면이 돌격 방향을 이만큼 마주 볼 때 벽 충돌로 본다(0~1)

    private PlayerMovement playerMovement;
    private float currentSpeed;
    private Vector3 dashDirection;
    private float dashElapsed;

    public bool IsDashing { get; private set; }
    public bool IsAtMaxSpeed => IsDashing && currentSpeed >= maxDashSpeed;
    public float Gauge { get; private set; }
    public float GaugeRatio => Gauge / maxGauge;
    public bool IsGaugeFull => infiniteGaugeForTest || Gauge >= maxGauge;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        UpdateButtonVisibility();
    }

    // 버튼 OnClick에 연결되어 있다(LobbyScene). 게이지가 가득 차 있을 때 돌격을 시작한다.
    public void Dash()
    {
        StartDash();
    }

    public void StartDash()
    {
        if (IsDashing || !IsGaugeFull)
            return;

        IsDashing = true;
        dashDirection = playerMovement.LastMoveDirection;
        dashElapsed = 0f;
        currentSpeed = playerMovement.DefaultMoveSpeed;
        playerMovement.SetForcedDirection(dashDirection);
        UpdateButtonVisibility();
    }

    // 충돌 없이 끝나는 경우(컴포넌트 비활성화 등)에는 게이지를 유지한다.
    public void EndDash()
    {
        if (!IsDashing)
            return;

        IsDashing = false;
        playerMovement.ClearForcedDirection();
        playerMovement.ResetMoveSpeed();
        UpdateButtonVisibility();
    }

    // 플레이어·파티가 적에게 피해를 줄 때 이 메서드를 거친다.
    // 피해를 입히면 기본 게이지를, 이번 피해로 처치하면 추가 게이지를 얻는다. 돌격 중에는 얻지 않는다.
    public void DealDamage(LivingEntity target, float damage)
    {
        if (target == null || target.dead)
            return;

        target.TakeDamage(damage);

        if (IsDashing)
            return;

        float amount = gaugePerHit;
        if (target.dead)
            amount += gaugePerKill;

        AddGauge(amount);
    }

    // 게이지 축적 원인(피해, 처치, 강화 효과 등)에서 호출한다. 돌격 중에는 쌓이지 않는다.
    public void AddGauge(float amount)
    {
        if (IsDashing)
            return;

        Gauge = Mathf.Clamp(Gauge + amount, 0f, maxGauge);
        UpdateButtonVisibility();
    }

    private void Update()
    {
        if (!IsDashing)
            return;

        // 시간 제한에 걸려 끝나도 돌격은 사용한 것으로 보고 게이지를 소모한다.
        dashElapsed += Time.deltaTime;
        if (dashElapsed >= maxDashDuration)
        {
            Gauge = 0f;
            EndDash();
            return;
        }

        if (currentSpeed >= maxDashSpeed)
            return;

        float acceleration = (maxDashSpeed - playerMovement.DefaultMoveSpeed) / Mathf.Max(accelerationTime, 0.01f);
        currentSpeed = Mathf.MoveTowards(currentSpeed, maxDashSpeed, acceleration * Time.deltaTime);
        playerMovement.SetMoveSpeed(currentSpeed);
    }

    // 파티원은 Player의 복합 콜라이더이므로 파티원 쪽 접촉도 여기로 들어온다.
    // 벽에 붙은 채로 돌격을 시작하면 Enter가 오지 않으므로 Stay에서도 확인한다.
    private void OnCollisionEnter(Collision collision)
    {
        HandleDashCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleDashCollision(collision);
    }

    private void HandleDashCollision(Collision collision)
    {
        if (!IsDashing)
            return;

        // 부서지는 구조물을 정면으로 들이받으면 부수고, 벽 충돌과 같이 게이지를 소모하며 끝낸다.
        DestructibleStructure structure = collision.collider.GetComponentInParent<DestructibleStructure>();

        if (structure != null)
        {
            if (!structure.IsBroken && IsFrontalWall(collision))
            {
                structure.Break(dashDirection, gameObject);
                FinishDashOnImpact();
            }

            return;
        }

        LivingEntity target = collision.collider.GetComponentInParent<LivingEntity>();

        if (target != null && !target.transform.IsChildOf(transform))
        {
            if (target.dead)
                return;

            DealDamage(target, dashDamage);
            FinishDashOnImpact();
            return;
        }

        if (IsFrontalWall(collision))
            FinishDashOnImpact();
    }

    // 바닥이나 옆으로 스치는 벽은 제외하고, 돌격 방향을 정면으로 막는 면만 벽으로 본다.
    private bool IsFrontalWall(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Dot(collision.GetContact(i).normal, -dashDirection) >= wallFacingThreshold)
                return true;
        }

        return false;
    }

    private void FinishDashOnImpact()
    {
        Gauge = 0f;
        EndDash();
    }

    private void UpdateButtonVisibility()
    {
        if (dashButton != null)
            dashButton.SetActive(IsGaugeFull && !IsDashing);
    }

    private void OnDisable()
    {
        EndDash();
    }

    [ContextMenu("게이지 채우기 (테스트)")]
    private void FillGaugeForTest()
    {
        AddGauge(maxGauge);
    }
}
