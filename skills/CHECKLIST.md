# 작업 체크리스트

## 관리 규칙
- 실제 수행과 필요한 검증이 끝난 항목만 체크한다.
- 실패·차단·미실행은 완료로 표시하지 않는다. 불필요한 항목은 `해당 없음 — 이유`로 표시한다.
- 주요 단계가 끝나거나 상태가 바뀔 때 갱신한다.
- 여러 작업을 병행하면 작업별 섹션을 추가한다. 지속 규칙은 [CLAUDE.md](CLAUDE.md)에 둔다.

---

# 질주 모드

작업 절차: [.claude/skills/rush-mode/SKILL.md](../.claude/skills/rush-mode/SKILL.md) · 기획 원문: [README.md](../README.md) 8~9장

## 목표와 범위
- 목표: 게이지가 가득 차면 버튼으로 일직선 돌격을 시작하고, 적·벽 충돌 시 게이지를 소모하며 공격하는 질주 모드를 구현한다.
- 작업 대상: Assets/Scripts/PlayerScripts (PlayerDash, PlayerMovement), EnemyScripts, 필요 시 신규 스크립트
- 완료 조건: 게이지 가득 참 → 버튼 표시 → 클릭 시 직선 돌격(가속 후 유지) → 적/벽 충돌 시 게이지 0·적 피해·돌격 종료·기본 속도 복귀가 Play 모드에서 확인된다. (넉백·지형 파괴·강화 연동은 후속 단계)

## 현재 상태
- 진행 단계: 구현 (직선 돌격·게이지·충돌 종료·피해 코드 완료, 에디터 확인 대기)
- 다음 할 일: 인스펙터에서 `Dash Button` 연결 → Play 모드 확인 → 넉백 구현
- 차단 원인: 없음

## 확정 사항 (사용자 결정)
- [x] 1. 질주 모드 = PlayerDash
- [x] 2. 게이지 = 질주 모드를 켤 수 있게 해 주는 조건. 가득 차야 버튼이 보인다
- [x] 3. 버튼 클릭 → 일직선으로만 돌격 (조이스틱 입력 무시)
- [x] 4. 속도 = 돌격 중 점점 가속, 임계 속도(maxDashSpeed) 도달 후 유지
- [x] 5. 벽이나 적에 부딪히면 게이지를 모두 소모하고 공격(적에게 피해) 후 종료
- [x] 6. 피해 경로 = `LivingEntity.TakeDamage`, 게이지 축적 = 피해 시 기본 + 처치 시 추가, 돌격 중에는 게이지가 차지 않음
- [x] 7. 충돌 없이 1분(maxDashDuration 60초)이 지나면 돌격 종료
- [x] 8. 돌격 방향 = 마지막 이동 방향 (8방향. 한 번도 움직이지 않았으면 +Z)
- 폐기: 버튼 토글로 끄기, 움직임을 멈추면 종료 (직선 돌격 방식으로 대체)

## 미결정 사항 (사용자 확인 필요)
- [ ] 7-1. 시간 제한으로 끝날 때 게이지 소모 여부 (현재: 소모)
- [ ] 9. 파괴 가능 지형 구분 방식 (태그 / 레이어 / 컴포넌트) — 현재 구현: 컴포넌트(DestructibleStructure), 2026-11-09 개발 판단. 사용자 확인 전
- [ ] 10. 임시 수치 확정: maxGauge 100, gaugePerHit 10, gaugePerKill 20, maxDashSpeed 10, accelerationTime 2초, dashDamage 10

## 1. 사전 확인
- [x] CLAUDE.md와 기획서(README.md)를 확인했다.
- [x] 관련 코드 상태를 확인했다.
- [x] 작업 직전 `git status`로 미커밋 변경을 확인했다. (스크립트 미커밋 변경 없음)
- 참고: PlayerDash는 현재 `Assets/Scenes/DashTest.unity`에만 배치되어 있다(Button OnClick → `Dash`). 2026-11-09 로비 거점 작업으로 LobbyScene에서 시험 오브젝트와 함께 옮겼다. BattleScene에는 없다. 질주 Play 확인은 DashTest 씬에서 한다.
- 참고: 현재 씬·프리팹에 EnemyCotroller가 배치된 곳이 없다. 적 충돌 확인에는 적 오브젝트(Collider + EnemyCotroller + NavMeshAgent) 배치가 필요하다.

## 2. 실행
- [ ] 단계 1: 게이지 + 버튼 표시 (코드 완료, Play 모드 확인 전)
  - `Gauge`, `GaugeRatio`, `IsGaugeFull`, `AddGauge(float)`(돌격 중 무시)
  - `DealDamage(LivingEntity, float)`: TakeDamage 후 gaugePerHit, 처치 시 gaugePerKill 추가. 돌격 중에는 게이지 없음. 일반 공격 구현 시 이 메서드를 거쳐야 한다(PlayerAttack은 현재 비어 있음)
  - `dashButton`: 게이지 가득 참 && 돌격 중 아님일 때만 표시. **인스펙터 연결 필요**
  - 테스트: PlayerDash 컴포넌트 우클릭 → "게이지 채우기 (테스트)"
  - 테스트용 `infiniteGaugeForTest`(2026-11-09): 켜면 게이지가 항상 가득 찬 것으로 본다. DashTest 씬에서 켜 두고 `dashButton`도 연결함(에디터 SerializedObject로 설정 후 저장). 실제 전투 씬에서는 끈다.
  - 수정(2026-11-09): 돌격 시작 시 버튼이 숨겨지지 않던 문제 → StartDash에서 버튼 표시 갱신.
  - 확인(2026-11-09, DashTest Play, 버튼 onClick 호출): 게이지 없이 버튼으로 돌격 시작·버튼 숨김 → 앞 3m 벽 파괴·돌격 종료·버튼 다시 표시 → 다시 눌러 재돌격. 파티원 RED가 플레이어 앞(약 1.5m)에 있어 RED가 먼저 벽에 닿고, 플레이어는 약 1.1m 지점에서 멈춤(복합 콜라이더 설계대로). 실제 터치 클릭은 사용자 확인 필요.
- [ ] 단계 2: 직선 돌격 + 가속·유지 + 종료 시 속도 복원 (코드 완료, Play 모드 확인 전)
  - `Dash()`(버튼 연결 유지) → `StartDash()`: 마지막 이동 방향으로 고정
  - PlayerMovement: `LastMoveDirection`, `SetForcedDirection/ClearForcedDirection` 추가. `SetMoveSpeed`는 대입 방식
  - 시간 제한: 충돌 없이 maxDashDuration(60초) 경과 시 게이지 0 후 종료
  - `EndDash()`: 강제 방향 해제 + 기본 속도 복원. 충돌 없이 끝나면(비활성화 등) 게이지 유지
- [ ] 단계 3: 충돌 처리 (넉백 제외 코드 완료, Play 모드 확인 전)
  - `OnCollisionEnter/Stay` → 적(LivingEntity, 자기 자식 제외)이면 dashDamage 피해 후 종료, 정면 벽(접촉 법선과 돌격 방향 내적 ≥ wallFacingThreshold 0.5)이면 종료. 둘 다 게이지 0
  - 바닥·옆으로 스치는 벽은 종료 조건에서 제외
  - 넉백(NavMeshAgent 정지·복귀): 미구현
- [ ] 단계 4: 적-적 / 적-벽 충돌 추가 피해 (연쇄 파쇄 기반)
- [ ] 단계 5: 벽·일부 지형 파괴
- [ ] 단계 6: 발동 조건 확장(아이템·유물), 질주 계열 강화(폭주 코어·중량 강화·연쇄 파쇄·흡혈 엔진)·시너지 연결
- [ ] 단계 7: 공격 모션 변화 및 연출

## 3. 검증
- [x] 변경 후 컴파일 오류가 없다. (VS Code 진단, 아래 기록)
- [ ] 처음엔 버튼이 숨겨져 있고, 게이지가 가득 차면 나타난다.
- [ ] 버튼 클릭 시 마지막 이동 방향으로 직진하고, 조이스틱을 움직여도 방향이 바뀌지 않는다.
- [ ] 돌격 중 약 2초간 가속 후 속도가 유지된다.
- [ ] 정면 벽 충돌 시 돌격 종료, 게이지 0, 버튼 숨김, 기본 속도·조이스틱 조작 복귀.
- [ ] 적 충돌 시 적 체력 감소 후 같은 결과. 돌격 중 피해로 게이지가 차지 않는다.
- [ ] 바닥이나 옆벽을 스칠 때는 돌격이 끝나지 않는다.
- [ ] 충돌 없이 60초가 지나면 돌격이 끝나고 게이지가 0이 된다. (테스트 시 인스펙터에서 maxDashDuration을 짧게 조정 가능)
- [ ] 파티원(복합 콜라이더) 접촉도 충돌로 처리된다.
- [ ] 기존 일반 이동·조이스틱 입력·파티 대형이 그대로 동작한다.

### 검증 기록
| 확인 항목 | 명령 또는 방법 | 결과 | 미실행·실패 이유 |
|---|---|---|---|
| PlayerDash.cs, PlayerMovement.cs 컴파일 | VS Code 진단(getDiagnostics) | 통과 (오류·경고 0) | |
| 토글·가속·유지·복원 동작 | LobbyScene Play 모드 | 해당 없음 | 직선 돌격 방식으로 설계 변경 |
| 게이지·버튼 표시·정지 해제·충돌 피해 추가 후 컴파일 | VS Code 진단(getDiagnostics) | 통과 (PlayerDash.cs, PlayerMovement.cs 오류 0) | |
| 버튼 표시/숨김, 정지 시 해제, 적 충돌 피해 | Play 모드 | 해당 없음 | 직선 돌격 방식으로 설계 변경 |
| 직선 돌격·충돌 종료 변경 후 컴파일 | VS Code 진단(getDiagnostics) | 통과 (PlayerDash.cs, PlayerMovement.cs 오류 0) | |
| 직선 돌격·벽/적 충돌 종료·게이지 소모 | Play 모드 | 미실행 | 에디터 실행·적 배치 필요 |
| 시간 제한 추가 후 컴파일 | VS Code 진단(getDiagnostics) | 통과 (PlayerDash.cs 오류 0) | |

결과는 통과 / 실패 / 미실행 / 해당 없음으로 구분한다.

## 4. 완료
- [ ] 필요한 문서(CLAUDE.md, 스킬)를 갱신했다.
- [ ] 변경 내용을 최종 검토했다.
- [ ] 미완료 항목과 남은 한계를 기록했다.
- [ ] 사용자에게 변경 내용과 검증 결과를 보고했다.

## 주요 결정
- 결정: 질주 모드 작업용 지침을 CLAUDE.md / 스킬 / 체크리스트로 분리
- 이유: 지속 규칙(CLAUDE.md), 작업 절차(스킬), 진행 상태(체크리스트)를 구분해 재개 시 혼동을 줄이기 위함

- 결정: PlayerDash를 질주 모드로 사용하고, 게이지가 가득 차면 버튼으로 직선 돌격, 적·벽 충돌 시 게이지 소모 + 공격 후 종료 (사용자 결정)
- 이유: 게이지는 발동 조건이고 돌격은 한 번의 충돌 공격으로 끝나는 구조. `Dash()` 이름을 유지해 기존 Button 연결을 그대로 쓴다
- 결정: 정면 벽 판정은 태그 없이 접촉 법선과 돌격 방향으로 한다
- 이유: 아직 벽 태그·레이어가 없고, 바닥·옆벽 스침을 제외할 수 있다

## 남은 작업
- [ ] Play 모드 확인 (3. 검증 항목)
- [ ] 넉백 단계 진행

---

# 협업 작업 기록

이 아래 템플릿은 새 그림 협업 작업의 섹션으로 복사해 사용한다. 기존 질주 모드의 완료 여부를 뜻하지 않는다.

## 그림 협업 작업용 템플릿
<!-- 그림 담당과 협업하는 작업에만 사용한다. 해당 없으면 `해당 없음 — 이유`로 표시한다. -->
- 작업 번호: (예: 20261109-001)
- [ ] 지시서를 `orders/<작업번호>.order.md`에 작성했다(대상, 시트 배치, 그림체 기준, 출력 형식 포함).
- [ ] 사용자가 앱 메시지 전송을 허용한 경우 챗지피티에 "우편함 확인해" 알림을 보냈다. 보낸 시각:
- [ ] `deliveries/<작업번호>/done.json`을 확인했다. 확인 시각:
- [ ] 그림을 오리고 크기를 맞췄다(원본 시트 보관).
- [ ] 필요한 효과를 코드로 덧그렸다.
- [ ] `reviews/<작업번호>.review.md`에 검수 결과를 기록했다. 재작업이 필요하면 새 작업 번호:
- [ ] 처리가 끝난 건을 `archive/`로 옮겼다.
- [ ] 변경을 확정된 시험 환경(테스트 빌드 또는 스테이징)에 반영하고 확인했다.
- [ ] 사람에게 확인받을 결정(출시·그림체 등)을 질문했다. 답변:
- [ ] 출시: 사람이 대화에서 직접 "출시해"라고 한 뒤에만 실제 앱에 반영했다.
---

# 지침 통합 (2026-11-09)
- 목표: 기존 게임 지침과 D:/tools의 협업 지침을 skills 폴더로 통합한다.
- [x] 프로젝트와 참고 지침, 기존 변경 상태를 확인했다.
- [x] 게임 고유 규칙을 보존하고 협업·우편함·검수·배포 규칙을 합쳤다.
- [x] AGENTS.md와 SKILL.md 및 체크리스트 템플릿에 협업 절차를 반영했다.
- [x] 루트 및 이전 CLAUDE.md 진입점과 질주 스킬의 참조를 새 경로에 연결했다.
- [x] 저장 결과, 문서 참조, 스킬 메타데이터와 기존 기록 보존을 확인했다.
- 검증 기록: 저장 내용 일치, UTF-8 디코딩, 상대 링크 21 개, CLAUDE import 2개, 두 SKILL.md의 필수 frontmatter를 확인했다. 기존 게임 지침 1~7장과 질주 체크리스트는 경로·문서 위치 설명 변경 외에 보존되었다. quick_validate.py는 Python 실행기를 사용할 수 없어 실행하지 않았으며 필수 메타데이터를 별도 검사했다.
- 미확정 운영 설정: 폴링/대기 시간·가공 스크립트·출력 규격·시험 환경·배포 대상. 실제 협업/배포 작업이 필요할 때 확인한다.
- 게임 실행 검증: 해당 없음 — 문서와 참조 경로만 변경한다. 기존 질주 Play 모드 확인 항목은 그대로 남긴다.
- 그림 요청·메시지 전송·배포: 수행하지 않음.

# 우편함 경로 지정 (2026-11-09)
- [x] 사용자가 작업 폴더(D:/Game/Wildgolem)를 공유 폴더로 쓰겠다고 지정했다.
- [x] 우편함을 프로젝트 루트 `mailbox/`로 정하고 CLAUDE.md 10장, AGENTS.md, SKILL.md에 반영했다.
- [x] `mailbox/orders`, `deliveries`, `reviews`, `archive` 폴더를 만들었다.
- [x] 사용자 결정: mailbox는 git에서 제외한다(.gitignore에 `/mailbox/` 추가).
- 남은 미확정: 폴링 간격·최대 대기 시간.

# 20261109-001 RED·와일드골렘 공격/피격 모션 (2026-11-09)
- 방식(사용자 지정): GPT가 블렌더 MCP로 3D 애니메이션 → 블렌더에서 픽셀화 렌더. 방향·칸 크기는 기존 기준(정면·좌·우, 1028px).
- [x] 기존 자료 확인: `WildGolemSprite3D/wildgolem.blend`(zstd 압축이라 내부 오브젝트 미확인), 기존 렌더 1028px 칸.
- [x] 지시서 초안 작성: `mailbox/orders/20261109-001.order.md`. 프레임 수(공격 6·피격 3)는 임시값, 픽셀 격자 크기는 시범 렌더 후 사용자가 결정.
- [ ] 사용자 지시서 확인
- [ ] GPT 블렌더 MCP 연결(사용자 진행 중)
- [ ] GPT에 알림 — 앱 메시지 전송 권한 미부여, 사용자가 직접 전달하거나 권한을 줘야 함
- [x] 시범 렌더 후 4px로 결정됨(납품 기준, 사용자와 GPT 사이에서 결정).
- [x] done.json 확인(13:42, 피격 13:45). 자동 검사 통과 — 해시·크기·투명·잘림·바닥선·Idle 복귀. 4px 블록 안에 렌더 잡음이 조금 있음. 검수: `mailbox/reviews/20261109-001.review.md`
- [x] ~~사용자 결정: 옆모습(±90°)·앞쪽 대각선(±45°) 5방향, 옆모습 8장을 `001/side/`로 추가 요청~~ — 아래 결정으로 대체됨. 이 개정은 지시서 파일에는 반영되지 않았었다.
- [x] 사용자 결정(2026-11-09): 방향 7개 — 정면 0°, 앞쪽 대각선 ±45°(001 납품분 유지), 옆 ±90°, 뒤쪽 대각선 ±135°(뒷모습 180° 대신). 001 지시서 상단에 방향 체계 변경을 기록했다.
- [x] 추가 방향 지시서 작성: `mailbox/orders/20261109-002.order.md` (옆·뒤 대각선 16장, 001과 같은 애니메이션·4px·1028칸, 납품 위치 `deliveries/20261109-002/`)
- [ ] 002 알림 전달(사용자) → done.json 확인 → 자동 검사
- [ ] 그림체 승인
- [x] 지시서와 Unity 프로젝트 차이 점검(파일 기준, 에디터 실행 안 함): 10건. 검수 파일 "지시서 ↔ Unity 프로젝트 차이" 참고. 핵심: Max Size 2048, Bilinear·압축, 화면에서 4px가 0.5px로 보임, 자동 슬라이스·중앙 피벗, 방향·공격·피격 상태 없음
- [x] 차이 조치(코드): 가져오기 에디터 도구 `Assets/Editor/GolemSpriteImporter.cs`(설정·슬라이스·클립·Animator 상태), 방향 재생 `Assets/Scripts/UnitScripts/GolemMotionAnimator.cs`, `RedUnit` 공격·피격 호출. dotnet 빌드 오류 0.
- [x] Unity CLI 연결(2026-11-09): com.unity.pipeline 0.10.0-exp.1 설치, `unity status` ready(포트 7800). 에디터 Console 오류 0·컴파일 실패 없음 — 새 스크립트 컴파일 확인.
- [x] 가져오기 메뉴 실행(CLI, 2026-11-09): 12개 시트 → `Assets/Sprites/Golems/`, 클립 12개 → `Assets/Animation/Golems/`, 두 컨트롤러에 attack/hit × front/left/right 상태 6개씩 추가. Console 오류·경고 0(npotScale 경고와 클립 길이 +1 tick을 고친 뒤 재실행).
- [x] 에디터 검증(script eval): 텍스처 원본 크기 유지(축소 없음), Point·무압축·최대 16384, 프레임 수 일치, 1028칸·중앙 피벗, 클립 길이 = done.json(1.5/0.417/0.667/0.333초), 반복 없음, 끝나면 Idle로 전환
- [x] Play 시험(CLI, LobbyScene, 2026-11-09): 프레임 진행 확인. GolemMotionAnimator를 직접 호출해 8방향 × 공격·피격 16회 → 정면/±45°는 해당 상태, 옆·뒤 대각선은 ±45°로 대신 재생(002 미납품). 공격 후 Idle 복귀 확인. 화면 캡처로 left=화면 왼쪽, right=화면 오른쪽 확인 → swapLeftRight 불필요. 시험 후 Play 종료, Run In Background·auto-tick 원래대로.
- 관찰(기존 문제, 고치지 않음): `RedUnit.Start`에서 "Tag: Enemy is not defined" 예외. 게임 화면에서 골렘 높이 약 90px/540px라 4px 픽셀 느낌이 보이지 않는다(차이 4번 결정 필요). RED가 플레이어 위에 겹쳐 그려진다.
- [ ] RedUnit.Attack 호출 지점 없음 — 공격 기능 작업 때 연결
- [ ] 사용자 결정: 화면 픽셀 크기(픽셀 크기·PPU·카메라), 기존 Idle·Walk 재렌더 여부, 공격 간격 대비 RED 1.5초 모션
- [ ] 와일드골렘 공격·피격 호출 지점 없음(PlayerAttack 비어 있음) — 공격 기능 작업 때 PlayAttack/PlayHit 연결
- [ ] Unity 반영(임포트 설정·Animator는 에디터 작업) → Play 모드 확인
- 참고: WildGolemSprite3D/ 폴더는 git 미추적 상태다(추적 여부 미결정).

# 공격 모션 정면 시안 (2026-11-09, 20261109-001)
- 사용자 방향: 와일드골렘은 일반적인 공격, RED는 탱커형의 묵직한 공격.
- [x] Blender MCP 연결과 두 캐릭터 모델을 확인했다. 누락 외부 참조는 렌더에 영향이 없다는 사용자 확인을 받았다.
- [x] 원본을 보존하고 mailbox/deliveries/20261109-001/wildgolem_motion.blend 작업본에 모션을 만들었다.
- [x] 와일드골렘: 약 0.67초, 6개 키 포즈, 짧은 준비와 한 팔 타격·빠른 복귀.
- [x] RED: 약 1.5초, 10개 키 포즈, 양팔 들어 내려치기·가라앉기·느린 복귀.
- [x] 정면 4px 시안 16개 포즈를 렌더했다. 셀 1028×1028, 투명 PNG RGBA, 가로 한 줄.
- [x] 와일드골렘 동일 타격 자세의 4px/8px 비교 이미지와 두 캐릭터 GIF·HTML 미리보기를 만들었다.
- [x] 원본 SHA-256 동일, 모든 포즈의 화면 잘림 없음, 시작/끝 자세 복귀(렌더 차이 최대 1/255)를 확인했다.
- 카메라: 기존 정면 직교 카메라를 복사하고 세로 중심만 3.0→3.45로 이동해 머리 잘림을 방지했다. 직교 배율·거리·프레임별 크기는 고정했다.
- 검증 기록: trial/*_render_checks.json, attack_trial_manifest.json. 시트 조합 과정의 색 공간을 보정하고 기존 렌더와 같은 색으로 저장했다.
- [ ] 픽셀 크기(4px/8px) 사용자 선택 및 모션 피드백.
- [ ] 선택 후 좌/우 방향과 최종 시트 제작. 이번 요청은 공격이며 피격 모션은 제작하지 않았다.
- 상태: 정면 시안 검토 대기. trial/ready.txt 작성, 최종 done.json은 미작성. Unity 반영·출시·앱 메시지 전송 없음.

# 대각선 공격 모션 (2026-11-09, 20261109-001)
- 사용자 요청: 기존 공격 모션의 대각선 방향을 추가한다.
- [x] 앞쪽 대각선 좌·우 45도 카메라를 만들고 기존 모션·재생 타이밍·직교 배율과 거리를 유지했다.
- [x] 와일드골렘 6프레임 × 2방향, RED 10프레임 × 2방향, 총 32포즈를 각각 3D 렌더했다(미러링 아님).
- [x] 투명 PNG RGBA 시트 4장(셀 1028×1028), GIF 4개, 비교 HTML과 재생 간격 메타데이터를 저장했다.
- [x] 모든 렌더에서 잘림 없음, 기본 자세 복귀(차이 최대 1/255), 원본·정면 작업본 SHA-256 보존을 확인했다.
- 검증 기록: mailbox/deliveries/20261109-001/diagonal/*_checks.json 및 diagonal_manifest.json.
- 작업본: wildgolem_motion_diagonal.blend. 기존 wildgolem_motion.blend는 보존했다.
- 픽셀 크기: 정면 시안과 같은 4px로 제작. 별도의 사용자 확정 선택으로 기록하지 않는다.
- 상태: 이번 대각선 좌·우 공격 추가 요청 완료. 전체 공격/피격 지시서의 최종 done.json은 작성하지 않았다. Unity 반영·배포 없음.

# 피격 스프라이트 제작·납품 (2026-11-09, 20261109-001)
- 사용자 요청: 추천한 각 골렘의 피격 모션을 스프라이트로 만들어 우편함에 넣는다.
- [x] 와일드골렘: 상체 뒤로 젖힘·팔 벌어짐·작은 반동 후 복귀, 5포즈, 약 0.33초.
- [x] RED: 작은 어깨 반동·몸 낮춰 충격 흡수·안정적인 복귀, 5포즈, 약 0.42초.
- [x] 정면·앞쪽 대각선 좌우(0/-45/+45도), 총 6시트·30포즈를 렌더했다.
- [x] 모든 시트는 셀 1028×1028, 5프레임 가로 한 줄, 투명 PNG RGBA, 기존 4px 표현이다.
- [x] 발의 월드 변환 고정, 화면 잘림 없음, 기본 자세 복귀(렌더 차이 최대 1/255), 원본·기존 공격 작업본 SHA-256 보존을 확인했다.
- [x] 우편함에 6개 피격 PNG·GIF 미리보기·HTML·재생 간격 JSON·별도 wildgolem_hit.blend를 저장했다.
- [x] 기존 공격 6장과 피격 6장을 작업 루트의 일관된 이름으로 정리하고 최종 파일 검증 후 done.json을 작성했다.
- 위치: mailbox/deliveries/20261109-001/. 상세 피격 자료는 hit/ 하위.
- 검증 기록: hit/hit_manifest.json, hit/*_checks.json. 재생 간격: sprite_timing.json.
- 상태: 요청한 스프라이트 제작·우편함 납품 완료. 제작 완료는 개발 담당의 Unity 반영·사용자 최종 검수·출시 승인을 뜻하지 않는다.

# 20261109-004 전체 동작 12px 재렌더 (2026-11-09)
- 결정(사용자): 픽셀 블록 12px(1028 칸 기준), Idle·걷기도 함께 다시 렌더. 근거: 4·12·16·24·32px 게임 화면 크기 미리보기 비교.
- [x] 지시서 `mailbox/orders/20261109-004.order.md` 작성: 대상 2 × idle·walk·attack·hit × 7방향 = 56장, 칸 86×86 원본 렌더(업스케일 금지), 먼저 시범 2장(trial/). 002는 취소·대체 표시.
- [x] 가져오기 도구: done.json의 cell_size 칸으로 자르기, PPU를 칸 크기에 맞춰 화면 크기 유지, idle·walk 반복 클립(Idle 복귀 전환 없음), 등록되지 않은 대상은 건너뛰기. Unity 재컴파일·재실행 Console 오류 0, 기존 1028 시트 PPU 800 그대로 확인.
- [x] 작업 003(BLUE·GREEN idle, 지시서 없이 진행됨)이 도구에 자동으로 들어온 것을 발견해 Assets 사본만 삭제했다(우편함 원본 유지). 이제 등록되지 않은 대상은 가져오지 않는다.
- [x] 004 알림 전달: 개발 담당이 computer use로 Codex 앱 "Wildgolem 지침 통합" 대화에 사용자 승인 문장 전송(2026-11-09). GPT 응답: 004 시범 2장부터 진행.
- [x] 시범 2장 도착(14:31, trial/ready.txt). 자동 검사: 516×86·688×86 RGBA, 알파 0/255, 바닥선 84행, 칸 경계 닿음 없음, 001 공격 타이밍(3/3/1/4/4/1) 일치. red_idle 8프레임×3tick=1초 반복.
- [x] 사용자 확인: 시범 2장 승인(2026-11-09). Codex에 나머지 54장 진행 요청 전송(사용자 승인 문장, 완료 시 GPT가 computer use로 Claude 앱에 완료 답변을 보내도록 요청). GPT 응답: 진행 시작.
- [x] 004 done.json 도착. 자동 검사 56장 전부 통과: RGBA, 칸 86, 알파 0/255, 바닥선 84행(두 골렘 모두), 잘림 없음, 공격·피격 타이밍 001과 같음. idle 8프레임×3tick(1초), walk 12프레임×3tick(1.5초).
- [ ] 004 → 가져오기(walk·idle 상태 추가) → Play 확인 → 본 작업 done.json → 가져오기·Play 확인
- [x] 003 처리 결정(사용자, 2026-11-09): BLUE·GREEN 디자인 승인, 12px로 다시 렌더. 003 4px는 보류(그림 담당 hold.json). 검수 `mailbox/reviews/20261109-003.review.md`
- [x] Idle·걷기 7방향을 쓰는 이동 애니메이션 코드 — 위 004 항목 참고

# BLUE·GREEN Idle 12px (2026-11-09, 003 → 005 · 006)
- 005(지시서 없이 진행 중, 14:22~): GREEN 작은 정사각형 수정, 12px 칸 86, 정면·±45° 3장. 아직 done.json 없음.
- [x] 지시서 `mailbox/orders/20261109-006.order.md`: BLUE idle 7방향 + GREEN 나머지 4방향(005 형태 사용자 확인 후), 12px 칸 86, 11장.
- [x] 006 알림 전달: 004와 같은 메시지로 전송(2026-11-09). GPT 응답: BLUE 진행, GREEN 4방향은 005 형태 확인 전까지 대기.
- [x] 005 done.json(14:25): GREEN 정면·±45° 3장 자동 검사 통과(알파 0/255, 잘림 없음). 바닥선 front·left 83행, right 84행으로 1px 차이.
- [x] 006 done.json(14:33): BLUE 7방향 자동 검사 통과(688×86, 알파 0/255, 바닥선 84행, 잘림 없음). 옆·뒤 방향이 매우 어둡다(조명 정면 기준). GREEN 4방향은 005 확인 대기.
- [ ] 사용자 확인: 005 GREEN 수정 형태, BLUE 옆·뒤 밝기
- [ ] 005·006 done.json → 검수 → 가져오기 도구에 blue·green 등록(Animator Controller 새로 필요) → Play 확인

# 에이전트 간 computer use 메시지 지침 (2026-11-09)
- 요청(사용자): 자동화를 위해 두 모델이 computer use를 쓰고, 필요하면 사용자에게 질문하도록 지침 수정.
- [x] skills/CLAUDE.md §9 역할 표, §11 "에이전트 간 메시지"·"사용자에게 질문" 추가. 정형 알림은 바로 보내고, 사용자 결정을 담은 메시지는 승인 뒤 전송(앞서 정한 "매번 확인"을 이 기준으로 바꿈).
- [x] skills/AGENTS.md "Agent Messaging", skills/SKILL.md 진행 순서 맞춤.
- [x] 그림 담당(GPT)에게 바뀐 지침 알림 전송(사용자 승인, 004 응답 종료 후). GPT 응답: 두 파일 최신 지침을 읽었고 정형 알림은 우편함 기록 후 바로, 결정 전달은 승인 후, 상대가 답변 중이면 기다리겠다고 확인.
- [x] GPT → Claude 앱 메시지 확인: 004 완료 메시지가 이 작업 대화에 도착(2026-11-09).
- 주의: 상대가 답변 중이면 기다리는 규칙 때문에 서로 기다리는 상태가 생길 수 있다. 상대 메시지를 받으면 짧게 답하고 턴을 끝낸 뒤 보낸다.

- [x] 004 Unity 가져오기(2026-11-09, 사용자 요청): 메뉴 실행. 시트 56장 모두 86칸·PPU 66.93·Point, 스프라이트 수=프레임 수, 클립 56개(idle·walk 반복, attack·hit 1회). 두 컨트롤러에 idle/walk/attack/hit × 7방향 = 28상태, 기본 상태 Idle 유지. 첫 실행 때 001의 1028px 시트 12장을 교체하며 옛 슬라이스 경고 78개(다시 자른 뒤 해소), 재실행 Console 오류 0·경고 0. blue·green은 미등록이라 건너뜀.
- [x] 004 검수 알림 전송(정형 알림, 2026-11-09).
- [x] 7방향 idle·walk 전환 코드(2026-11-09, 사용자 요청): GolemMotionAnimator가 대기·걷기 상태를 직접 고른다. Player·파티원은 PlayerMovement의 입력 기준 이동 방향(IsMoving·LastMoveDirection)을, 그 밖의 골렘은 위치 변화를 쓴다(벽에 밀리는 움직임으로 방향이 흔들리던 문제 때문). 공격·피격은 끝까지 재생 후 대기·걷기로 복귀, 걷는 중 방향 전환은 재생 진행도 유지. 상태가 없는 골렘은 기존 isMoving·isLeft 전환 유지. PlayerMovement.Awake에서 GolemMotionAnimator를 없으면 붙인다(RedUnit과 같은 방식).
- [x] Play 확인(CLI, SetForcedDirection으로 8방향 이동): Player·RED 모두 walk_<방향>, 멈추면 idle_<같은 방향>, 정뒤(180°)는 back_right. RED attack_right → idle_right 복귀, hit_* 재생 확인. 컴파일 오류 0. 화면 캡처로 12px 표시 확인.
- [ ] 사용자 확인: 조이스틱으로 직접 조작했을 때 느낌(사용자 확인 필요)

# 로비씬 방치형 거점 설계 (2026-11-09)
- 목표: LobbyScene을 방치형 거점으로 만든다(방치 재화 누적·수령 → 건물 업그레이드 → 골렘 관리·편성 → 원정 출발·귀환). 기획 원문: [README.md](../README.md) 3·4·6·7장.
- 레퍼런스: AFK: 새로운 여정 — 장르·구조·메커니즘만 참고한다(오프라인 보상 상한 누적·한 번에 수령, 월드 안 건물 상호작용, 골렘 육성, 속성 조합 편성). 이름·캐릭터·그림·UI 모양은 쓰지 않는다.
- 상태: **0~4단계 구현·Play 확인 완료(2026-11-09). 5단계(원정 연결)·6단계(그림) 남음. 커밋 안 함.** L-10~L-12 미확정(임시 규칙으로 구현).

## 현재 상태 (파일 기준 확인, 에디터 미실행)
- LobbyScene = 이동·질주 시험 공간: Player(PlayerInput·PlayerMovement·PlayerDash·PartyController), RED 프리팹 2기, Floating Joystick, 질주 Button, 벽(w ×4, wall)·Cube·Capsule, CinemachineCamera, NavMesh.
- 거점 시스템(저장·재화·건물·편성) 코드는 없다. 씬 전환: Intro 버튼 → 빌드 인덱스 1(LobbyScene, `SceneChange1`), BattleScene 트리거 → 인덱스 0(Intro, `SceneChange`).
- 골렘 데이터: `UnitInfo`(이름·HP·SP·Damage·AttackSpeed)만 있다. 속성(RED/GREEN/BLUE) 필드는 없다.

## 확정 사항 (사용자 결정, 2026-11-09)
- [x] L-1. 화면 = 걸어다니는 3D 거점. 기존 조이스틱 이동·카메라를 쓰고, 건물에 다가가거나 탭하면 패널이 열린다. 원정은 거점 출구(게이트)에서 출발한다.
- [x] L-2. 방치 재화 출처 = 생산 건물(채석장·제련소) 레벨 + 점령 지역 수 보너스.
- [x] L-3. 첫 구현 재화 = 목재·석재·금속 3종. 마력 정수·특수 건축 재료는 종류만 정의하고 이후 원정 전리품으로 지급한다.
- [x] L-4. 질주 시험 오브젝트(벽·캡슐·질주 버튼·RED 2기)는 시험 전용 씬(예: DashTest)으로 옮긴다. 이동은 에디터에서 한다(씬 텍스트 편집 금지).

- [x] L-5. 목재 = 거점 안의 작은 숲 지역. 플레이어가 직접 벌목하거나, 골렘을 숲에 배정해 방치 중에도 목재를 얻는다.
- [x] L-6. 수치는 임시값으로 만든다(아래 "임시값" 표). 모두 ScriptableObject 인스펙터 조정값이다.
- [x] L-7. 편성 = 플레이어 주변 8방향 슬롯에 방향마다 1명. 일반 원정은 플레이어 포함 최대 6명(골렘 5), 이후 보스 레이드 같은 콘텐츠는 최대 9명(8방향 모두)까지 늘릴 수 있게 한다. 기존 `PartyController1`의 8방향 슬롯(`SlotDirections`, 0=앞부터 시계 방향)을 그대로 쓴다.
- [x] L-8. 골렘 강화 = 연구로 레벨 상한 해금 + 골렘 레벨업 재화 소비를 함께 쓴다.
- [x] L-9. 거점 건물은 도형 자리표시로 시작한다(우편함 그림 요청은 나중에).

## 미결정 사항 (사용자 확인 필요)
- [ ] L-10. 숲에 배정한 골렘을 원정 편성에도 넣을 수 있는가 (기본안: 배정 중에는 편성 불가, 편성하면 배정 해제)
- [ ] L-11. 골렘 레벨업 재화 종류 (임시: 금속. 기획서상 후보는 마력 정수 — 원정 전리품 연결 후 바꿀 수 있음)
- [ ] L-12. 시너지 효과 수치(README 7장 표의 효과량) — 4단계 때 임시값으로 둔다

## 임시값 (L-6, 모두 "임시값" 표시, 인스펙터 조정)
| 항목 | 임시값 |
|---|---|
| 오프라인 누적 상한 | 12시간 |
| 채석장 Lv1 석재 생산 | 60/시간, 레벨당 +30 |
| 제련소 Lv1 금속 생산 | 30/시간, 레벨당 +15 |
| 숲 배정 골렘 1기 목재 | 20/시간, 숲 배정 칸 3 |
| 직접 벌목 | 나무 1그루 타격당 목재 5, 3회에 쓰러짐, 30초 뒤 다시 자람 |
| 점령 지역 보너스 | 지역 1곳당 생산량 +5% |
| 건물 최대 레벨 | 10 |
| 업그레이드 비용 | 기본 비용 × 1.5^(현재 레벨−1). 채석장 기본: 목재 50·금속 20, 제련소 기본: 목재 50·석재 50 |
| 시작 재화 | 목재 100·석재 100·금속 50 |
| 연구 1단계당 골렘 레벨 상한 | +10 (시작 상한 10) |
| 골렘 레벨업 비용 | 금속 10 × 현재 레벨 |

## 화면 구조 (안)
- 거점 맵(3D): 폐허 거점 위에 건물 부지. 부지마다 `BuildingSite`(트리거 + 탭) → 건물 패널(레벨·효과·업그레이드 비용·버튼). 미해금 부지는 잠금 표시.
- 상단 HUD: 목재·석재·금속 보유량.
- 방치 보상 상자(거점 중앙 오브젝트 + HUD 버튼): 누적량·누적 시간/상한 표시, 탭하면 수령.
- 숲 지역: 나무(자리표시 원기둥)에 다가가 공격/탭하면 직접 벌목. 숲 입구의 배정 패널에서 골렘을 배정하면 방치 목재 생산.
- 골렘 패널(훈련소/연구소에서 열기): 보유 골렘 목록·정보·레벨업. 연구소 패널: 레벨 상한 연구.
- 편성 패널(게이트에서 열기): 플레이어를 가운데 둔 8방향 슬롯 그림에 골렘 배치(최대 인원 표시), 속성 시너지·인원 편성(1인/2인/다인) 미리보기 → 원정 출발.
- 귀환 시: 전리품 결과 패널 → 재화 반영.

## 시스템 목록 (안)
| 시스템 | 역할 |
|---|---|
| `SaveSystem` | JSON 저장/불러오기(`Application.persistentDataPath`). 재화·건물 레벨·골렘·편성·점령 지역·마지막 수령 시각 |
| `ResourceWallet` | 재화 보유·증감·지불 가능 확인, 변경 이벤트 |
| `IdleProduction` | (생산 건물 레벨 생산량 + 숲 배정 골렘 생산량) × (1 + 점령 보너스). 마지막 수령 시각부터 상한까지 누적, 수령 시 지갑에 반영 |
| `ForestArea` / `Tree` | 직접 벌목(타격 횟수·재생 시간), 숲 배정 칸 관리 |
| `BuildingSystem` | 건물 정의·레벨·업그레이드(비용 지불 → 레벨 +1 → 효과·해금 갱신) |
| `GolemRoster` | 보유 골렘·레벨·레벨업(연구 상한 이내, 재화 소비) |
| `ResearchSystem` | 연구 단계 → 골렘 레벨 상한 |
| `PartyFormation` | 8방향 슬롯(인덱스 0~7)에 골렘 id, 콘텐츠별 최대 인원(일반 6·레이드 9), 속성 집계, 시너지 미리보기 |
| `ExpeditionSession` | 씬 전환 사이에 편성 → BattleScene, 전리품·점령 결과 → LobbyScene 전달 |
| 거점 UI | HUD, 건물·골렘·편성·결과 패널(uGUI, 기존 Canvas 방식) |

## 데이터 구조 초안
```csharp
enum ResourceType { Wood, Stone, Metal, ManaEssence, SpecialMaterial }
enum GolemElement { Red, Green, Blue }

// ScriptableObject (수치는 임시값)
BuildingDefinition { string id; string displayName; BuildingLevel[] levels; }
BuildingLevel { ResourceAmount[] upgradeCost; ResourceAmount[] productionPerHour; string[] unlocks; }
IdleConfig { float maxOfflineHours; float bonusPerConqueredRegion; }
ForestConfig { int assignSlots; float woodPerGolemPerHour; int woodPerHit; int hitsPerTree; float regrowSeconds; }
FormationRule { string contentId; int maxMembersIncludingPlayer; }  // normal = 6, raid = 9
ResearchDefinition { int baseLevelCap; int levelCapPerStep; ResourceAmount[] stepCosts; }
GolemDefinition { string id; UnitInfo baseStats; GolemElement element; ResourceType levelUpCurrency; }  // UnitInfo 재사용

// 저장 데이터 (JSON)
SaveData {
  long lastClaimUnixTime;
  List<ResourceAmount> resources;
  List<BuildingState> buildings;   // { id, level }
  List<GolemState> golems;         // { id, level }
  string[] partySlots = new string[8];  // 방향 슬롯별 골렘 id (빈 칸 = null), PartyController1.SlotDirections 순서
  List<string> forestAssigned;     // 숲 배정 골렘 id
  int researchStep;
  List<string> conqueredRegions;
}
```
- 시간은 UTC Unix 시각으로 저장한다. 기기 시계를 되돌리면 누적을 0으로 처리한다(음수 경과 방지). 서버 시각 검증은 범위 밖.

## 구현 단계 (사용자 승인 2026-11-09 "시작해")
- [x] 0. 질주 시험 오브젝트 분리(CLI script eval): LobbyScene을 `Assets/Scenes/DashTest.unity`로 복사(질주 시험 구성 그대로 보존), LobbyScene에서 wall·Capsule ×3·Cube ×4·질주 Button·RedUnit과 Player의 PlayerDash 컴포넌트를 제거 후 저장. DashTest를 빌드 목록 4번째(인덱스 3)에 추가해 기존 인덱스(Intro 0·Lobby 1·Battle 2)는 그대로.
- [x] 1. 데이터·저장: `Assets/Scripts/LobbyScripts/LobbyTypes.cs`(enum·SaveData), `SaveSystem.cs`(JSON, 임시 파일 후 교체), `LobbyState.cs`(규칙 전체, MonoBehaviour 아님), `LobbyManager.cs`(불러오기·변경 시 저장·일시정지/종료 시 저장). SO: `BuildingDefinition`·`GolemDefinition`·`LobbyConfig`(Assets/ScriptableObject/CSharp), 에셋은 `Assets/ScriptableObject/Lobby/`.
- [x] 2. 방치 누적·수령 + HUD: 상단 재화, 오른쪽 위 방치 보상 버튼(누적 시간/상한·받을 양, 가득 차면 주황), 상자 패널에서 수령.
- [x] 3. 건물: 채석장·제련소 업그레이드, 대장간·수용소·성벽은 잠금("준비 중"). 지점에 다가가면 오른쪽 아래 "○○ 열기" 버튼 → 패널.
- [x] 3-1. 숲: 나무 4그루 직접 벌목(3타에 쓰러짐, 30초 뒤 재생) + 숲 작업장 표지판에서 골렘 배정/해제.
- [x] 4. 훈련소(골렘 레벨업), 연구소(레벨 상한 연구), 원정 게이트(콘텐츠 일반/레이드 전환, 8방향 슬롯 순환, 인원·시너지 미리보기).
  - 편성은 저장만 한다. 로비의 PartyController1에 골렘을 실제로 세우는 것은 5단계에서 원정 연결과 함께 한다.
- [ ] 5. 원정 출발·귀환 연결(ExpeditionSession). 게이트의 "원정 출발" 버튼은 비활성 자리표시. 원정 런 구조는 BattleScene 작업과 함께 정한다
- [ ] 6. 건물 그림·연출: 자리표시로 시작, 그림은 이후 우편함 절차
- 각 단계: 컴파일(Unity CLI Console) → Play 확인 → 이 섹션 갱신. 실행하지 않은 검증은 미실행으로 기록한다.

## 구현 중 정한 것 (설계 초안과 달라진 점)
- 건물 레벨별 배열 대신 공식(기본값 + 레벨당 증가, 비용 × 1.5^(레벨-1))으로 정의했다. 수치는 모두 임시값, `Building_*.asset`에서 조정.
- 연구소·훈련소·게이트·숲 표지판은 건물 정의가 아니라 상호작용 지점 종류(LobbySiteKind)다. 연구소 자체 레벨은 없다.
- 시작 보유 골렘: RED·GREEN·BLUE 3기(임시값, 편성·시너지 시험용. `Golem_*.asset`의 ownedAtStart). 플레이어(와일드골렘)는 속성 없음으로 보고 시너지 집계에서 뺐다.
- "다수" 시너지 기준: 같은 속성 2기 이상(임시값). 효과는 이름만 표시(L-12).
- 숲 배정 골렘은 편성 불가, 편성 중 골렘을 배정하면 편성에서 빠진다(L-10 기본안, 미확정).
- UI: 실행 중 코드로 만드는 uGUI(`LobbyUI`). 프로젝트에 한글 TMP 글꼴이 없어 레거시 Text + OS 글꼴(맑은 고딕 등)을 동적으로 쓴다 — 모바일 등에서 글꼴이 달라질 수 있어 정식 글꼴 결정 필요.
- 자리표시 배치: 메뉴 `Tools/Wild Golem/로비 거점 자리표시 배치`(`Assets/Editor/LobbySceneBuilder.cs`). 다시 실행하면 씬의 LobbyBase를 새로 만들고, 조정값 에셋은 이미 있으면 그대로 둔다. 바닥 밖으로 나가지 않게 보이지 않는 경계 벽을 둔다.
- 시험 메뉴: LobbyManager 컴포넌트 우클릭 → "시험: 1시간 경과 / 13시간 경과 / 점령 지역 +1 / 재화 +1000 / 저장 초기화".
- 저장 위치: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/WildGolem/wildgolem_save.json`. 시험 후 지워 두었다(다음 Play는 새 데이터로 시작).

## 검증 기록
| 확인 항목 | 명령 또는 방법 | 결과 | 비고 |
|---|---|---|---|
| 컴파일 | `unity recompile` | 통과 (오류 0·경고 0) | FindFirstObjectByType 경고 1건 수정 후 |
| 자리표시 배치 메뉴 | CLI로 메뉴 실행 | 통과 | 바닥 18.6×18.6, 지점 14개, Console 오류 0 |
| 규칙 29개 | Play 중 script eval로 LobbyState 직접 호출 | 통과 29/29 | 시작 재화·1시간 누적(석재60·금속30)·수령·13시간→12시간 상한(석재720)·업그레이드 비용/레벨/생산량·1.5배 비용·잠금·재화 부족·숲 배정 생산·배정 중 편성 불가·슬롯 이동·인원 상한(3명 규칙)·RED+BLUE·코어 공명·연구 상한 20·레벨업 금속10·상한에서 막힘·점령 +5%·시계 되돌림·저장 파일 재읽기 |
| 저장 유지 | Play 종료 → 재시작 후 값 확인 | 통과 | 재화·채석장 Lv2·연구 1·편성 슬롯 유지 |
| 접근 → 상호작용 버튼 → 패널 | 플레이어를 채석장 앞으로 옮긴 뒤 버튼 호출, 화면 캡처 | 통과 | 한글 표시 확인, 비용 부족 시 버튼 비활성 |
| 벌목 | 나무 앞에서 버튼 4회 | 통과 | 목재 +15(3타), 나무 숨김, 재생 30초 표시 |
| 편성 패널 배치 | 화면 캡처 | 통과 | 버튼 10개가 패널 안에 들어감(높이 64→54 조정 후) |
| 조이스틱으로 직접 걸어서 조작 | 사람 조작 | 미실행 | 사용자 확인 필요(이동은 위치 이동으로 대신 시험) |
| DashTest 씬 질주 동작 | Play | 미실행 | 질주 체크리스트의 Play 확인과 함께 |
- 관찰(고치지 않음): LobbyScene의 Main Camera에 MainCamera 태그가 없다(LobbyUI는 다른 카메라를 찾아 쓰도록 처리). 바닥(Plane) 색이 분홍색으로 보인다. 지점 트리거가 일부 겹쳐(연구소·채석장) 가까운 쪽이 선택된다. 로비 NavMesh는 옛 장애물 기준으로 구워져 있다(로비에 NavMesh 사용자는 현재 없음).

# 20261109-007 부서지는 전투 구조물 시범 (2026-11-09)
- 결정(사용자): 전투 건물·조형물은 섞어 쓰기(부서지는 구조물=3D 저면 모델+픽셀 텍스처, 배경 장식=픽셀 스프라이트). 벽·기둥 같은 부서지는 구조물부터.
- [x] 지시서 `mailbox/orders/20261109-007.order.md`: struct_wall_a(2.0×1.6×0.5)·struct_pillar_a(0.6×2.0×0.6) 크기 임시값, 온전한 모양+조각 4~8개, 텍스처 1유닛당 약 67픽셀, FBX+PNG+.blend, 미리보기 2장.
- [x] 지침 skills/CLAUDE.md §9에 구조물 제작 방식 기록.
- [x] GPT에게 지시서 알림(정형 알림, 2026-11-09 16:22 전송). GPT 작업 시작 확인.
- [x] 납품 done.json(16:32) + GPT 완료 메시지 수신. 자동 검사(개발 담당, Blender 헤드리스 재가져오기): 두 FBX 모두 intact·조각 6개, 조각 합친 범위 = intact 범위(벽 2.0×1.6×0.5, 기둥 0.6×2.0×0.6), 원점 바닥 가운데, UV·재질 1개. 텍스처 134×183·40×194 RGB 8색 불투명. 계층: 루트 아래 intact_state·fractured_state 묶음(게임에서 fractured_state를 처음엔 꺼야 함).
- 관찰: 조각이 거의 직육면체 블록(삼각형 12개씩)이라 돌무더기보다 벽돌 덩어리처럼 갈라진다. 미리보기 렌더는 텍스처가 부드럽게 보간되어 게임의 Point 필터 모습과 다르다.
- [x] 사용자 시범 승인(2026-11-09). 크기는 임시값 유지. 검수 `mailbox/reviews/20261109-007.review.md` 작성.
- [x] 가져오기 도구 `Assets/Editor/StructureImporter.cs`(메뉴 Tools/Wild Golem/우편함 구조물 가져오기): done.json의 kind=destructible_structure 항목 → `Assets/Models/Structures/<이름>/`(FBX·PNG 사본, Point·무압축·밉맵 없음, URP Lit 재질) → `Assets/PreFab/Structures/<이름>.prefab`(루트 BoxCollider, 조각 offset에 Rigidbody 질량 20 임시값 + 메시에 볼록 MeshCollider, fractured_state 꺼짐).
- [x] 파괴 코드 `Assets/Scripts/StructureScripts/DestructibleStructure.cs`: `Break(방향, 부순 쪽)` → 온전한 모양·루트 충돌체 끄기, 조각 켜고 돌격 방향+위+무작위 힘(임시값), 부순 쪽과 조각 충돌 무시, 4초 뒤 조각 숨김(임시값). `onBroken` 이벤트.
- [x] PlayerDash: 돌격 중 DestructibleStructure를 정면으로 들이받으면 부수고 게이지 소모·돌격 종료(벽 충돌과 같은 규칙). 스칠 때는 무시. 파괴 가능 지형 구분 = 컴포넌트 방식(질주 미결정 9번에 대한 개발 판단, 태그·레이어 설정 불필요).
- 검증(2026-11-09): 재컴파일 오류 0, 가져오기 메뉴 오류·경고 0. 프리팹 크기 벽 2.0×1.6×0.5·기둥 0.6×2.0×0.6, 회전 0, Rigidbody·MeshCollider 각 6, 텍스처 Point. DashTest Play에서 앞 3m에 벽 생성 → 돌격 → IsBroken=true, 돌격 종료, 게이지 0 확인. Console 오류 1건은 기존 RedUnit "Enemy 태그 없음". 씬에는 배치·저장하지 않음(실행 중 생성만).
- [x] 벽 파편 약함(사용자 Play 확인: 기둥은 잘 무너지나 벽은 미세한 균열만) → 유니티 물리로 강화(2026-11-09). 조각 속도를 질량과 무관하게 지정(VelocityChange: 돌격 방향 4, 위 2.5, 무작위 1.5 m/s), 무작위 회전(최대 8 rad/s), 부딪힌 지점에 가까울수록 ×1.5~먼 조각 ×0.6, 돌격 속도/10을 0.75~1.5로 제한한 배율. 모두 임시값. PlayerDash가 접촉 지점·현재 속도를 넘김.
  - 확인(DashTest Play): 4.5m 앞 벽 → 조각 6개가 원위치에서 약 0.7~1.7m 앞으로 흩어져 바닥에 쓰러짐(높이 1.6m 조각 포함), 캡처로 확인. Console 오류는 기존 RedUnit 태그 1건.
- [ ] 사용자 확인: 강화 후 벽 파편 느낌, 사라지는 시간(4초), 전투 씬 배치. 부족하면 벽 조각 수 증가(GPT 재작업) 검토
- [x] GPT에게 검수 결과 알림(2026-11-09, 사용자가 문안 승인 후 Codex "Wildgolem 지침 통합"에 전송). GPT 답: 007 검수 확인, 본 작업은 새 지시서를 받은 뒤 진행.
- 참고: BattleScene 카메라는 원근(FOV 60, 프리팹 기준 아래로 약 32° 기울어짐). 정확한 각도는 씬 확인 필요(이번엔 LobbyScene이 열려 있어 열지 않음).

# Tripo CLI 3D 생성 도입 (2026-11-09)
- 결정(사용자): 흐름 GPT 이미지·글 → Tripo CLI → GPT 블렌더 다듬기. Tripo 실행은 GPT(그림 담당). 크레딧은 지시서마다 상한을 적고 사용자 승인.
- [x] 설치 확인: tripo-cli 0.11.2(npm 전역), `tripo doctor` 정상(키: default 프로필, 지역 ov), 잔액 500 크레딧. 키 값은 확인·기록하지 않음.
- [x] 비용 확인(CLI 문서): P2 기본 100~130/회, H3.1 이미지→모델 20~50 + smart_low_poly 10, 텍스처 끄면 낮은 등급.
- [x] 지침 skills/CLAUDE.md §9·§10, AGENTS.md "3D Generation"에 기록.
- [x] GPT에게 바뀐 지침 알림 (2026-11-09, 사용자 승인 후 Codex "Wildgolem 지침 통합"에 전송). GPT 답: 규칙 확인, Tripo 실행은 GPT 담당·지시서별 상한 승인 후 dry-run 확인, 지금은 Tripo 명령 실행·생성 없음
- [ ] 첫 Tripo 사용 지시서(대상·크레딧 상한) — 사용자 승인 필요

# 20261109-008 마족 원안: 가고일 (2026-11-09)
- 결정(사용자): 마족 첫 디자인은 가고일 스타일, 게임은 캐주얼 스타일, 포인트 색은 붉은색+보라색 섞기.
- 개발 판단: RED 골렘 코어와 구분되도록 진홍~보라로 이어지는 색으로 지시. 원안 이미지만 요청하고 Tripo 3D 생성은 원안 확인 뒤 크레딧 상한을 정해 별도 지시서로.
- [x] 지시서 `mailbox/orders/20261109-008.order.md`: 안 3개(concept-01~03) + 추천 1안 3면도(turnaround), 흰 배경·평면 조명·A자 자세, 긴 변 1024px 이상, Tripo 실행 금지.
- [x] GPT에게 지시서 알림(2026-11-09, 사용자 문안 승인 후 Codex "Wildgolem 지침 통합"에 전송). GPT 답: 요구사항 확인, 원안 3개+추천안 3면도 제작 시작, Tripo 미사용, 완료 후 done.json·Claude 알림.
- [x] 납품 done.json(17:52) + GPT 완료 메시지 수신. 자동 검사: sha256 4개 일치, 원안 1254×1254 흰 배경, 3면도 2172×724, Tripo·Blender 미사용(크레딧 0). 86px 축소 미리보기로 가독성 확인. 검수 `mailbox/reviews/20261109-008.review.md`.
- [x] 사용자 안 선택: **01안 그대로**(2026-11-09). 색 섞기·수정 원안 없음.
- [ ] (선택 후) Tripo 3D 생성 지시서 — 크레딧 상한 사용자 승인 필요

# 20261109-009 가고일 마족 3D 모델 (Tripo 첫 사용, 2026-11-09)
- 결정(사용자): 008 01안 그대로, Tripo 크레딧 상한 40.
- [x] 지시서 `mailbox/orders/20261109-009.order.md`: 3면도 multiview(안 되면 정면 1장) → H3.1 smart_low_poly, texture=false, face_limit 3000(임시값), 생성 1회·재시도 없음, dry-run 먼저. 블렌더: 키 1.0m(임시값), 팔 띄우기, 빛나는 부분 재질 분리. 애니메이션·픽셀 렌더는 별도.
- [x] GPT에게 지시서 알림(2026-11-09, 사용자 문안 승인 후 Codex "Wildgolem 지침 통합"에 전송). GPT 답: dry-run으로 예상 비용 먼저 확인, 40 이하일 때만 1회 생성, 블렌더 정리, 재생성 없음, done.json·Claude 알림.
- [x] 납품 done.json(18:26) + GPT 완료 메시지 수신. 검수: dry-run 30·실제 30크레딧(상한 40), 잔액 500→470, sha256 17개 일치, FBX 재가져오기(메시 8·삼각형 2929·높이 1.0m·바닥 원점·재질 6·아마추어 없음), 키 문자열 없음. 검수 `mailbox/reviews/20261109-009.review.md`.
- [x] 사용자 형태·크기 확정(2026-11-09).
- [ ] 다음: 리깅·애니메이션 → 12px 7방향 픽셀 렌더 지시서(모션 종류·프레임 수 사용자 결정)

# 20261109-010 가고일 리깅·애니메이션·12px 7방향 렌더 (2026-11-09)
- 결정(사용자): 모션 = 골렘과 같은 4개(idle 8·walk 12·attack 6·hit 5프레임, 004 타이밍) + 사망(death 8프레임×3tick, 임시값). 리깅 = GPT가 블렌더에서 직접(Tripo 미사용).
- [x] 지시서 `mailbox/orders/20261109-010.order.md`: 5동작×7방향 35장, 칸 86×86, 004와 같은 직교 카메라, 시범 2장(idle_front·attack_front) 먼저.
- [x] GPT에게 지시서 알림(2026-11-09, 사용자 문안 승인 후 Codex "Wildgolem 지침 통합"에 전송). GPT 답: 009 모델에 블렌더 리깅 후 idle_front·attack_front 시범 2장만 렌더하고 멈춰 알림.
- [x] 시범 2장 도착(18:48, trial/ready.txt + GPT 알림). 개발 담당 검사: idle 688×86(8프레임)·attack 516×86(6프레임), 알파 0/255만, 전 프레임 바닥선 84행(골렘과 같음), 칸 경계 닿음 없음(공격 3번째 프레임 오른쪽 여백 4px). 높이 76px vs 와일드골렘 84px(1.0m:1.1m 비율과 맞음). 리깅 13본, Tripo 미사용.
- 관찰: 몸이 골렘보다 훨씬 어둡고 표면에 점 잡음이 있어 골렘의 깔끔한 면 그림과 느낌이 다르다. 어두운 몸에 팔이 묻혀 공격 동작(할퀴기)이 잘 안 보이고, 눈·균열 색도 약하다.
- [x] 시범 1차 사용자 판정: 밝기 수정 후 재시범(2026-11-09). 요청 내용 `mailbox/reviews/20261109-010.review.md`(몸 밝기↑, 점 잡음 제거, 공격 팔 가독성, 눈·균열 대비 재확인, 형태·카메라·뼈대 유지, v1은 trial/v1/ 보관).
- [x] GPT에게 재시범 요청 알림(2026-11-09, 사용자 문안 승인 후 Codex "Wildgolem 지침 통합"에 전송). GPT 답: 1차 시범 trial/v1/ 보관, 몸 밝기·점 잡음·공격 팔·눈·균열 대비 조정 후 시범 2장만 재렌더하고 멈춰 알림.
- [x] 2차 시범 도착·검사: 규격 통과(알파 0/255, 바닥선 84, 경계 여백), 몸 밝기 골렘과 비슷, 색 수 92→75, 공격 팔 보임. 옅은 얼룩·팔/몸 내부 경계 약함은 남음. 검수 기록 갱신.
- [x] 2차 시범 사용자 승인(2026-11-09).
- [x] GPT에게 본 작업 진행 알림(2026-11-09, 사용자 문안 승인 후 Codex "Wildgolem 지침 통합"에 전송). GPT 답: 리뷰·2차 시범 설정 확인, 시범 2장 유지하며 나머지 33장·리깅 FBX·타이밍 파일 제작·검증 후 done.json·Claude 알림.
- [x] 최종 납품 done.json(2026-11-16 15:11) + GPT 완료 메시지 수신. 개발 담당 전수 검사: 35장·273프레임, sha256 일치, 크기·알파·바닥선 84·여백(최소 2px)·타이밍 모두 지시서와 일치, death 마지막 프레임 유지.
- [x] 사용자 확인(2026-11-16): hit 반동 50%·대각선 ±3px 보정은 사용자가 직접 허용함. 009 파일 변경은 기록만.
- (기록) (1) GPT가 "사용자 직접 허용"으로 적은 hit 반동 50%·대각선 ±3px 보정이 실제 사용자 결정인지 (2) 009 gargoyle_a.blend가 11-09 19:19에 수정됨(지시서 위반, 원본 사본 보관됨, 게임 영향 없음).
- [x] Unity 반영(2026-11-18): GolemSpriteImporter에 gargoyle 등록(`Assets/Animation/Gargoyle.controller` 없으면 새로 만들고 기본 상태 idle_front), 모션에 death 추가, done.json `render_offset_pixels`만큼 피벗을 같은 쪽으로 옮겨 대각선 ±3px 되돌림, `hold_last_frame` 동작은 기본 상태로 돌아가는 전환 없음. GolemMotionAnimator에 `PlayDeath()`·`IsDead` 추가(사망 후 대기·걷기·공격·피격으로 바뀌지 않음).
- [x] 가져오기 실행: 시트 35장 → `Assets/Sprites/Golems/gargoyle/`, 클립 35개 → `Assets/Animation/Golems/gargoyle/`, 상태 35개. 컴파일 오류 없음. 경고 6개는 다른 납품의 비스프라이트 항목(struct_*, gargoyle_concept/turnaround) 이름 건너뜀이라 무해.
- [x] 확인: PPU 66.93·Point·칸 86, 피벗 front (43,43)·left/back_left (40,43)·right/back_right (46,43). 클립 길이 idle 1.0·walk 1.5·attack 0.667·hit 0.333·death 1.0초, attack·hit → idle_front 전환, idle·walk·death 전환 없음.
- [x] Play 확인(DashTest, 임시 오브젝트, 저장 안 함): 화면 크기 1.29유닛(와일드골렘 칸과 같음), 이동 시 옆모습 전환, 공격·피격 후 idle_front 복귀, 사망 후 마지막 프레임(death_front_7) 유지·이동해도 유지. 기존 오류 "Tag: Enemy is not defined"만 있음.
- [x] 적 프리팹(2026-11-18): `Assets/PreFab/Enemies/Gargoyle.prefab` = SpriteRenderer + Animator(Gargoyle.controller) + 키네마틱 Rigidbody + BoxCollider(0.5×1.0×0.3, Player와 같음) + NavMeshAgent(baseOffset 0.57, radius 0.3, speed 2, 회전 끔) + GolemMotionAnimator + EnemyCotroller(체력 30).
  - 피해 경로: `LivingEntity.TakeDamage`(PlayerDash.DealDamage와 같음).
  - `EnemyCotroller` 보강: 대상 미지정 시 Player 추적, 사거리 1.2 안이면 멈추고 공격 동작(쿨다운 1초), 피격 시 0.3초 멈춤·피격 동작, 사망 시 에이전트·콜라이더 끄고 사망 동작 후 3초 뒤 삭제.
  - 임시값: 체력 30, 이동 속도 2, 사거리 1.2, 피격 정지 0.3초, 시체 3초.
  - Player는 체력이 없어 가고일 공격은 동작만 재생하고 피해를 주지 않음. 넉백은 아직 없음(질주 작업에서 NavMeshAgent 처리와 함께 결정).
- [x] Play 확인(DashTest, 실행 중 생성, 씬 저장 안 함): NavMesh 위에서 Player 추적·걷기 방향 전환·근접 공격 동작, 피해 10씩 체력 감소·피격 동작, 돌격 충돌로 피해 10·처치 → 사망 마지막 프레임 → 3초 뒤 삭제, 높이 0.65로 Player와 같음.
  - 관찰: 4번 돌격 중 1번은 가고일이 다가오는 중에 돌격이 피해 없이 멈춤(원인 미확인, 재현 안 됨).
  - Console: 기존 "Tag: Enemy is not defined"와 에디터 Burst 컴파일 서버 오류(코드와 무관)만 있음.
- [x] 기존 오류 수정(2026-11-18): RedUnit.Start의 `FindGameObjectWithTag("Enemy")`(태그 미정의 → UnityException)와 Update의 `LookAt` 제거. 대신 `FindNearestEnemy()`(살아 있는 EnemyCotroller 중 가장 가까운 적) 추가. Play 확인: 오류 없음, RED 회전 (0,0,0) 유지, 가고일을 가장 가까운 적으로 찾음. RedUnit.Attack은 대상의 IUnit으로 피해를 주는데 적은 IUnit이 아니고 호출하는 곳도 없어 그대로 둠(파티 공격 구현 때 피해 경로 결정 필요).
- [x] 적 이동 8방향 제한(2026-11-18, 사용자 요청): EnemyCotroller가 NavMeshAgent는 경로 계산에만 쓰고(isStopped), 다음 꺾임점 방향을 45° 단위로 맞춰 `NavMeshAgent.Move`로 옮긴다. 경계 각도 떨림 방지 여유각 `directionHysteresis` 10°(임시값) — 목표가 비스듬하면 계단 모양으로 걷는다.
  - Play 확인(DashTest, 가고일 2마리, 매 프레임 기록): 이동 프레임 1,000여 개 모두 45° 배수(GA 90°·45°, GB −45°·0°), Player 앞에서 정지, 오류 없음. 이전에는 67.4°·−39.8°로 직선 이동했음.
  - 미확인: DashTest NavMesh가 평면뿐(정점 16개, 구조물이 구워져 있지 않음)이라 벽을 돌아가는 경로는 확인 못 함. 다른 적과 겹침 회피(avoidance)는 Move에 적용되지 않음.
- [ ] 남은 일: 전투 씬 배치·생성기, Player 체력, 넉백, 체력 등 수치 확정. 방향 전환 때 발 위치·사망 연출 느낌은 사용자 확인 필요.
