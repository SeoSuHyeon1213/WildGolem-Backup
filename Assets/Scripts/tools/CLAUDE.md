# CLAUDE.md — 와일드 골렘 (Wild Golem)

이 저장소에서 작업할 때 따르는 지침이다. 기획 원문은 [README.md](README.md)(간단 기획서)이며, 기획과 코드가 다르면 임의로 맞추지 말고 차이를 기록한 뒤 사용자에게 확인한다.

## 1. 게임 요약 (기획서 기준)
- 장르: 방치형 거점 성장 + 로그라이트 액션 RPG
- 핵심 루프: 방치 재화 → 건물 업그레이드 → 골렘·원정 강화 → 파티 편성 → 로그라이트 원정(전투/이벤트/강화 선택/보스) → 전리품 → 거점 복귀
- 런 한정 강화는 귀환·패배 시 초기화, 건축 재화·설계도·골렘·전향 마족은 영구 보존
- 파티: 1인/2인/다인 편성마다 다른 장점. 속성 RED/GREEN/BLUE 조합 시너지
- **질주 모드(현재 작업 중)**: 전투 중 조건(게이지 축적, 아이템, 능력/유물)으로 발동하는 일시적 강화 상태. 이동 속도·관성/충돌 피해·넉백 강화, 벽/지형 파괴, 적-적 충돌 추가 피해, 공격 모션 변화
- 질주 관련 로그라이트 강화: 폭주 코어(처치 시 게이지 추가), 중량 강화(속도↓ 충돌 피해↑), 연쇄 파쇄(적을 적/벽에 충돌 시 추가 피해), 흡혈 엔진(질주 중 처치 시 회복)
- 질주 관련 시너지: RED 다수(넉백·근접 강화), RED+BLUE(돌진 중 경직 저항), RED+GREEN(처치 시 이동 속도 증가)

질주 모드 작업 절차는 [.claude/skills/rush-mode/SKILL.md](.claude/skills/rush-mode/SKILL.md), 진행 상태는 [CHECKLIST.md](CHECKLIST.md)를 본다.

## 2. 기술 환경 (설정 파일에서 확인)
| 항목 | 값 | 근거 |
|---|---|---|
| 엔진 | Unity 6000.6.0f1 | ProjectSettings/ProjectVersion.txt |
| 렌더 파이프라인 | URP 17.6.0 | Packages/manifest.json |
| 입력 | Input System 1.20.0 + Joystick Pack(가상 조이스틱) | manifest.json, Assets/Downloded/Joystick Pack |
| AI 이동 | AI Navigation 2.0.14 (NavMeshAgent) | manifest.json |
| 테스트 | Test Framework 1.8.0 설치됨, 작성된 테스트 없음 | manifest.json |

## 3. 주요 디렉터리
| 경로 | 역할 |
|---|---|
| Assets/Scripts/PlayerScripts | 플레이어 입력·이동·질주(PlayerDash)·공격·파티 대형 |
| Assets/Scripts/UnitScripts | LivingEntity(체력/사망 기반 클래스), 골렘 유닛(RedUnit) |
| Assets/Scripts/EnemyScripts | 적 AI(EnemyCotroller, NavMeshAgent 추적) |
| Assets/Scripts/Interface | IUnit, IEnemy, IDamageable |
| Assets/Scripts/LegacyScripts | 이전 구현(대부분 주석 처리). 참고용, 새 코드에서 의존하지 않는다 |
| Assets/ScriptableObject/CSharp | ScriptableObject 정의(UnitInfo) |
| Assets/Scenes | Intro, LobbyScene, BattleScene |
| Assets/Downloded | 외부 에셋(Joystick Pack 등). 직접 수정하지 않는다 |
| Assets/Scripts/tools | 범용 작업 지침 템플릿(AGENTS.md, CHECKLIST.md, SKILL.md) |

저장소 루트의 `*CommandBuffer.cs` 파일들은 Assets 밖에 있어 컴파일 대상이 아니다. 용도가 확인되지 않았으므로 건드리지 않는다.

## 4. 실행·검증 방법
- 빌드/테스트 CLI 명령은 확인된 것이 없다. 기본 검증은 Unity 에디터에서 해당 씬을 Play 모드로 실행해 확인하는 것이다.
- 컴파일 확인: 에디터 Console 오류 또는 IDE 진단(Assembly-CSharp.csproj 기준).
- 실행하지 않은 검증을 통과로 보고하지 않는다. 에디터 확인이 필요한 항목은 "사용자 확인 필요"로 남긴다.

## 5. 코드 규칙 (기존 코드에서 관찰)
- 주석과 로그 메시지는 한국어로 쓴다.
- 컴포넌트 참조는 `Awake`에서 `GetComponent`로 가져온다. 조정값은 `[SerializeField] private` 필드를 쓴다.
- Animator 파라미터는 `Animator.StringToHash`로 캐시한다(PartyController 방식). 기존 파라미터: `isMoving`, `isLeft`.
- 유닛 기본 수치는 ScriptableObject(UnitInfo)에 둔다. 새 조정값 묶음도 ScriptableObject를 우선 검토한다.
- 파티원은 Player의 자식으로 붙고 Rigidbody를 갖지 않는다. 즉 **파티 전체가 Player Rigidbody의 복합 콜라이더**로 충돌한다.

## 6. 주의 사항 (현재 코드에서 확인된 점)
수정 요청 범위에 들어올 때만 고치고, 그 외에는 보고만 한다.
- MonoBehaviour 파일명과 클래스명이 일치해야 인스펙터에 붙일 수 있다. `PartyController.cs`의 클래스명은 `PartyController1`이다.
- `EnemyCotroller`(오타)처럼 이미 씬/프리팹에 붙은 스크립트의 이름을 바꾸면 참조가 끊길 수 있다. 이름 변경은 사용자 확인 후 진행한다.
- 피해 처리 경로가 세 갈래다: `IDamageable.OnDamage(float, Vector3, Vector3)`, `IUnit/IEnemy.takeDamage(int)`, `LivingEntity.TakeDamage(float)`. 새 피해 로직은 어느 경로를 쓸지 먼저 정한다.
- 질주 모드는 `PlayerDash`가 담당한다. 속도는 `PlayerMovement.SetMoveSpeed`(대입)와 `ResetMoveSpeed`로만 바꾼다.
- `PlayerMovement`는 `Update`에서 `Rigidbody.MovePosition`을 호출한다.
- NavMeshAgent가 켜진 적에게 Rigidbody 힘을 주면 에이전트가 위치를 덮어쓴다. 넉백 시 에이전트 처리 방식을 함께 정해야 한다.
- 씬(.unity)·프리팹·.meta 파일은 직접 텍스트 편집하지 않는다. 에셋 추가·이동·삭제는 에디터에서 하도록 안내한다.

## 7. 작업 원칙
- 작업 전 `git status`와 관련 diff를 확인하고, 사용자 변경을 되돌리거나 덮어쓰지 않는다.
- 요청 범위 밖의 리팩터링·포맷 변경·파일 이동을 하지 않는다.
- 기획서에 수치가 없는 값(게이지 최대치, 지속 시간, 배율 등)은 임의로 확정하지 않는다. 임시값은 인스펙터 조정값으로 두고 "임시값"이라고 표시한다.
- 여러 단계 작업은 [CHECKLIST.md](CHECKLIST.md)에 진행 상황과 검증 결과를 갱신한다.
- 커밋·푸시는 요청받았을 때만 한다.
