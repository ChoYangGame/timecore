# TimeCore — AI 활용 로그

> 제출물 4번(AI 활용 기술 문서)과 5번(팀원 롤 기술서)의 원재료.
> 매일 append. 마지막 날 몰아쓰면 반드시 빠진다.

---

## 사용 도구 목록

| 도구 | 용도 | 비고 |
|---|---|---|
| Claude (Opus 5) | 기획 검토, 스코프 결정, 설계 상담 | 웹 채팅 |
| Claude Code | 코드 생성·수정, 디버깅 | 로컬 |
| Unity MCP | 에디터 상태 조회, 콘솔 에러 자동 수정 | 사용 여부 미정 |
| | | |

---

## 일자별 기록

### 2026-07-31 | 프로젝트 생성 및 배포 파이프라인 확보

- **도구**: Claude (웹 채팅)
- **작업**: Unity 프로젝트 생성 설정 검토. Universal 2D / 6000.3.20f1 LTS 확정.
  빈 프로젝트 상태에서 Web 플랫폼 스위칭 → 배포 스모크 테스트 우선 수행 결정.
- **프롬프트 원문**: "unity 프로젝트 생성을 해야해 이 상황인데 어떻게 설정하고 진행하는게 좋을까"
- **AI 산출물 vs 수정**: (작성)
- **담당**: 개발

### 2026-07-31 | 예선 스코프 축소 결정

- **도구**: Claude (웹 채팅)
- **작업**: D-10 기준 역산 결과 4시대 → **2시대(원시·중세)** 로 축소.
  맵 기믹 삭제(평평한 맵 사양과 충돌), 아이템 슬롯 4종 삭제(증강과 기능 중복).
  1회 플레이 목표 5분.
- **프롬프트 원문**: "지금 프로젝트가 10일 남았어"
- **AI 산출물 vs 수정**: (작성)
- **담당**: 개발 + 디자인 합의

---

## 외부 에셋 / 오픈소스 출처

> 다운로드 즉시 이 표에 추가할 것. 나중에 복원 불가.

| 항목 | 출처 URL | 라이선스 | 용도 |
|---|---|---|---|
| | | | |

---

## AI 생성 에셋

> AI로 만든 스프라이트·사운드·텍스트도 출처 기재 대상.

| 항목 | 생성 도구/모델 | 프롬프트 | 후처리 |
|---|---|---|---|
| | | | |

---

## 팀원별 담당 (제출물 5번 원재료)

| 팀원 | 역할 | 실제 구현 영역 | 협업 방식 |
|---|---|---|---|
| | 개발 | | |
| | 디자인 | | |

---

### 2026-08-01 | Vercel 첫 배포 및 dist 분리 (deploy.bat 재작성)

- **도구**: Claude Code (Sonnet 5)
- **작업**: 첫 배포 시 Build 폴더명(대문자)으로 인한 프로젝트명 생성 실패 확인. vercel.json `name` 필드는 deprecated라 대신 `dist/` 배포 폴더 분리 + `.vercel` 백업·복원 로직으로 deploy.bat 재작성. `vercel link --yes --project timecore`로 비대화형 링크 후 배포 성공, curl -I로 `.data.br`(content-encoding: br) / `.wasm.br`(content-type: application/wasm) 헤더 검증 완료.
- **프롬프트 원문**: "vercel.json의 name 속성은 deprecated라 안 되니까 2번 방식으로 가되, Unity 재빌드 때 .vercel이 날아가는 문제를 막기 위해 배포 폴더를 분리해줘."
- **AI 산출물 vs 수정**: deploy.bat/.gitignore 변경 전량 AI 작성. 스크립트 로직(백업 이동 순서, dist 구조)은 사용자가 직접 지정, AI는 그대로 구현·실행·검증만 수행
- **담당**: 개발

### 2026-08-01 | 코어 루프 씬 구성 (Player/Enemy/Bullet/Spawner/Camera)

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: Assets/Scripts 7종을 SampleScene에 배치. 흰 사각형 스프라이트 1장을 생성해 Player(#6FD8E0)/Enemy(#A8322D)/Bullet(#F2EFE9) 색조로 재사용. Bullet·Enemy 프리팹화, Player에 AutoAimShooter→Bullet 연결, Main Camera에 CameraFollow + 배경색 #1B2436 적용. Play 모드에서 이동/스폰/사격/사망 전부 정상 확인.
- **프롬프트 원문**: "Player: 흰 사각형, PlayerMove + Health + AutoAimShooter, BoxCollider2D, Rigidbody2D(Kinematic), 태그 Player / Enemy 프리팹: Enemy + Health, BoxCollider2D(trigger), Rigidbody2D(Kinematic), 태그 Enemy / Bullet 프리팹: Bullet + BoxCollider2D(trigger) / Spawner: 빈 GameObject + EnemySpawner / Main Camera: CameraFollow, 배경색 #1B2436 / 색상: 플레이어 #6FD8E0, 적 #A8322D, 총알 #F2EFE9"
- **AI 산출물 vs 수정**: 씬 구성 전량 AI(Unity MCP RunCommand)가 생성. Spawner의 EnemySpawner 부착만 아래 실패로 사용자가 직접 붙이고 최종 검증(Play 모드)도 사용자가 수행.
- **담당**: 개발

### 2026-08-01 | 잘 안 된 시도: EnemySpawner가 MCP 경로 5개에서 전부 인식 실패

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: EnemySpawner만 RunCommand 컴파일타임 참조(x2)·string AddComponent·Type 리플렉션(x2)·Unity_ManageGameObject 문자열 조회까지 5개 경로 전부 "타입을 찾을 수 없음"으로 실패. 같은 폴더의 다른 6개 스크립트는 전부 정상 인식됨. Ctrl+R 재요청과 재컴파일로도 해결 안 됐음 — Auto Refresh 환경이라 Ctrl+R 자체가 무효였고, 실제 원인은 Library/ScriptAssemblies의 stale DLL 캐시였음.
- **프롬프트 원문**: "EnemySpawner를 MCP 5개 경로로 붙이려다 전부 실패 / 원인은 Library/ScriptAssemblies의 stale DLL 캐시 / Ctrl+R과 타임스탬프 갱신으로는 해결 안 됨 / Unity 종료 후 ScriptAssemblies 삭제 → 재시작으로 해결"
- **AI 산출물 vs 수정**: 진단과 5회 우회 시도는 AI가 수행했으나 전부 실패. 근본 원인 파악, 에디터 재시작을 통한 해결, Spawner 컴포넌트 최종 부착은 사용자가 직접 수행.
- **담당**: 개발

### 2026-08-01 | Web 빌드 및 Vercel 프로덕션 배포 (코어 루프 반영)

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: Unity_GetConsoleLogs로 에러 0건 확인 후 BuildPipeline.BuildPlayer(WebGL)로 Build/ 재생성, dist/index.html에 `{{{ }}}` 매크로 잔존 없음 확인, deploy.bat 실행해 프로덕션 배포 완료(https://timecore-chi.vercel.app).
- **프롬프트 원문**: "첫 작업: Web 빌드 → deploy.bat 실행 → 배포 / 빌드 전에 Unity 콘솔에 에러 없는지 Unity_GetConsoleLogs로 확인 / 빌드 완료 후 dist/index.html에 {{{ }}} 매크로가 남아있지 않은지 확인 / 배포 후 URL 알려주면 내가 브라우저에서 직접 확인할게 / DLL 타임스탬프 폴링은 하지 마."
- **AI 산출물 vs 수정**: 빌드·검증·배포 전량 AI가 직접 수행. Unity MCP RunCommand가 Sentis 패키지 셰이더 경고 30건을 이유로 빌드를 "failed"로 오보고했으나, 에러 0건과 Build/ 산출물 타임스탬프·용량 변화로 실제 빌드 성공을 확인.
- **담당**: 개발

### 2026-08-01 | HUD 구성 (HP/EXP바·레벨·타이머·킬수) + GameManager

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: TMP Essential Resources 임포트 후 HUD_Canvas(ScaleWithScreenSize, 1920x1080, Match 0.5) 생성. HP바(좌하단)/EXP바(상단 전체)/레벨(EXP바 좌측)/타이머(상단 중앙)/킬수(우상단) 배치. GameManager 싱글톤이 생존시간·킬수 관리, HealthBarUI가 Health.OnDamaged/OnDeath 구독해 HP바 실시간 갱신, Enemy.HandleDeath에 킬 등록 1줄 추가.
- **프롬프트 원문**: "Canvas 하나에 아래를 전부 만들어줘 (TextMeshPro 사용): HP 바 — 화면 좌하단 / 경험치 바 — 화면 상단 가로 전체 / 레벨 표시 — 경험치 바 좌측 / 생존 시간 타이머 — 화면 상단 중앙 / 킬 수 — 화면 우상단 / Canvas Scaler는 Scale With Screen Size, 1920x1080 기준, Match 0.5. / 색상: 배경 #1B2436, 텍스트 #F2EFE9, 강조·경험치 #6FD8E0, HP #A8322D. / Health.cs와 연동해서 HP 바가 실시간으로 줄어들게 해줘. / 타이머와 킬 수를 관리할 GameManager.cs도 같이 만들어줘 (싱글톤, 씬에 빈 오브젝트로 배치). / 스크립트가 타입으로 인식 안 되면 바로 나에게 알려줘."
- **AI 산출물 vs 수정**: 스크립트 3종(GameManager/HealthBarUI/HudController) 및 씬 하이어라키 전량 AI가 RunCommand로 생성. 레벨/경험치 증가 로직은 아직 없어 GameManager.SetExp()만 열어두고 기본값(Lv.1, 0%)으로 둠 — 성장 시스템은 별도 요청 필요.
- **담당**: 개발

### 2026-08-01 | 버그: HP바가 시작부터 빈 상태로 보임 (Awake 순서 경합)

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: 사용자가 "HP/EXP바 변화 없음" 보고 → RunCommand로 Play 모드 진입 후 실측: 풀피 상태인데 fillAmount=0으로 초기화됨. 원인은 HealthBarUI.Awake()가 Health.Awake()(CurrentHp 설정)보다 먼저 실행되는 순서 경합. Awake→Start로 옮겨 해결(Unity가 모든 Awake 종료 후 Start를 호출하는 걸 보장). Play 모드 재진입해 풀피=1.0, 데미지 30 적용 시 0.7로 정상 반응 확인.
- **프롬프트 원문**: "exp바랑 hp바가 변화가 없는것 같은데"
- **AI 산출물 vs 수정**: 진단(Play 모드 강제 진입, TakeDamage 강제 호출로 실측)과 수정 전량 AI가 수행. EXP바는 별도 버그가 아니라 레벨업 시스템 자체가 아직 없어 0%로 고정된 정상 상태임을 사용자에게 별도 안내.
- **담당**: 개발

### 2026-08-02 | 경험치·레벨업·증강 시스템 구현

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: ExpOrb(흡수 반경 1.5, 속도 8, sqrMagnitude 거리계산) + 프리팹, Enemy 사망 시 드롭 연결. GameManager에 currentExp/level/expToNextLevel(5+(lv-1)*3), AddExp/OnLevelUp 추가. AugmentData(SO) 3종 + AugmentManager(레벨업 시 timeScale=0, 카드 3장 표시, 선택 시 PlayerMove/AutoAimShooter 배율 누적 적용 후 재개) + AugmentCardUI + 카드 3장 UI(InputSystemUIInputModule 기반 EventSystem 포함) 구성.
- **프롬프트 원문**: "좋아. 이제 경험치·레벨업·증강 시스템을 붙이자. 오늘의 핵심 작업이야." (이하 1~4번 섹션 스펙 원문 전체 — ExpOrb/GameManager 확장/증강 시스템/증강 선택 UI 상세 스펙, "각 단계 끝나면 컴파일 에러 확인하고, 타입 인식 안 되면 바로 알려줘"까지 포함)
- **AI 산출물 vs 수정**: 스크립트 6종(ExpOrb/AugmentData/AugmentManager/AugmentCardUI + GameManager·Enemy·AutoAimShooter 확장) 전량 AI 작성. Play 모드에서 적 처치→오브 흡수→레벨업→카드 표시→선택→재개 전체 흐름과 배율 누적(1.25×1.25=1.5625)을 실측 검증.
- **담당**: 개발

### 2026-08-02 | 버그: 증강 카드 한글 텍스트가 □로 깨짐 + 카드 테두리 미표시

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: 기본 TMP 폰트(LiberationSans SDF)가 한글을 지원하지 않아 카드 텍스트가 전부 tofu box로 깨짐. 사용자 확인 후 Windows 맑은 고딕에서 실제 사용 글자(약 47자)만 뽑아 Static SDF 서브셋 폰트를 생성(원본 13MB TTF는 굽고 나서 참조 해제·삭제, 빌드 용량 영향 최소화)해 카드 텍스트에 적용. 추가로 카드 배경색이 오버레이와 같은 색이라 카드 자체가 안 보이고 Outline 컴포넌트가 단색 사각형엔 테두리로 작동하지 않는 것도 발견해 프레임+Fill 2단 구조로 교체. 스크린샷으로 최종 확인.
- **프롬프트 원문**: (사용자 지시 없음 — Play 모드 콘솔 경고 확인 중 AI가 자체 발견, 폰트 서브셋 추가 여부만 AskUserQuestion으로 확인받음: "한글 서브셋 폰트 에셋 추가 (권장)" 선택됨)
- **AI 산출물 vs 수정**: 진단·폰트 생성·카드 구조 수정 전량 AI 수행. Font Asset Creator API를 Static 모드로 바로 호출하면 글리프 추가가 거부돼 Dynamic으로 생성 후 굽고 Static으로 전환하는 우회가 필요했음.
- **담당**: 개발
