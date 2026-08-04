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
| Malgun Gothic (맑은 고딕) | Windows 10/11 번들 폰트 (C:\Windows\Fonts\malgun.ttf) | Microsoft 번들 폰트 EULA — 임베딩 허용 범위 미확인, 재배포용 아님 | ~~TMP 한글 서브셋(KoreanSubset SDF) 생성 소스~~ — 2026-08-02 라이선스 문제로 Pretendard로 교체, 프로젝트에서 제거됨 |
| Pretendard | https://github.com/orioncactus/pretendard | SIL OFL 1.1 | UI 폰트 |

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

### 2026-08-02 (D-7) | 웨이브 시스템 + 원시시대 보스 "고대의 포식자"

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: WaveManager(25초/웨이브, 스폰간격×0.9 최소0.4, 적체력+15%/웨이브, 6웨이브 보스) + Boss(추적/돌진(3초 대기→1초)/충격파(8발) 패턴, HP4000, 사망 시 오브10개) + BossProjectile + 보스 HP바·이름표·등장 배너.
- **프롬프트 원문**: "오늘 핵심은 보스야. 웨이브는 보스 등장을 위한 최소 구조만 만들면 돼." (이하 1~5번 섹션 스펙 전체, "각 단계 끝나면 컴파일 에러 확인하고, 막히면 바로 알려줘"까지 포함)
- **설계 판단과 근거**: 체력 스케일링은 Enemy 프리팹을 안 건드리고 EnemySpawner.OnEnemySpawned(신규 이벤트)로 스폰 직후 Health.SetMaxHp(신규 메서드)를 호출하는 방식 채택 — 프리팹 기본값·인스펙터 조정력 유지. 보스는 Enemy와 같은 "Enemy" 태그를 재사용해 AutoAimShooter/Bullet의 기존 타게팅에 자동으로 걸리게 함(새 분기 불필요).
- **검증 방법**: ForceSpawnBoss()(B키와 동일 경로) 호출 후 4초 대기해 돌진(위치 17→5.38)·충격파(투사체 7~8개) 둘 다 발동 실측. HP 4000에 1000 데미지 시 fillAmount 0.9475→0.6975 확인. 처치 시 오브 10개 생성 → 플레이어를 오브 클러스터로 이동시켜 흡수 → Lv.1→2 레벨업과 증강 카드 자동 표시까지 실측.
- **AI 산출물 vs 사용자 개입**: 스크립트 5종(WaveManager/Boss/BossProjectile/BossHpUI/BossBannerUI) + 프리팹 2종(Boss/BossProjectile) + 기존 스크립트 2종(Health/EnemySpawner) 확장 전량 AI가 생성·배선·검증. "고대의 포식자" 한글 6자가 기존 서브셋 폰트에 없어 사전 확인 후 재굽기 처리.
- **담당**: 개발

### 2026-08-02 (D-7) | 폰트 교체: 맑은 고딕 → Pretendard (라이선스 문제)

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: 한글 서브셋 폰트의 소스를 맑은 고딕(Windows 번들)에서 Pretendard(SIL OFL 1.1)로 전면 교체. 기존 53자 + Timer/Kills/Level/WAVE 배너용 라틴·기호 15자를 합쳐 68자 Static SDF로 재생성. 씬의 TMP 텍스트 11개(HUD 3종·증강 카드 6종·보스 이름표·WAVE 배너) 전부 새 폰트로 교체하고, 맑은 고딕 기반 KoreanSubset SDF 에셋은 삭제. docs/licenses/Pretendard-OFL.txt에 라이선스 전문 동봉.
- **프롬프트 원문**: "맑은 고딕은 Windows 번들 폰트라 임베딩·재배포 권한이 없어서 교체한다. Pretendard는 SIL OFL 1.1이라 상용·임베딩·재배포 모두 허용된다." + 1~7번 작업 지시 전체
- **설계 판단과 근거**: 맑은 고딕을 폰트 소스로 쓴 시점에 AI가 출처 표에 "임베딩 허용 범위 미확인"이라고 선제적으로 플래그했었음 — 제출물 4번은 라이선스 명시가 의무라 그냥 넘어갈 수 없는 사안이었고, 확인 결과 실제 재배포 권한이 없어 교체 결정. TMP_FontAsset.CreateFontAsset은 Font 오브젝트만 받고 Assets/ 밖 경로를 직접 구울 공개 API가 없어, 맑은 고딕 때와 동일하게 임시 복사→굽기→즉시 삭제 방식을 사용함(사용자가 요청한 "Assets 복사 금지"와는 다른 처리라 진행 전 별도로 알림).
- **검증 방법**: Play 모드에서 "고대의 포식자" 보스 이름표와 증강 카드 3장 전부 콘솔 경고(tofu box) 없이 표시되는지 확인.
- **AI 산출물 vs 사용자 개입**: 폰트 생성·전체 텍스트 교체·구 에셋 삭제·라이선스 문서화 전량 AI 수행. 폰트 파일(Pretendard-Regular.ttf)과 라이선스 원문(LICENSE.txt)은 사용자가 다운로드해 제공.
- **담당**: 개발

### 2026-08-02 (D-7) | 보스 배너 · 증강 카드 표시 우선순위 정리

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: 증강 카드(우선) vs 보스 등장 배너가 동시에 뜨는 문제 해결. 카드 표시 중 보스가 등장하면 배너 요청을 WaveManager에 보류했다가 카드 선택 후 재개 시점에 띄움. 반대로 배너 표시 중 레벨업이 발생하면 AugmentManager가 배너를 즉시 종료(BossBannerUI.CancelImmediate)하고 카드를 띄움. BossBannerUI의 표시/페이드 타이머를 WaitForSeconds→WaitForSecondsRealtime, Time.deltaTime→Time.unscaledDeltaTime으로 교체.
- **프롬프트 원문**: "우선순위: 증강 카드 > 보스 배너 / 증강 카드가 표시 중이면 보스 배너를 대기시켰다가 카드 선택 후 재개될 때 배너를 띄운다 / 반대로 배너 표시 중에 레벨업이 발생하면 배너를 즉시 종료하고 카드를 띄운다 / 증강 카드는 Time.timeScale=0으로 게임을 멈추니까 그 상태에서 배너 타이머가 unscaledDeltaTime으로 도는지도 확인해줘."
- **설계 판단과 근거**: AugmentManager에 IsShowing/OnPanelClosed를 추가해 WaveManager가 "카드가 떠 있는가"를 물어보고 배너를 보류(pending text 캐싱)하는 방향을 택함 — WaveManager가 보스·배너 오케스트레이션을 이미 갖고 있어 새 이벤트 흐름 대신 기존 구조에 얹었다. 역방향(배너→카드 인터럽트)은 우선순위가 높은 AugmentManager가 낮은 쪽(BossBannerUI)을 직접 제어하는 게 자연스러워 AugmentManager.HandleLevelUp에서 bossBanner.CancelImmediate()를 먼저 호출하도록 함.
- **검증 방법**: Play 모드에서 (1) 카드 표시 중 ForceSpawnBoss() 호출 → 배너 비활성 유지 확인 → 카드 선택 후 배너 자동 표시 확인. (2) 배너 표시 중(active=True) 같은 프레임에 레벨업 발생 → 배너 즉시 비활성, 카드 즉시 활성 확인. (3) 배너를 띄운 뒤 Time.timeScale=0으로 강제 고정 후 2.6초 대기 → 배너가 정상적으로 페이드 완료·비활성화됨을 실측(unscaledDeltaTime 동작 증명).
- **AI 산출물 vs 사용자 개입**: 스크립트 3종(AugmentManager/BossBannerUI/WaveManager) 수정 및 씬 참조 배선(WaveManager↔AugmentManager↔BossBanner), 3가지 시나리오 실측 전량 AI 수행.
- **담당**: 개발

### 2026-08-02 (D-7) | 증강 5종 추가 (총 8종) — 관통·다중사출·무적 구조 확장

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: 시간 왜곡(경험치 흡수 반경+50%)·균열 복구(최대HP+20%, 즉시 그만큼 회복)·관통 코어(관통 누적)·다중 사출(발사수 누적, 최대 5발 상한)·위상 이동(이동속도+10%, 피격 시 0.5초 무적+스프라이트 점멸) 추가. AugmentType enum 8종 체제로 확장. 새 증강 5개 에셋 생성 및 AugmentManager 풀(3→8) 배선.
- **프롬프트 원문**: "증강 5개를 추가해줘. 기존 3종과 같은 방식으로: [5종 스펙] / 관통과 다중 사출은 Bullet/AutoAimShooter 구조 변경이 필요할 수 있어. 기존 3종처럼 단순 배율이 아니니까 구현 방식 먼저 알려주고 진행해줘. / 다중 사출이 누적되면 총알 수가 급증하니 상한을 두자 (최대 5발). / 무적 시간은 Health에 무적 플래그를 추가하는 방식으로. 피격 시 스프라이트 깜빡임도. / 새 한글이 추가되니 Pretendard SDF 서브셋 갱신 필요."
- **설계 판단과 근거**: 관통은 Bullet에 PierceRemaining(발사 시 AutoAimShooter가 채움)을 둬서 히트마다 감소만 시키고 0일 때만 Destroy — 기존 즉시파괴 동작과 하위호환. 다중사출은 ExtraShots를 무제한 누적시키되 발사 "시점"에 `Mathf.Clamp(1+ExtraShots,1,5)`로 상한을 걸어 카운터 자체는 안 건드리고 실제 총알 수만 절대 못 넘게 함(저사양 보호). 무적은 Health가 Player/Enemy/Boss 공용이라 hitInvincibilityDuration을 0(비활성) 기본값으로 둬 다른 유닛엔 영향 없게 opt-in 처리. 균열 복구는 기존 SetMaxHp(풀회복)와 요구사항이 달라(증가분만큼만 회복) IncreaseMaxHp를 신규로 분리.
- **검증 방법**: Play 모드에서 8개 증강을 전부 소진할 때까지 레벨업을 반복해 각각 뽑아 적용 — 8/8 커버리지, 수치 전부 공식과 정확히 일치(예: 균열복구 CurrentHp가 풀피가 아니라 델타(+20)만큼만 증가). 관통·다중사출은 별도로 적 HP를 100000으로 올려 죽지 않게 한 뒤 관찰: 총알 645발=129볼리×5발로 상한 정확히 확인, PierceRemaining=0인데 파괴 안 되고 생존한 총알 440발로 관통 확인. 무적은 연속 데미지 2회 중 2번째가 막히는 것 실측.
- **AI 산출물 vs 사용자 개입**: 스크립트 7종 수정(Health/Bullet/AutoAimShooter/GameManager/ExpOrb/AugmentData/AugmentManager) + 에셋 5종 생성 + Pretendard SDF 재굽기(68→100자) 전량 AI 수행. 구현 방식은 사전 설명 후 그대로 승인받아 진행.
- **담당**: 개발

### 2026-08-02 (D-7) | 버그: HP·EXP·보스 HP바가 배포본에서 안 바뀜 — 진짜 원인은 Source Image 누락

- **도구**: Claude Code (Sonnet 5) / Unity MCP / Claude in Chrome
- **작업**: 배포된 빌드에서 HP/EXP/보스 HP바가 항상 꽉 차 보이고 실제 수치와 무관하게 안 바뀌는 버그. 실제 원인은 세 바의 Fill Image에 Source Image(스프라이트)가 None이라 Image Type=Filled여도 fillAmount 기반 메시 크롭이 전혀 동작하지 않았던 것 — 항상 전체 사각형으로 렌더링됨. 세 Fill과 Background에 프로젝트 기존 "Square" 스프라이트를 할당(+ Filled/Horizontal/Left 재설정)해 해결.
- **프롬프트 원문**: "fillAmount=0인데 화면이 차 보인다는 건, 보이는 게 Fill이 아니라 Background라는 뜻이야... [1차 가설, 결과적으로 오답]" 이후 "원인 찾았어. EXPBar/Fill과 HPBar/Fill의 Image에 Source Image가 None이라 Image Type이 아예 표시되지 않고 Filled 모드가 동작하지 않아... UISprite를 할당해줘... 셰이더 에러와 HDR은 이 문제와 무관하니 원복하고 더 파지 마."
- **설계 판단과 근거**: AI는 fillAmount·참조·씬 데이터가 전부 정상인데도 렌더링만 깨지는 걸 URP/WebGL 셰이더 호환성 문제(HDR, CoreCopy 셰이더 에러)로 오판해 상당 시간을 허비했다 — 실제로는 무관했음. 사용자가 에디터에서 Source Image 필드를 직접 눈으로 확인해 진짜 원인을 특정. Unity 내장 UISprite 대신 프로젝트의 기존 "Square" 스프라이트를 써서 모서리가 둥글어지는 부작용 없이 각진 디자인을 유지.
- **검증 방법**: (1) Play 모드에서 fillAmount=0.5 강제 후 스크린샷으로 절반만 차는 것 확인 — 첫 시도는 Play 모드가 우연히 일시정지(EditorApplication.isPaused)돼 있어 오래된 프레임만 캡처되는 바람에 헛다리를 짚었고, 재개 후 재캡처해 실제 확인함. (2) Play 모드 중 적용한 스프라이트 변경이 Play 모드 종료 시 씬에 저장되지 않는다는 걸 뒤늦게 발견 — Edit 모드로 나와서 다시 적용. (3) 최종적으로 실제 배포 URL에서 게임을 플레이하며 EXP바가 Kills 0→1로 늘어날 때 실제로 채워지는 것을 스크린샷으로 확인.
- **AI 산출물 vs 사용자 개입**: 최초 두 가설(프레임+Fill 참조 어긋남, WebGL 셰이더/HDR 호환성)은 AI가 냈으나 전부 오답이었음. Source Image 누락이라는 정확한 근본 원인은 사용자가 에디터를 직접 열어 찾아냄. 수정 구현과 3단계 검증, 재빌드·재배포는 AI가 수행.
- **담당**: 개발

### 2026-08-02 (D-6) | 시대 전환 시스템 구현 (원시 → 중세)

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: EraManager(빈 GameObject) 신규 작성. WaveManager.OnBossDefeated 구독 → 1초 대기(증강 카드 표시 중이면 닫힐 때까지 추가 대기) → 화면 풀블랙 페이드아웃(0.8초, unscaledDeltaTime) → 배경색·적 프리팹·보스 색상 교체 + 웨이브 리셋 + 플레이어를 맵 왼쪽으로 이동 + 잔여 적/투사체/경험치 오브 정리 → 페이드인(0.8초) → BossBannerUI 재사용해 시대 배너 표시. WaveManager에 ConfigureBoss()/ResetForNewEra(), EnemySpawner에 SetEnemyPrefab() 추가. MedievalEnemy 프리팹(Enemy 복제, 색 #8A6D3B, HP 30, 이동속도 2.6) 신규 제작. HUD_Canvas 최상단에 EraFade(풀스크린 검정 Image, Source Image=Square, raycastTarget=false) 배치. N키 즉시 전환 디버그 지원.
- **프롬프트 원문**: "오늘 목표: 시대 전환 (원시 → 중세)" (이하 1~5번 섹션 스펙 전체 — EraManager 설계, 시대별 설정, 중세 적 프리팹, 전환 연출, 디버그 키 상세 포함, "UI 관련 작업 시 Image에 Source Image가 할당돼 있는지 반드시 확인해줘"까지 포함)
- **설계 판단과 근거**: 시간 관계상 중세 보스는 별도 프리팹 없이 WaveManager가 스폰 직후 SpriteRenderer.color만 덮어쓰는 방식(ConfigureBoss로 이름·색 주입)을 택함 — 프리팹 구조 변경 없이 시대별 외형만 교체. ClearBattlefield는 FindObjectsByType로 Enemy/BossProjectile/ExpOrb를 전환 시 1회만 일괄 Destroy — 매 프레임 호출이 아니라 성능 영향 없음. 페이드 타이머는 증강 카드가 Time.timeScale=0을 걸 수 있어 기존 BossBannerUI와 동일하게 unscaledDeltaTime 기반으로 작성.
- **검증 방법**: Play 모드에서 EraManager.ForceEraTransition()을 직접 호출해 실측 — 배경색이 RGBA(0.180,0.165,0.220,1)로 바뀌고, 새로 스폰된 적이 MedievalEnemy(Clone)/색 #8A6D3B/HP 30으로 교체되고, WaveManager.CurrentWave가 1로 리셋되며, 레벨(1)·킬수(계속 누적)는 그대로 유지됨을 확인. 강제 소환한 보스가 중세 색상(#8A6D3B)으로 나오는 것과, 그 보스에게 치명타를 줬을 때(2번째=중세 보스이므로) 추가 시대 전환 없이 EraManager.OnGameClear 이벤트만 발행되는 것을 로그로 확인.
- **AI 산출물 vs 사용자 개입**: EraManager.cs 신규 작성, WaveManager/EnemySpawner 확장, MedievalEnemy 프리팹 제작, EraFade UI 배치, 씬 배선, Play 모드 실측 검증까지 전량 AI가 수행.
- **담당**: 개발

### 2026-08-02 (D-6) | 잘 안 된 시도: 참조 주입 도구 버그 + TMP 폰트 아틀라스 확장이 인터랙티브 다이얼로그에 막힘

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: (1) EraManager 필드 7개를 Unity_ManageGameObject의 `{"find":..., "component":...}` 참조 주입 문법으로 연결 시도 → 매번 "Unexpected token type 'EndObject'" JSON 역직렬화 오류로 실패("Property not found"라는 오해를 유발하는 오진단 메시지까지 표시). RunCommand로 SerializedObject.FindProperty(...).objectReferenceValue를 직접 대입하는 방식으로 우회해 해결. (2) "MEDIEVAL ERA"/"PRIMITIVE ERA" 표시에 필요한 대문자 M/D/I/R/T를 Pretendard SDF에 추가하려 TMP_FontAsset.CreateFontAsset/TryAddCharacters를 호출했으나 매번 "User interactions are not supported for MCP tool calls"로 즉시 차단됨. 단독 TTF 임포트는 성공하지만 TMP 폰트 엔진 초기화 자체가 인터랙티브 다이얼로그를 띄우는 것으로 확인, MCP로는 우회 불가 — 재부팅 후 첫 폰트 엔진 사용이라 발생했을 가능성.
- **프롬프트 원문**: 해당 없음 (작업 중 AI가 자체 발견 및 진단)
- **설계 판단과 근거**: (1)은 GameObject.Find가 비활성 오브젝트(BossBanner 등)를 못 찾는 것과는 별개의, 도구 자체의 JSON 파싱 버그로 판단 — 이후 참조 주입은 RunCommand+SerializedObject를 기본으로 쓴다. (2)는 실패한 4회 시도(SerializedObject 조작, 신규 폰트 애셋 생성, 최소 재현 테스트 2회) 중 남긴 임시 파일(_TempPretendard*.ttf)을 전부 정리했고, 기존 폰트 애셋이 Static/1024x1024/100자로 손상 없이 원상태임을 재확인함.
- **검증 방법**: 실패 후 Pretendard SDF.asset의 atlasPopulationMode/atlasWidth/atlasHeight/characterTable.Count를 재조회해 변동 없음을 확인. Assets/Fonts/ 폴더에 임시 파일이 남아있지 않음을 확인.
- **AI 산출물 vs 사용자 개입**: 진단과 우회 시도 전부 AI가 수행. 폰트 재굽기는 Unity 에디터에서 사람이 다이얼로그를 한 번 직접 클릭해야 풀리는 문제라 사용자 개입이 필요 — N키 디버그 전환 시 "PRIMITIVE ERA"/"MEDIEVAL ERA" 배너 텍스트가 일부 글자(M/D/I/R/T)만 tofu box로 보일 수 있음.
- **담당**: 개발

### 2026-08-02 (D-6) | 시대 배너 텍스트 영문 → 한글 교체 ("원시 시대"/"중세 시대")

- **도구**: Claude Code (Sonnet 5) / Unity MCP
- **작업**: EraManager의 eraLabel을 "PRIMITIVE ERA"/"MEDIEVAL ERA"에서 "원시 시대"/"중세 시대"로 교체(스크립트 기본값 + 씬 인스턴스 값 모두). 교체 전 폰트에 필요한 글자(중/세/시/대/원/공백)가 있는지 문자 코드로 전수 대조.
- **프롬프트 원문**: "배너 텍스트를 '중세 시대', '원시 시대'로 바꿔줘. 이 글자들이 기존 SDF 폰트에 있는지 먼저 확인하고, 있으면 교체해줘."
- **설계 판단과 근거**: 영문 라벨 전환 당시 필요했던 M/D/I/R/T 문제는 한글 교체로 자연히 해소됨(더 이상 필요 없음). 대신 "세"와 "원" 두 글자가 신규로 없음을 확인 — 직전 로그의 폰트 다이얼로그 차단 이슈가 동일하게 재현되어 추가 불가.
- **검증 방법**: `font.characterTable`을 순회해 "중세시대원 " 6글자를 유니코드 단위로 대조 — 중/세/시/대/공백 중 세만 없고, 대신 원도 없음을 확인(있는 글자: 중, 시, 대, 공백 / 없는 글자: 세, 원).
- **AI 산출물 vs 사용자 개입**: 글자 대조·스크립트·씬 값 교체는 AI가 수행. "세"/"원" 두 글자는 여전히 다이얼로그 차단으로 AI가 못 구움 — 이전 M/D/I/R/T와 같은 방법으로 사용자가 직접 2글자만 추가하면 됨. 그 전까지는 배너의 "세"/"원" 위치만 tofu box로 보임.
- **담당**: 개발

### 2026-08-02 (D-6) | 사고: 폰트 재굽기가 타입을 바꿔 씬의 TMP 참조 11개 전멸 → 복구

- **도구**: Claude Code (Sonnet 5 → Opus 5) / Unity MCP
- **작업**: "세"/"원" 2자를 추가하려 사용자가 Font Asset Creator로 폰트를 재생성(103자)했는데, Unity 6에서 **새 애셋으로 만들면 `TMP_FontAsset`이 아니라 `UnityEngine.TextCore.Text.FontAsset` 타입으로 생성**된다. 같은 GUID 자리에 다른 타입이 앉으면서 씬의 TMP_Text 11개가 전부 폰트 참조를 잃었고(9개는 LiberationSans SDF로 조용히 폴백, 2개는 null) 모든 한글이 □로 표시됨. 사용자가 `git checkout`으로 폰트 파일을 되돌렸으나 화면은 그대로였음.
- **프롬프트 원문**: "폰트가 깨졌어. git checkout이 제대로 됐는지부터 확인하고 진행해줘. ... ## 절대 건드리면 안 되는 것 - Assets/Scenes/SampleScene.unity (오늘 작업한 시대 전환 내용이 있음) ... ## 하지 말 것 - 폰트를 다시 굽지 마. 복구만 해줘. - 원인이 확정되기 전에 파일을 삭제하거나 새로 만들지 마."
- **설계 판단과 근거**: 복구 전에 디스크 상태를 먼저 전수 확인해 "checkout은 성공했고 디스크는 정상, 인메모리만 깨짐"을 분리해낸 것이 핵심이었다 — 이걸 안 하고 Library 삭제나 재굽기로 갔으면 오늘 작업까지 날릴 뻔했다. 씬 `isDirty=False`를 확인해 재로드로 잃을 것이 없음을 보장한 뒤에야 손을 댔다.
- **검증 방법**: git status/diff(Fonts 변경 0건), 애셋 내 `m_Script` GUID(`71c1514a…`=TMP_FontAsset)와 `m_EditorClassIdentifier` 대조, .meta GUID와 씬의 `m_fontAsset` 참조 22건 일치 확인. 1차 시도(Play 모드 종료)는 실패 — Unity가 "이 GUID→유효하지 않음" 해석을 AssetDatabase에 캐싱해 재해석을 안 했기 때문. `ImportAsset(ForceUpdate)` + `OpenScene` 재로드 후 11/11 전부 Pretendard SDF 복귀, EraManager 배선·한글 라벨 전부 보존 확인.
- **AI 산출물 vs 사용자 개입**: 폰트 재굽기와 git checkout은 사용자가 수행. 원인 분리(디스크 정상/인메모리 파손), Play 모드 종료로는 안 되는 이유 규명, 재임포트+재로드 복구, 보존 검증은 AI가 수행. 폰트는 100자 상태로 되돌아가 "세"/"원"은 여전히 없음 — 재굽기는 반드시 기존 애셋의 Update Atlas Texture → Save 경로로 해야 함을 CLAUDE.md에 규칙으로 추가.
- **담당**: 개발

### 2026-08-02 (D-6) | ProjectSettings 원복 — preloadedAssets 누락으로 빌드 UI 입력이 깨질 뻔함

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 커밋 직전 스테이징 내용을 훑다가 `ProjectSettings.asset`의 `preloadedAssets`에서 `InputSystem_Actions.inputactions`(guid `2bcd2660…`)가 통째로 빠져 있는 것을 발견. 세션 시작 전부터 있던 미커밋 변경이었고, 전날 HDR/셰이더 오진단 작업 중 딸려 들어간 것으로 확인됨. 시대 전환 기능과 무관해 커밋에서 제외하고 사용자에게 보고 → `git checkout`으로 원복.
- **프롬프트 원문**: "preloadedAssets에서 InputSystem_Actions가 빠진 건 의도한 변경이 아니야. 어제 HDR 진단하다가 딸려 들어간 것 같아. 원복 후 InputSystem_Actions가 preloadedAssets에 다시 있는지, HDR 설정도 원래 값인지 확인해줘."
- **설계 판단과 근거**: 씬을 grep해 `InputSystem_Actions`를 직접 참조하는 오브젝트가 **한 곳도 없음**을 확인한 것이 판단 근거였다 — 직접 참조가 없으면 `preloadedAssets`가 유일한 빌드 포함 경로이므로, 그대로 배포했으면 WebGL 빌드에서 `InputSystemUIInputModule`이 액션 애셋을 못 찾아 **증강 카드 클릭이 죽을 수 있었다**. 이동은 `Keyboard.current` 직접 조회라 무관해서 로컬 Play 테스트로는 안 잡혔을 버그.
- **검증 방법**: 원복 후 `preloadedAssets`에 guid `2bcd2660…` 복원 확인. HDR은 `m_SupportsHDR: 1`이고 `git log`로 URP 애셋 3종(UniversalRP/Renderer2D/GlobalSettings)이 **초기 커밋 `a586f96` 이후 한 번도 수정된 적 없음** + 작업트리 변경 0건을 확인해, 전날 HDR 실험이 완전히 원복돼 있었음을 입증.
- **AI 산출물 vs 사용자 개입**: 커밋 전 스테이징 검토 중 AI가 발견하고 영향 범위(씬 직접 참조 없음 → 빌드 입력 위험)를 규명해 보고. 의도한 변경이 아니라는 판단과 원복 지시는 사용자가 내림.
- **담당**: 개발

### 2026-08-02 (D-6) | 폰트 재굽기 성공 (103자) — Update Atlas Texture 경로로 타입 보존

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 앞선 사고를 되풀이하지 않는 경로로 "세"/"원"을 추가해 100자 → 103자 재생성. **Window 메뉴에서 Font Asset Creator를 새로 열어 굽지 않고**, 기존 `Assets/Fonts/Pretendard SDF` 인스펙터의 **Update Atlas Texture** → Generate → **Save**(Save as 아님)로 제자리 갱신. AI는 임시 TTF를 Assets에 배치하고 붙여넣을 문자열·설정값(Padding 5 / Atlas 1024 / SDFAA)만 준비한 뒤 멈췄고, 굽기는 사용자가 수행.
- **프롬프트 원문**: "3번에서 반드시 멈춰. 폰트 굽기는 내가 직접 한다. / 폰트가 또 깨지면 Library 삭제 전에 AssetDatabase.ImportAsset(ForceUpdate) + 씬 재로드를 먼저 시도해. / 4번 검증에서 문제 있으면 5번으로 넘어가지 말고 알려줘."
- **설계 판단과 근거**: 사고의 근본 원인은 "새 애셋 생성"이었다 — Unity 6에서 Font Asset Creator로 새로 만들면 `TMP_FontAsset`이 아닌 `UnityEngine.TextCore.Text.FontAsset`이 나오고, 같은 GUID 자리에 다른 타입이 앉으면 씬의 TMP 참조가 전멸한다. 기존 애셋에 Save로 덮어쓰면 타입이 보존된다. 이 두 경로의 차이를 CLAUDE.md에 규칙으로 못박아 재발을 막았다.
- **검증 방법**: 타입 `TMPro.TMP_FontAsset` 확인, 103자/Static/1024×1024/Padding 5/SDFAA 확인, `세`·`원`·em dash(U+2014) 개별 포함 확인, 씬 TMP **11/11** Pretendard SDF 참조 확인. Play 모드에서 "원시 시대"/"중세 시대"/"WAVE 6 — BOSS"/"고대의 포식자" 4개 문자열을 실제 렌더해 `characterInfo[i].fontAsset` 전수 검사 — **누락 글리프 0건**, TMP 글리프 경고 0건. em dash가 정상이라 하이픈 교체는 불필요했음. 임시 TTF·.meta 삭제 후 재검사에서도 103자 유지 확인.
- **AI 산출물 vs 사용자 개입**: 굽기는 사용자가 직접 수행(MCP는 인터랙티브 다이얼로그로 불가). 경로 설계, 문자열·설정값 준비, 굽기 후 7단계 검증, 임시 파일 정리는 AI가 수행. 에디터 전용 메타데이터 `sourceFontFileGUID`에 삭제된 TTF의 GUID가 남았으나 런타임 참조(`m_SourceFontFile`)는 비어 있어 빌드 무영향 — API 경로가 불확실해 무리하게 건드리지 않고 남겨둠.
- **담당**: 개발

### 2026-08-02 (D-6) | Web 빌드 및 배포 (시대 전환 + 폰트 반영, dist 13MB)

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 콘솔 에러 0건 확인 후 `BuildPipeline.BuildPlayer(WebGL)`로 재빌드, `deploy.bat`으로 프로덕션 배포(https://timecore-chi.vercel.app). 커밋 3건(시대 전환 / 폰트 원본 / 폰트 서브셋)을 push.
- **프롬프트 원문**: "빌드 전 Unity_GetConsoleLogs로 에러 0건 확인 / \"failed\"로 나와도 에러 0건이면 Build/ 타임스탬프와 용량으로 판단 / deploy.bat 실행 / dist/ 총 용량 알려줘"
- **설계 판단과 근거**: `fonts/Pretendard-Regular.ttf`(2.7MB)를 저장소 루트에 커밋했다 — Assets 밖이라 **WebGL 빌드 용량에 영향이 없으면서** 재굽기 소스로 계속 필요하다(D-4에 UI 텍스트가 늘면 또 굽는다). SIL OFL 1.1이라 재배포도 허용된다. 반대로 Assets 안 임시 TTF는 굽기 직후 삭제해 빌드에 안 들어가게 했다.
- **검증 방법**: `Unity_RunCommand`가 이번에도 빌드를 "failed"로 오보고했으나 전부 Sentis(`com.unity.ai.inference`) 셰이더 경고였고, 콘솔 에러 0건 + 산출물 4종 타임스탬프 갱신(02:55 → 16:58)으로 실제 성공 판정. `index.html`에 미치환 `{{{ }}}` 매크로 0건 및 산출물 4개 참조 일치 확인. 배포 후 `curl -I`로 `.wasm.br`(`content-encoding: br` + `content-type: application/wasm`)와 `.data.br`(`br`) 헤더 검증. dist 총 **13MB** (wasm 7.2 / data 4.8 / framework 76K / loader 28K) — 직전 12MB 대비 약 1MB 증가.
- **AI 산출물 vs 사용자 개입**: 빌드·검증·배포·커밋·push 전량 AI가 수행. 브라우저 최종 확인은 사용자가 수행.
- **담당**: 개발

### 2026-08-03 (D-5) | 디렉터 판단 로그 첫 실측 — 시대 전환 밀도 리셋 검증

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 사용자가 에디터에서 클리어까지 1회 완주(게임 내 경과 약 288초). 콘솔에 남은 `[AnomalyDirector]` 개입 10건을 회수해 `812f95f`(시대 전환 시 밀도 누적이 stale해지는 버그) 수정이 실제 플레이에서 동작하는지 확인.
- **프롬프트 원문**: "에디터에서 플레이하고 클리어했어 디렉터 뭐시기 확인해준다매 해줘"
- **설계 판단과 근거**: 수정은 의도대로 동작했으나 로그 자체는 제출 문서용으로 약하다고 판단했다 — 10건이 전부 규칙 2(과잉 화력)뿐이고, 그중 6건이 `누적 0.50→0.50`으로 `maxAccumulatedRatio` 바닥에 걸려 밀도 개입이 사실상 무효인 줄이다. 임계값은 플레이 감각에 직결되므로 임의로 손대지 않고 사용자 판단으로 남겼다.
- **검증 방법**: 원시 시대 `1.00→0.71→0.50→(0.50 고정 ×3)`, t=110~160s 구간 개입 0건(보스전·전환을 `CanIntervene`이 차단), 중세 시대 t=160s에서 **`누적 1.00→0.71`** — 리셋 확인. 수정 전이라면 0.50을 물고 넘어와 중세 내내 밀도를 못 올렸을 지점이다. 에러·경고 0건. hp 최저 0.84 / killRate 최저 0.60이라 규칙 1(위기 0.35)·규칙 3(정체 0.30)은 조건 미달로 미발동.
- **AI 산출물 vs 사용자 개입**: 플레이 완주는 사용자, 로그 회수·해석·문제 제기는 AI.
- **담당**: 개발

### 2026-08-03 (D-5) | 판 종료 시 디렉터 요약 자동 출력 — OnApplicationQuit 대신 OnDestroy

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 위 실측에서 `GetActivitySummary()`(평가 N회 중 개입 M회 + 건너뜀 사유)를 못 건진 문제를 해결. 판이 끝나면 요약과 판단 전문이 자동으로 1회 남도록 `OnDestroy`/`OnApplicationQuit` + `_autoDumped` 가드를 추가하고, `DumpLog()`의 "개입 0건이면 조기 반환"을 제거.
- **프롬프트 원문**: "Play 모드 종료 시 GetActivitySummary()가 자동으로 콘솔에 찍히게 해줘. OnDisable이나 OnApplicationQuit 검토해서 확실한 쪽으로. 내가 인스펙터 우클릭을 깜빡해도 로그가 남아야 해."
- **설계 판단과 근거**: `OnApplicationQuit`은 씬 리로드에 불리지 않아 `GameOverController:140`의 재파견으로 시작한 판이 통째로 유실된다. `OnDisable`은 판 중간 비활성화에도 불려 오탐이 난다. 그래서 Play 종료와 씬 리로드 양쪽에 모두 걸리는 `OnDestroy`를 주력으로 삼고, `OnApplicationQuit`은 빌드에서 종료 시 `OnDestroy`가 생략되는 경우만 대비한 이중 안전장치로 남긴 뒤 플래그로 중복 출력을 막았다. 요약은 `verboseLog`에 묶지 않았다 — 개입 0건일 때 "왜 안 했는지"가 가장 필요한데 그 토글이 꺼져 있으면 목적을 잃는다.
- **검증 방법**: `HideAndDontSave` 임시 오브젝트(씬 미오염)에 컴포넌트를 붙여 수동 `DumpLog()`와 `DestroyImmediate` 두 경로를 실행 — 콘솔 2건 출력, 스택이 `OnDestroy → AutoDump → DumpLog`로 찍히는 것까지 확인. 컴파일 에러 0건. 실제 Play 모드 종료 경로는 다음 완주 때 확인 예정.
- **AI 산출물 vs 사용자 개입**: 콜백 3종 비교·구현·스모크 테스트 전량 AI 수행. 어느 콜백이 확실한지 검토하라는 요구는 사용자.
- **담당**: 개발

### 2026-08-05 (D-3) | 브로테이토식 아레나 맵 (기획서 5번) — ArenaBounds 도입

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 원시/중세 배경 아트(3344×1882, 각 13MB) 임포트 후 논리적 아레나 경계(`ArenaBounds`)를 배경 스프라이트의 실제 `renderer.bounds`에서 역산하도록 신규 작성. `PlayerMove`/`CameraFollow`에 클램프 적용, `EnemySpawner`(+`AnomalyDirector` 난입)를 아레나 가장자리 스폰으로, `WaveManager`(보스)·`EraManager`(시간 균열)를 각각 우측/좌측 끝 스폰으로 변경. `BackgroundGrid`는 삭제 대신 비활성화(되살릴 여지 남김).
- **프롬프트 원문**: "기획서 5번(장애물 없는 평평한 아레나, 시간 균열은 왼쪽 끝, 보스는 오른쪽 끝)을 구현하는 작업이야 ... 구현 방식을 먼저 알려주고 확인받은 뒤 진행해줘"
- **설계 판단과 근거**: `ArenaBounds`가 배경 스프라이트 실측 크기에서 경계를 역산하게 만들어 PPU·스케일을 얼마로 바꿔도 시각적 벽=논리적 벽이 항상 일치하게 했다. 콜라이더 대신 위치 클램프만 쓴 이유는 CLAUDE.md의 물리 연산 회피 원칙. `BackgroundGrid` 폴링 컨벤션(EraManager 비수정)을 그대로 재사용해 새 코드 스타일을 늘리지 않았다.
- **오진단(기록)**: Sprite Pixels Per Unit을 Max Size로 축소된 실제 픽셀 폭(2048)에 맞춰 계산했다가 아레나가 의도보다 1.63배 크게 나왔다. 원인은 Unity가 Max Size로 축소된 텍스처의 PPU를 원본 해상도(3344px) 기준으로 자동 보정하는 동작 — Max Size는 시각 해상도만 낮추고 월드 크기는 원본 기준으로 고정하기 위한 의도된 동작이었다. 원본 해상도 기준으로 재계산(PPU 134)해 해결.
- **검증 방법**: Play 모드에서 `Time.timeScale`이 타이틀 대기로 0인 것을 발견해 `TitleController.StartRun()`으로 해제 후 재검증. `ArenaBounds.Clamp(9999,-9999)`가 정확히 rect 경계로 스냅, 카메라가 아레나 밖으로 나간 플레이어를 뷰 절반만큼 정확히 clamp, 아레나를 화면보다 작게 만들면 카메라가 추적 대신 중앙 고정으로 전환되는 것까지 확인. `ForceSpawnBoss()`로 보스가 xMax-1에, `ForceEraTransition()`으로 배경 스프라이트 교체+좌측 끝 리스폰까지 실측. 콘솔 에러·경고 0건.
- **AI 산출물 vs 사용자 개입**: 설계~구현~씬 배선~Play 모드 실측 검증 전량 AI 수행. 임포트 수치(1MB 기준)는 리플렉션 제한으로 Inspector 라벨을 직접 못 읽어 DXT1 공식 추정치로 보고, 필요 시 실측(WebGL 빌드)은 사용자 요청 시 추가 진행하기로 함.
- **담당**: 개발

### 2026-08-05 (D-3) | 아레나 벽 체감 수정 — 카메라 클램프를 VisualRect로 분리

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 위 아레나 작업 직후 사용자가 "벽까지 이동해도 끝까지 안 보인다"고 지적. `ArenaBounds`에 inset 없는 `VisualRect`(스프라이트 원본 bounds)를 추가하고 `CameraFollow`의 클램프 기준을 `Rect`(걸을 수 있는 영역)에서 `VisualRect`로 교체.
- **프롬프트 원문**: "아냐아냐 다 보이게 하고 싶은게 아니라 움직였을때 끝까지 안보인다는거야"
- **되돌린 시도(기록)**: 처음엔 "그림이 짤려 보인다"는 말을 문자 그대로 받아들여 아레나를 화면 안에 다 들어오는 크기(`arenaScale` 1→0.68)로 줄였다. 사용자가 즉시 정정: 원인은 아레나 크기가 아니라 `CameraFollow`가 걸을 수 있는 영역(`Rect`, inset 적용)으로 클램프돼 있어서 플레이어가 벽에 붙어도 카메라가 그 안쪽에서 멈추고 inset만큼의 테두리 장식이 화면 밖으로 밀려나는 것이었다. `arenaScale`·inset은 원래 브로테이토식 값(1.0 / 1.7,1.6)으로 되돌리고, `Rect`(로직용)와 `VisualRect`(카메라 클램프용)를 분리하는 쪽으로 방향을 바꿨다.
- **검증 방법**: Play 모드에서 카메라 aspect를 16:9로 고정하고 플레이어를 `Rect.xMax`(논리적 벽)에 둔 뒤 `CameraFollow`를 잠깐 비활성화해 `DesiredPosition()`과 동일한 클램프 값으로 카메라를 고정, `Unity_Camera_Capture`로 실제 렌더 캡처. 캡처에서 화면 오른쪽 끝에 테두리 장식(돌·두개골·식물)이 명확히 들어오는 것을 눈으로 확인. 콘솔 에러 0건.
- **AI 산출물 vs 사용자 개입**: 첫 진단(아레나 크기 문제)은 AI의 오판이었고, 실제 원인 파악과 방향 전환은 사용자 피드백이 트리거. 구현·재검증은 AI 수행.
- **담당**: 개발

### 2026-08-05 (D-3) | 모래시계 시대 전환 게이트 + 보스 처치 시 경험치 자석

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 원시 보스 처치 후 자동으로 시작되던 시대 전환을 "아레나 중앙에 모래시계 스폰 → 플레이어가 먹어야 전환 시작"으로 변경(`Hourglass.cs` 신규). `WaveManager.HandleBossDeath`에서 보스 처치 시 필드의 `ExpOrb`를 전부 `ForceAbsorb()`로 즉시 자석 흡수시킴(원시·중세 보스 공통).
- **프롬프트 원문**: "스테이지 넘어갈때 중앙에 모래시계를 먹으면 넘어갈 수있게 해주고 보스를 잡으면 필드에 있는 exp 구슬들이 전부 자석처럼 먹게해줘"
- **설계 판단과 근거**: `EraManager.TransitionRoutine()`의 `delayBeforeFade`(보스 처치 후 1초 대기)는 이제 플레이어가 모래시계까지 걸어가는 시간이 그 역할을 대신하므로 통째로 제거(죽은 코드 정리). 모래시계 프리팹은 기존 `ExpOrb`/`Enemy`와 동일하게 내장 Knob 스프라이트+틴트 색상 placeholder로 만듦(커스텀 아트 아직 없음, 기존 컨벤션 유지). 스폰 위치는 `ArenaBounds.Rect.center`(아레나 작업에서 이미 만든 API 재사용).
- **검증 방법**: Play 모드에서 `EnemySpawner`/`AnomalyDirector`를 잠깐 꺼서 잡음을 없앤 뒤, 보스 스폰 직후 즉시 처치(대기 없이) → 모래시계가 정확히 `Rect.center`(0,0)에 스폰됨을 확인, 플레이어를 그 위로 이동시켜 전환 시작·`CurrentEra=Medieval`·배경 스프라이트 교체·좌측 끝 리스폰까지 확인. 콘솔 에러 0건.
- **실패한 시도 2건(기록)**: (1) 첫 시도에서 보스가 정지 상태 플레이어를 실시간으로 따라잡아 접촉 데미지로 죽여 `IsGameOver` 가드에 막혀 모래시계가 안 뜬 것으로 오인할 뻔함 — 보스 스폰과 즉시 처치를 한 호출 안에 묶어 해결. (2) 플레이어를 스크립트로 순간이동시켰을 때 `Collider2D.bounds`가 갱신 안 돼 트리거가 안 잡힘 — `Physics2D.SyncTransforms()` 필요함을 확인. 둘 다 실제 게임 버그가 아니라 에디터 스크립트로 순간이동시키는 테스트 방식 자체의 한계였음.
- **AI 산출물 vs 사용자 개입**: 설계~구현~Play 모드 실측 검증 전량 AI 수행.
- **담당**: 개발
