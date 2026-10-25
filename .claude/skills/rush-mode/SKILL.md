---
name: rush-mode
description: 와일드 골렘의 질주 모드(발동 게이지, 일시 강화 상태, 충돌 피해·넉백, 지형 파괴, 질주 계열 로그라이트 강화·시너지)를 설계·구현·수정할 때 사용한다. PlayerDash, PlayerMovement, 적 넉백, 충돌 피해를 건드리는 작업도 포함한다.
---

# 질주 모드 작업 스킬

## 목적
[README.md](../../../README.md) 8~9장의 질주 모드를 기존 코드 구조에 맞게 단계적으로 구현하고, 진행과 검증 결과를 [Assets/Scripts/tools/CHECKLIST.md](../../../Assets/Scripts/tools/CHECKLIST.md)에 남긴다.

## 시작할 때
1. [Assets/Scripts/tools/CLAUDE.md](../../../Assets/Scripts/tools/CLAUDE.md)를 읽는다. 특히 6장 "주의 사항".
2. [Assets/Scripts/tools/CHECKLIST.md](../../../Assets/Scripts/tools/CHECKLIST.md)의 "질주 모드" 섹션에서 현재 단계, 미결정 사항, 다음 할 일을 확인한다.
3. 관련 파일의 실제 상태를 확인한다: `PlayerScripts/PlayerDash.cs`, `PlayerMovement.cs`, `PlayerInput.cs`, `PartyController.cs`, `EnemyScripts/EnemyCotroller.cs`, `UnitScripts/LivingEntity.cs`, `Interface/*`.
4. `git status`로 사용자의 미커밋 변경을 확인한다.

## 기획 요구사항 (원문 요약)
| 구분 | 내용 |
|---|---|
| 정의 | 특정 조건 충족 시 사용하는 **일시적인** 강화 전투 상태 |
| 발동 조건 | 전투 중 게이지 축적 / 특정 아이템 획득 / 특수 능력·유물 효과 |
| 효과 | 이동 속도 증가, 관성·충돌 피해 증가, 넉백 강화, 벽·일부 지형 파괴, 적을 다른 적에게 충돌시켜 추가 피해, 일부 공격 모션 변화 |
| 목표 경험 | 일반 전투와 다른 빠르고 파괴적인 템포 |
| 연동 강화 | 폭주 코어, 중량 강화, 연쇄 파쇄, 흡혈 엔진 |
| 연동 시너지 | RED 다수(넉백), RED+BLUE(돌진 중 경직 저항), RED+GREEN(처치 시 이동 속도) |

기획서에 수치가 없다. 게이지 최대치, 지속 시간, 속도·피해 배율은 인스펙터 조정값(임시값)으로 두고 확정하지 않는다.

## 현재 코드와의 관계
- **질주 모드 = `PlayerDash`** (사용자 결정). 질주 관련 기능은 PlayerDash를 중심으로 붙인다.
- 게이지는 질주 모드를 켤 수 있게 하는 **조건**이다. 가득 차야 버튼이 보인다. 피해를 입히면 기본 획득, 처치하면 추가 획득, 돌격 중에는 차지 않는다(사용자 결정).
- 입력: 버튼 클릭 → 마지막 이동 방향으로 **일직선 돌격**, 조이스틱 입력 무시(`PlayerMovement.SetForcedDirection`). LobbyScene의 Button OnClick이 `Dash()`를 호출하므로 이름을 유지한다.
- 속도: 돌격 중 기본 속도에서 `maxDashSpeed`까지 `accelerationTime`에 걸쳐 가속하고, 도달하면 유지한다. 종료 시 `ResetMoveSpeed()`로 복원한다.
- 종료: 적이나 정면 벽에 부딪히면 게이지를 모두 소모하고(적이면 피해) 끝난다(사용자 결정). 정면 벽은 접촉 법선과 돌격 방향으로 판정한다. 충돌 없이 1분(`maxDashDuration`)이 지나도 끝난다(사용자 결정).
- 플레이어·파티가 적에게 주는 피해는 `PlayerDash.DealDamage(LivingEntity, float)`를 거친다. 직접 `TakeDamage`를 호출하면 게이지가 차지 않는다. 강화 효과 등 피해 외 축적은 `AddGauge(float)`.
- 충돌 피해: `LivingEntity.TakeDamage`를 쓴다(사용자 결정).
- `PlayerMovement.SetMoveSpeed`는 대입 방식이다. 다른 속도 효과(시너지 등)가 추가되면 PlayerDash와 덮어쓰기 충돌이 나지 않도록 구조를 다시 검토한다.

## 구현 권장 구조 (권장 사항, 강제 아님)
기획의 확장 요소(강화·시너지·아이템)가 많으므로 다음 책임을 분리하는 것을 권장한다.
1. **게이지**: 축적·소모·최대치. 적 처치/피격/시간 등 축적 원인을 메서드로 받는다(폭주 코어가 여기에 붙는다).
2. **상태**: 진입·유지·종료와 지속 시간. 진입/종료/질주 중 처치 이벤트(`event Action`)를 제공해 강화·시너지·UI가 구독하게 한다. 기존 `LivingEntity.onDeath` 이벤트 방식과 맞춘다.
3. **효과 적용**: 이동 속도, 충돌 피해, 넉백 배율. 해제 시 원래 값으로 복원한다.
4. **충돌 처리**: Player Rigidbody 기준 `OnCollisionEnter`. 파티원은 복합 콜라이더이므로 파티원 쪽 접촉도 Player에서 들어온다.
5. **조정값**: 게이지·지속 시간·배율은 ScriptableObject 또는 `[SerializeField]` 필드로 노출한다.

## 구현 시 확인할 기술 포인트
- **피해 경로**: 충돌 피해에 `IDamageable`, `IEnemy.takeDamage`, `LivingEntity.TakeDamage` 중 무엇을 쓸지 먼저 정한다. 현재 `EnemyCotroller`는 `LivingEntity`를 상속한다.
- **넉백과 NavMeshAgent**: 에이전트가 켜진 상태에서는 Rigidbody 힘이 무시되거나 덮어써진다. 넉백 동안 에이전트 정지/비활성화 후 복귀하는 처리가 필요하다.
- **적-적 충돌(연쇄 파쇄)**: 넉백 중인 적이 다른 적·벽에 부딪힌 것을 판정하려면 넉백 상태를 적 쪽에서 알아야 한다.
- **지형 파괴**: 파괴 가능 오브젝트를 태그/레이어/컴포넌트 중 무엇으로 구분할지 정한다. 레이어·태그 추가는 에디터 작업이므로 사용자에게 안내한다.
- **물리 타이밍**: 고속 이동 시 콜라이더 관통 가능성을 확인한다(Rigidbody 충돌 감지 모드, 이동 처리 위치).
- **애니메이션**: 공격 모션 변화가 필요하면 새 Animator 파라미터를 추가해야 하며, Animator Controller 수정은 에디터에서 하도록 안내한다.
- **런 한정 상태**: 질주 강화는 런 한정이므로 원정 종료 시 초기화되는 위치에 둔다.

## 단계 진행 순서
한 번에 전부 만들지 않고 아래 순서로 나눠 각 단계마다 Play 모드 확인을 요청한다.
1. 게이지 + 상태(수동 발동 버튼으로 진입/종료, 지속 시간 후 자동 해제)
2. 이동 속도 증가와 정확한 복원
3. 적 충돌 피해 + 넉백(NavMeshAgent 처리 포함)
4. 적-적/벽 충돌 추가 피해
5. 지형 파괴
6. 발동 조건 확장(아이템, 유물) 및 질주 계열 강화·시너지 연결
7. 공격 모션 변화, 연출

## 검증
- CLI 빌드·테스트 명령은 확인된 것이 없다. 코드 변경 후 IDE 진단으로 컴파일 오류를 확인하고, 동작은 사용자에게 Play 모드 확인 항목을 구체적으로 제시한다.
- 확인 항목 예: 게이지 최대 시 발동 가능 여부, 지속 시간 후 해제, 해제 후 속도 기본값 복귀, 적 넉백 후 추적 재개, 버튼 연타·해제 미호출 시 속도 누적 여부.
- 실행하지 않은 확인은 CHECKLIST.md 검증 기록에 "미실행"으로 남긴다.

## 하지 않을 것
- 요청받지 않은 PlayerMovement/PartyController 리팩터링, 스크립트 이름 변경.
- 씬·프리팹·.meta·Animator Controller의 텍스트 직접 편집.
- 기획서에 없는 수치를 확정값처럼 문서화.
- Assets/Downloded 외부 에셋 수정.

## 마무리
- CHECKLIST.md의 진행 단계, 검증 기록, 주요 결정, 남은 작업을 갱신한다.
- 사용자에게 변경 파일, 확인한 것, 에디터에서 확인이 필요한 것, 남은 결정 사항을 짧게 보고한다.
- 지속적으로 적용할 규칙이 생겼을 때만 CLAUDE.md를 갱신한다.
