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
| Kenney Particle Pack (1.1) | https://kenney.nl/assets/particle-pack | **CC0 1.0** (표기 의무 없음, 전문: `docs/licenses/Kenney-ParticlePack-CC0.txt`) | 이펙트 스프라이트 5종 (`Assets/Art/FX/`). 배포 팩 80종 중 선별 — 전량 도입 시 원본 15 MB |

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

### 2026-08-05 (D-3) | 빌드·커밋·푸시·배포 2회 (아레나 + 모래시계 반영)

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 위 아레나(`ArenaBounds`/`VisualRect`)와 모래시계+경험치 자석 작업을 커밋 2건(`6c14693`, `98340af`)으로 나눠 push. 각각 WebGL 재빌드 후 `deploy.bat`으로 프로덕션 배포(https://timecore-chi.vercel.app).
- **프롬프트 원문**: "빌드 커밋 푸시 다해줘" → (다음 기능 완료 후) "커밋해줘" → "푸시해줘" → "배포해줘" — 단계별로 나눠 요청.
- **설계 판단과 근거**: 커밋을 기능 단위로 쪼갠 이유는 아레나 구조 변경과 모래시계 게이트가 서로 다른 리뷰 단위라 롤백이 필요해지면 독립적으로 되돌릴 수 있어야 하기 때문. 매번 자동으로 묶어 처리하지 않고 사용자가 개별 요청한 단계(빌드/커밋/푸시/배포)만 그때그때 수행했다.
- **검증 방법**: 두 빌드 모두 "failed"로 오보고했으나 전부 Sentis 셰이더 경고뿐 — 콘솔 에러 0건 + `Build/` 산출물 4종 타임스탬프 갱신으로 실제 성공 판정. 배포 후 `curl`로 `timecore-chi.vercel.app`의 `Content-Length`가 로컬 `Build.data.br`과 정확히 일치함을 확인(1차 15,787,806 / 2차 15,792,868 bytes). 맵 텍스처 추가로 dist 총 용량이 약 12.6MB → 23.3MB로 증가.
- **AI 산출물 vs 사용자 개입**: 빌드 판정·커밋·푸시·배포 전량 AI 수행, 각 단계는 사용자가 개별 승인·요청.
- **담당**: 개발

### 2026-08-05 (D-3) | 시대 2개 → 4개 확장 (원시·중세·현대·미래)

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: `Era` enum에 `Modern`/`Future` 추가, `primitiveConfig`/`medievalConfig` 개별 필드를 `EraConfig[] eraConfigs` 배열로 통합(인덱스=enum 값). `EraConfig`에 `eraShortName`·`enemyColor`·`enemyHpMultiplier`·`bossHpMultiplier` 추가. `ArenaBackground`를 `Sprite[]`로, `AnomalyDirector`의 "반대 시대" 이분법을 `EraManager.GetRandomOtherEraConfig()` 호출로 교체(프리팹 필드 2개 제거 → 배선 중복 해소). `WaveManager.SpawnBoss`/`HandleEnemySpawned`에 HP 배율·색 주입. 맵 2종 임포트, 웨이브 25s×5 → 15s×4, 디렉터 warmup 25 → 15s.
- **프롬프트 원문**: "시대를 4개(원시/중세/현대/미래)로 확장한다. … [F. HP 스케일링] … 초기값은 완만하게: 1.0 / 1.2 / 1.45 / 1.75 (웨이브 단축으로 플레이어 증강 획득이 줄었으니 세게 잡지 마라)" / "[하지 말 것] Boss.cs 패턴 시대별 분리, 새 적 타입 / 새 프리팹 추가, ScriptableObject 도입, BackgroundGrid.cs, 요청하지 않은 리팩터링"
- **설계 판단과 근거**: `EraConfig` 기본값(라벨·색·배율)을 C# 필드 이니셜라이저에 넣었더니 Unity가 신규 배열 필드를 그 값으로 역직렬화해서, 우려했던 인스펙터 대량 수작업이 사라지고 오브젝트 참조 8칸(`enemyPrefab` 4 + 배경 4)만 주입하면 됐다. 프리팹은 기존 2종을 재사용(현대·미래는 `MedievalEnemy` 공유) — 색·HP를 config가 덮으므로 프리팹이 결정하는 건 이동속도뿐이다. WebGL 용량 때문에 맵도 기존과 동일하게 maxTextureSize 2048 + crunch 압축으로 맞췄다(3344×1882 → 2048×1153, PPU 82.07, 월드 24.96×14.05로 4장 완전 동일 → inset 재조정 불필요).
- **작업 중 발견한 기존 버그**: `Health.Awake()`가 프리팹 색을 `_baseColor`로 캐시하고 피격 플래시 후 그 색으로 되돌리는 탓에, 스폰 후 `sr.color`를 덮어쓰면 **첫 피격에 원래 색으로 돌아간다**. 중세 보스가 이미 이 경로였다(원시 색으로 복귀). `Health.SetBaseColor()`를 추가해 보스·적·디렉터 난입 적 3곳을 모두 교체했다.
- **검증 방법**: Play 모드 자동 드라이버로 4시대 완주 — 보스 HP **4000 / 4800 / 5800 / 7000**(정확히 ×1.0/1.2/1.45/1.75), 적 HP 25/36/43.5/52.5, 전환 3회 모두 모래시계 획득 → 1.60초 페이드, 미래 보스 처치 시 `OnGameClear` 수신 + `IsGameOver=True`. 시대별 화면 캡처 4종으로 색 분리 확인. 콘솔 **에러 0건**, 경고 1건(`래` 글리프 누락 — 의도된 미반영). 폰트는 `characterTable` 153자 전수 대조로 누락이 `래` 1자뿐임을 확인. WebGL 재빌드는 이번에도 "failed" 오보고였으나 전부 Sentis 셰이더 경고 + 콘솔 에러 0건 + 산출물 4종 타임스탬프 갱신으로 성공 판정. **빌드 용량 23,348,177 → 33,165,194 bytes (+9.36 MiB, +42%)** — 증가분은 전량 `Build.data.br`(맵 2장)이고 `Build.wasm.br`은 −2KB로 코드 변경 비용은 0이다. 맵 1장당 약 4.7 MiB.
- **실패한 시도·오진단 3건**: (1) 완주 드라이버가 "모래시계 미스폰"으로 죽었는데, 실제로는 플레이어가 아레나 중앙(0,0)에 서 있고 모래시계도 중앙에 스폰돼 **즉시 획득**된 것이었다 — 기능은 정상이었고 드라이버가 틀렸다. (2) 플레이어 색을 (1,1,1) 흰색으로 읽고 판단할 뻔했으나, 그 순간 피격 플래시 중이었고 **실제 색은 시안(0.435,0.847,0.878)** 이었다. (3) 동적 컴파일 어셈블리가 `UnityEngine.UI`를 참조하지 못해 증강 카드 버튼 클릭 경로를 못 태우고 `ForceClose()`로 우회했다(카드 효과 미적용 — 검증 목적에는 무영향).
- **미해결로 넘긴 것**: 미래 시대를 지시대로 시안 계열로 잡았으나 **플레이어 자체가 시안이라 보스와 같은 색 계열**이고 맵의 시안 발광 장식과도 섞인다. 캡처를 근거로 사용자 판단에 넘겼다. 보스 이름도 4시대 모두 `고대의 포식자`로 동일하다(새 이름 = 새 글리프라 폰트 굽기와 함께 확정 필요).
- **담당**: 개발

### 2026-08-05 (D-3) | 미래 시대 색 교체 + 보스 이름 4종 확정 — 씬 외부 편집으로 세션 8분 차단

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 미래 시대 `enemyColor`를 짙은 시안(0.055,0.478,0.522) → **짙은 마젠타(0.612,0.114,0.451)**, `bossColor`를 밝은 시안(0.133,0.761,0.816) → **밝은 마젠타/바이올렛(0.847,0.322,0.898)** 으로 교체. 보스 이름을 시대별로 확정 — 원시 `고대의 포식자` / 중세 `강철의 심문관` / 현대 `강화 병기` / 미래 `시간의 지배자`. CLAUDE.md에 "Unity 켜진 동안 `.unity`/`.prefab`/`.asset` 외부 편집 금지" 하드 룰 추가.
- **프롬프트 원문**: "EraManager.cs 미래 EraConfig의 색만 바꿔줘. enemyColor: 짙은 마젠타 계열 / bossColor: 밝은 마젠타/바이올렛 계열. 플레이어 시안 및 맵 발광 장식과 확실히 분리되는지 캡처로 확인. 다른 건 아무것도 건드리지 마라." / "EraConfig의 bossName 필드에 넣고, 씬 값은 SerializedObject로 주입해줘. 파일 직접 편집은 하지 말고."
- **설계 판단과 근거**: 직전 캡처에서 미래 시대가 **플레이어(시안 0.435,0.847,0.878) · 보스(시안) · 맵의 시안 발광 장식** 셋이 같은 색 계열로 겹치는 것이 확인됐다. 원시=적주황 / 중세=강철청 / 현대=올리브를 이미 쓰고 있어 남은 뚜렷한 색상이 시안의 보색인 마젠타였고, 네온 계열이라 "미래" 인상도 유지된다. 색상환에서 갈라놓았으므로 명도 대비에만 기대지 않는다.
- **실패한 시도 — 씬 파일 외부 편집으로 세션 차단(8분)**: `EraConfig` 값은 씬에 직렬화돼 있어 `.cs` 기본값만 고치면 반영되지 않는다. 이를 알고 `SampleScene.unity`를 텍스트로 직접 편집했는데, **Unity가 외부 변경을 감지해 모달 다이얼로그를 띄웠고 MCP 호출이 전부 무응답**이 됐다. 모달은 MCP로 닫을 수 없어 사용자가 직접 Reload를 눌러야 풀렸다. 이후 보스 이름 주입은 `SerializedObject.FindProperty` → `ApplyModifiedPropertiesWithoutUndo` → `SaveScene` 경로로 처리해 재발이 없었다. 같은 실수를 막으려고 CLAUDE.md 하드 룰로 승격했다.
- **오진단 1건**: 통합 폰트 문자열(153자 + 6자)을 손으로 병합하다 키릴 문자 `коде`를 섞어 넣었다. 굽기를 깨뜨릴 오류였고, 파이썬으로 재생성하며 길이(159)와 허용 외 문자(0개)를 단언 검사해 잡았다. 문자열 병합은 눈으로 검수하지 말 것.
- **검증 방법**: 실제 전환 경로로 미래 시대까지 3회 전환 후 캡처 — 플레이어 시안 / 적 마젠타(0.612,0.114,0.451, HP 52.5) / 보스 밝은 마젠타(0.847,0.322,0.898, HP 7000)가 회색 금속 바닥·시안 발광 장식과 모두 분리됨을 확인. 폰트는 문자열 21종을 `characterTable` 153자와 전수 대조해 누락 6자(`철심문병배래`) 확정, Play 모드 콘솔 폴백 경고도 정확히 2건(`래`·`배`)으로 일치. **콘솔 에러 0건.**
- **담당**: 개발

### 2026-08-07 (D-3) | 폰트 서브셋 159자 굽기 완료 — 굽기 실패 1회 뒤 재시도, 빌드·배포까지

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 8/5에 막혀 있던 폰트 굽기(153 → 159자, `철심문병배래`)를 사용자가 재수행. 굽기 검증 → 굽기용 임시 TTF 삭제 → 커밋 `6bfa858` + 미푸시 2건 push → WebGL 재빌드 → `deploy.bat` 프로덕션 배포(https://timecore-chi.vercel.app).
- **프롬프트 원문**: "시작해보자" → (굽기 안내 후) "됐다"
- **실패한 시도(8/5)와 원인**: 사용자가 구웠다고 했으나 `characterTable`이 153자 그대로였다. 애셋 diff가 **소스 폰트 GUID 2줄뿐**이고 줄 수(4167)·아틀라스가 바이트 동일 → 소스 폰트만 지정하고 **Generate Font Atlas를 안 누른 것**으로 특정. 재안내 시 그 단계를 굵게 강조해 이번엔 4167 → 4377줄로 갱신됐다. 다행히 타입은 `TMP_FontAsset` 유지라 8/2식 참조 유실 사고는 없었다.
- **설계 판단과 근거**: 굽기 전 159자 문자열을 **손으로 검수하지 않고** `docs/FONT_CHARS.md`에서 직접 읽어 스크립트로 단언 검사했다(8/5에 눈으로 병합하다 키릴 `коде`를 섞은 재발 방지) — 길이 159 / 중복 0 / 허용 범위 밖 0 / 기존 153자 중 유실 0. 임시 TTF는 8/2 선례대로 삭제했다(원본은 Assets 밖 `fonts/`에 git 추적 중, 해시 동일 확인 후 삭제).
- **검증 방법**: `characterTable`·`glyphTable` 159, glyph rect 유효성 6자 전부 정상. CLAUDE.md 절차(`ImportAsset(ForceUpdate)` → `ReadFontAssetDefinition()` → 씬 재로드) 후 **임시 캔버스에 실제로 렌더해** `characterInfo[i].fontAsset`를 전수 검사 — 보스명 4 + 시대 배너 4 + 난입 배너 4 + 게임오버 4 + 기존 UI 12종, **가시 문자 162자 전부 Pretendard SDF, 폴백 0건**(`HasCharacter()`만으로는 못 믿는다). 씬 TMP 참조 21/21. 빌드는 또 "failed" 오보고였으나 Sentis 셰이더 경고뿐 + 콘솔 에러 0건 + 산출물 4종 타임스탬프 갱신으로 성공 판정. **용량 33,165,194 → 33,180,773 bytes (+15,579 B)** — 증가분은 아틀라스 6자분이 전부고 코드 비용 0. 배포 후 `curl -I`로 4개 산출물의 `Content-Length`가 로컬과 바이트 단위 일치, `.br` 3종 `content-encoding: br` 확인.
- **막힌 경로(기록)**: `AssetDatabase.DeleteAsset`이 MCP에서 "User interactions are not supported"로 차단됐다. `.ttf`는 씬·프리팹이 아니라 CLAUDE.md 외부 편집 금지 대상이 아니므로 파일 삭제 + `AssetDatabase.Refresh()`로 우회했고, 삭제 후 `sourceFontFile = null` 상태에서도 렌더 폴백 0건을 재확인했다(Static SDF라 소스 불필요).
- **담당**: 개발

### 2026-08-07 (D-3) | 맵 기믹 "시간 감속 지대" + AI 디렉터 4번째 개입(전선 고정)

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: `RiftZone.cs`(지대 본체 + static 질의) / `RiftZoneSpawner.cs`(웨이브 상시 스폰 + 디렉터용 API) 신규. `Enemy`·`PlayerMove`에 배율 1줄씩, `EraManager.ClearBattlefield`에 `RiftZone.ClearAll()` 1줄, `AnomalyDirector`에 규칙 4 추가. 프리팹·씬 배선은 `SerializedObject` 주입으로 처리.
- **프롬프트 원문**: "맵에 기믹을 추가 하고 싶은데 추천좀 해줘" → 3안 제시 후 사용자 선택: "A안 + 디렉터 연동 (추천)"
- **설계 판단과 근거**: 기획서 5번이 "장애물 없는 평평한 아레나"라 **막는 기믹이 아니라 밟는 기믹**으로 갔다. 플레이어와 적에게 **같은 배율**을 걸어 규칙 하나로 판단이 갈리게 했다(적을 끌고 들어가면 도구, 혼자 늦게 밟으면 함정). 콜라이더 대신 대상이 매 프레임 `SpeedMultiplierAt()`을 묻는 방식 — 아레나 경계를 위치 클램프로 처리한 것과 같은 원칙이고 물리 연산이 0이다(존 ≤2 × 적 ≤40 = 프레임당 수십 회 비교). 원형 대신 **사각**으로 잡은 건 내장 `Square` 스프라이트를 그대로 써 WebGL 용량을 안 늘리려는 것과, 보이는 도형과 판정을 정확히 일치시키려는 것이다. 지대가 겹칠 때 배율을 곱하지 않고 **최솟값 하나만** 적용한다 — 곱하면 겹친 지점에서 사실상 정지해 억울사가 난다. 보스전에는 안 뜬다(`EnemySpawner.SpawningEnabled` 재사용).
- **디렉터 연동**: 기존 규칙 1~3은 전부 "적을 얼마나 낼지"였다. 규칙 4만 **공간**을 바꾼다 — 평가 간격 동안 플레이어 이동이 4유닛 미만인 상태가 2회 연속이고 적이 충분하면 그 자리에 지대를 깐다. 고착 카운트는 개입 쿨다운(20s) 밖에서 세야 표본이 쌓인다. 상시 스폰과 디렉터 개입을 분리해 **디렉터 조건이 한 번도 성립하지 않는 판에서도 기믹은 반드시 등장**하게 했다.
- **검증 방법**: Play 모드 드라이버 실측 — ① 판정 경계 = 보이는 사각형(반변 3.50, 안쪽 0.50 / 바깥 1.00) ② 같은 프리팹 적 2마리 1초 이동 **지대 안 1.100 / 밖 2.201 = 비율 0.500**(기대 0.50) ③ 플레이어 배율 0.50 ④ 구석 배치 시 사각형 전체가 아레나 Rect 안 ⑤ `ClearAll` 1→0 ⑥ 디렉터 규칙 4 **t=35.0s 자연 발화** — `판단: 전선 고정 → 개입: 플레이어 이동 0.0 < 4.0 ×2회 → 감속 지대 배치`(alive 21/40), 상시 스폰분과 합쳐 지대 2개. 콘솔 에러 0건, 씬 오염 0.
- **오진단 1건**: 규칙 4를 띄우려고 자동 사격을 끄고 플레이어를 고정했더니 t=15s에 **규칙 3(전선 정체)이 먼저** 걸렸다 — 그 시점엔 적이 7/40뿐이라 `aliveRatio 0.175 ≤ 0.25`였다. 규칙이 틀린 게 아니라 관찰 시점이 일렀다. 적이 21마리까지 쌓이는 t=35s까지 늘려 재관찰해 확인했다. 또 MCP가 `System.Reflection`을 통째로 막아 디렉터 사적 상태 주입이 불가능했고, 그 덕에 조건을 자연 성립시키는 쪽으로 갔다(결과적으로 더 강한 검증이 됐다).
- **폰트**: 배너 문구 `시간 이상 감지 — 전선 고정` / `균열 발생: 시간 감속 지대`를 159자 세트와 스크립트로 대조해 **신규 글자 0자** 확인 후 채택했다. `구역 고착`(`착`), `균열 확산`(`확`,`산`)은 재굽기가 필요해 버렸다.
- **담당**: 개발

### 2026-08-07 (D-3) | 시대별 회피 패턴 (예고 → 발사) — HazardBeam 1종으로 4시대 구현

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: `HazardBeam.cs`(예고 후 발사되는 직선 위험 구역) / `EraHazardSpawner.cs`(시대별 배치·개수·예고시간) 신규. `EraManager.ClearBattlefield`에 `HazardBeam.ClearAll()` 1줄. 프리팹·씬 배선은 `SerializedObject` 주입.
- **프롬프트 원문**: "나는 근데 시대마다 뭔가 레이저가 나오면 피하고 그런 패턴 기믹들 있잖아 그게 추가 됐으면 좋겠는데"
- **설계 판단과 근거**: 시대마다 다른 코드를 쓰면 D-3에 작업량이 4배가 된다. **직선 1종을 각도·개수·굵기·예고시간만 바꿔** 4시대를 만들었다 — 원시 지면 균열(랜덤 1줄, 예고 1.6s) / 중세 화살 세례(평행 3줄) / 현대 십자 폭격(직교 2줄) / 미래 레이저 격자(가로세로 4줄, 예고 1.0s). 뒤 시대일수록 줄이 많고 예고가 짧다. 예고와 발사를 **색이 아니라 굵기(22% → 100%)로** 갈랐다 — 저사양 브라우저에서 색만으로는 안 읽힌다. 평행·격자는 축을 구간으로 나눠 배치한다(완전 무작위면 두 줄이 붙어 나와 피할 틈이 사라진다). 적 피해는 기본 0이다 — 레이저가 웨이브를 대신 치우면 난이도 곡선이 무너진다. 보스전에는 안 뜬다(`SpawningEnabled` 재사용) — 보스 패턴 위에 겹치면 피할 수 없는 죽음이 된다. 판정은 콜라이더 없이 내적 2회(회전한 사각형)로, `RiftZone`과 같은 원칙이다.
- **검증 방법**: Play 모드 드라이버 — 예고 구간 굵기 **0.44**(=2.00×0.22)·`IsFiring=False`, 발사 구간 회전 45°/길이 20.00/굵기 2.00에 판정 경계가 전부 일치(`시각=판정 OK`). 피해는 **예고 중 100 → 발사 중 88(-12, 1회만) → 종료 후 88**, 막대 밖(y=4.0)에 서 있으면 **88 → 88 무피해**. 시대 순회 실측: 원시 1줄(112° 랜덤) / 중세 3줄 전부 90°(x=-5.8, -1.4, 9.0 — 3구간 균등) / 현대 0°+90° 직교 / 미래 가로 2(y=-2.7, 3.7) + 세로 2(x=-1.9, 8.7) 격자. 시대 전환마다 첫 패턴 지연 8초가 리셋되는 것까지 확인(t≈7.7~8.0s). 콘솔 에러 0건, 씬 오염 0.
- **오진단 1건(테스트 하니스)**: 첫 측정에서 A/B가 전부 FAIL로 나왔다. 원인은 제품이 아니라 드라이버였다 — 런타임에서 프리팹을 못 읽는다고 지레짐작해 `SetActive(false)`인 임시 GameObject를 템플릿으로 썼는데, 복제본도 비활성이라 `Awake`/`Update`가 아예 안 돌았다(`_renderer`가 null이라 스케일이 1.00 그대로였고 발사 구간에 진입조차 못 했다). 판정 함수(`Contains`)만은 그 상태에서도 정확히 맞아 제품 코드 문제가 아님을 알 수 있었다. MCP 동적 어셈블리는 **에디터 어셈블리라 `AssetDatabase`를 쓸 수 있다** — 실제 프리팹을 로드해 재측정하니 전부 OK.
- **담당**: 개발

### 2026-08-07 (D-3) | 기믹 3종 추가 — 회복 코어 / 추적 장판 / 균열 분출구

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: `RecoveryCore.cs`(머물면 확보되는 보상 코어) + `RecoveryCoreSpawner.cs`, `HomingHazard.cs`(따라오다 굳어 터지는 장판), `RiftVent.cs`(주기적 방사형 탄) 신규. `Health.Heal()` 추가. `EraHazardSpawner`에 추적·분출을 시대별 파라미터로 편입(타이머 3개 독립). `AnomalyDirector` 규칙 1 확장. `EraManager.ClearBattlefield`에 3줄.
- **프롬프트 원문**: "좋다 이런 기믹을 몇개 더 추가 하려고하는데 추전해줘" → 3안 제시(C는 프레임 부담으로 비추천) → "A B C 전부 추가하는건 어때"
- **설계 판단과 근거**: 기존 기믹 2종이 전부 "피하라"라서, **"버텨라"** 하나를 넣는 것이 판을 가장 크게 바꾼다고 봤다. 코어 위에 레이저가 예고되면 포기할지 버틸지가 갈린다. 코드에서 발견한 구멍도 이걸로 메웠다 — 규칙 1(위기)은 `RevertDensity()`가 false면(디렉터가 밀도를 올린 적 없는 판) **아무것도 못 하고 조용히 끝났다.** 네 규칙이 전부 압박 수단이라 구제책이 없었던 것이다. 이제 그 자리에 회복 코어를 깐다. 상시 스폰은 기본 꺼둠(`ambientInterval=0`) — 이미 기믹이 넷이라 다섯째가 상시로 끼면 화면을 못 읽고, 디렉터 전용일 때 "죽을 것 같을 때만 나타나는 구원"이라는 성격이 뚜렷해진다. 추적 장판은 추적 속도 3.6 < 플레이어 6이라 반드시 떼어낼 수 있게 뒀다. 분출구는 `BossProjectile`을 그대로 재사용해 새 프리팹·새 탄 코드가 0이다.
- **비추천을 뒤집은 근거(실측)**: C(분출구)는 저사양 프레임 부담을 이유로 비추천했으나 사용자가 전부 추가를 택했다. 그래서 **적 40마리(maxAlive) 고정 조건에서 분출구 유무만 바꿔** 실측했다 — 기준선 평균 324 FPS / 최저 66, 분출구 2개(동시 탄 최대 32발) 평균 329 FPS / 최저 92, **16.7ms 초과 프레임 0/1137 → 0/1163**. 에디터 기준 부담이 측정 노이즈 수준이라 우려는 근거가 없었다. 다만 이건 데스크톱 에디터 수치이므로 저사양 브라우저 실측은 배포 후 별도로 확인해야 한다.
- **검증 방법**: 회복 코어 — 진행도 0.5s=0.17 / 1.5s=0.50(captureTime 3s와 정확히 일치), HP 40→75(+35), 사거리 밖에서 0.40→0.16 감소, 풀피에서 `Heal(35)` 반환 0(코어가 헛되이 소모되지 않음). 추적 장판 — 1.4초간 5.04 접근(속도 3.6 × 1.4 = 5.04), 굳은 뒤 1.0초간 이동량 **0.0000**, 벗어나면 무피해, 안에 서 있으면 정확히 −15. 콘솔 에러 0건.
- **오진단 2건**: (1) 추적 장판 "굳은 뒤 이동량 0.345 FAIL"은 제품이 아니라 **표본을 1.4s(추적 구간 1.5s 안)에서 뽑은** 탓이었다 — 3.6×0.1=0.36과 일치한다. lock 구간 안에서 다시 재니 0.0000. (2) MCP 동적 컴파일이 **`/// <summary>` XML 문서 주석에서 로그 없이 실패**한다(두 번 연속 겪음). 일반 `//` 주석으로 바꾸면 통과한다 — 드라이버에는 XML 주석을 쓰지 말 것.
- **담당**: 개발

### 2026-08-07 (D-3) | 이펙트 — 파티클 없는 풀링 조각 + 카메라 셰이크

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: `EffectSystem.cs`(조각 풀 + 단일 Update), `CameraShake.cs` 신규. `Health.BaseColor` 접근자 추가. 훅 4곳 — 적 사망 버스트(`Enemy`), 플레이어 피격 셰이크(`CameraShake` 자체 구독), 레이저 발사·분출 셰이크(`HazardBeam`/`RiftVent`), 코어 확보 버스트(`RecoveryCore`). `CameraFollow`에 흔들림 합성.
- **프롬프트 원문**: "이펙트를 니가 추가해줄수있니" → 범위 선택: "핵심 4개 (추천)"
- **설계 판단과 근거**: **ParticleSystem을 쓰지 않았다.** 이 게임은 적이 초당 여러 마리 죽어 이펙트가 가장 자주 발생하는 축인데, 파티클은 인스턴스마다 Update·메시 갱신을 돌고 매번 Instantiate하면 GC가 먼저 터진다. 대신 (1) 조각 96개를 미리 만들어 재사용하고 (2) 조각에 MonoBehaviour를 붙이지 않고 배열 하나를 도는 Update **한 개**로 전부 움직인다. 살아있는 조각이 0이면 루프를 즉시 빠져나간다. 새 에셋이 0이라 빌드 용량 증가도 0이다. 사망 이펙트 색은 `SpriteRenderer.color`가 아니라 `Health.BaseColor`를 쓴다 — 맞고 죽는 순간엔 피격 플래시로 흰색이라 그걸 읽으면 이펙트가 전부 하얗게 나온다. 회복 코어에는 셰이크를 넣지 않았다(보상인데 피격과 같은 느낌이 나면 안 된다).
- **구조적 판단(카메라)**: 셰이크가 위치를 직접 쓰지 않고 `CurrentOffset`만 계산하고, `CameraFollow`가 아레나 클램프를 끝낸 뒤 마지막에 더한다. 실행 순서를 보장하지 않는 `LateUpdate` 두 개가 서로 위치를 덮어쓰는 것을 피하려는 것이고, 클램프 뒤에 더해야 벽에 붙었을 때 흔들림이 잘리지 않는다. `CameraFollow`에는 흔들리지 않은 `_followPos`를 따로 뒀다 — `transform.position`(흔들린 값)을 `SmoothDamp`의 시작점으로 쓰면 흔들림이 다음 프레임으로 계속 번진다.
- **검증 방법**: 적 사망 조각 정확히 +5, 0.8초 뒤 0(수명 회수 확인), 색은 프리팹 색 `RGBA(0.659,0.196,0.176)`. 피격 셰이크 오프셋 0.207 → 0.5초 뒤 0.0000. **셰이크 전후 카메라 정착 위치 차이 0.0000(누적 없음)**. 코어 확보 조각 12개. 프레임은 **같은 도살 루프에서 이펙트만 껐다 켜서 분리** — OFF 평균 305 FPS(16.7ms 초과 4/1375), ON 평균 330 FPS(2/1447), 동시 조각 최대 59/96. 차이가 음수라 **이펙트 비용은 측정 노이즈 이하**다. 콘솔 에러 0건.
- **오진단 1건**: 첫 프레임 측정에서 최저 14 FPS·느린 프레임 13개가 나와 이펙트를 의심했으나, 같은 루프가 0.15초마다 적 4마리를 `Instantiate`하고 있었다 — 분리 측정하니 이펙트 몫이 아니었다. 분리 측정 자체도 한 번 멈췄는데, 대량 도살로 레벨업해 **증강 카드가 `timeScale=0`을 걸어** 드라이버의 `WaitForSeconds`가 영원히 대기한 것이었다. `WaitForSecondsRealtime` + 카드 강제 닫기로 해결. Play 모드 드라이버는 이 게임에서 항상 실시간 대기를 써야 한다.
- **담당**: 개발

### 2026-08-07 (D-3) | 적 스폰을 아레나 가장자리 → 필드 안 예고 표식으로 변경

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: `SpawnPortal.cs` 신규(적이 나타나기 전 뜨는 예고 표식). `EnemySpawner`에 필드 안 스폰 경로·`_pending` 목록·`SpawnWithPortal()` 공개 API 추가. `AnomalyDirector`의 난입 적도 같은 표식을 쓰도록 `Dress()`로 분리. `EraManager.ClearBattlefield`에 `SpawnPortal.ClearAll()`.
- **프롬프트 원문**: "몬스터 생성 위치가 필드 내에서 생성되는 이팩트와 함께 나왔으면 좋겠는데 어때"
- **설계 판단과 근거**: 가장자리 스폰은 **적이 걸어 들어오는 시간이 곧 예고**였다. 필드 안에서 그냥 튀어나오면 반응할 틈이 없어 억울한 접촉 피해가 난다. 그래서 레이저·분출구에서 세운 "예고 → 실행" 규약을 그대로 썼다 — 표식이 작게 시작해 커지며 깜빡이다가(커지는 방향이라 "여기서 나온다"가 읽힌다) 그 자리에 적이 나오고 조각이 터진다. 표식은 `_pending`으로 **maxAlive에 함께 센다** — 안 그러면 예고가 겹치는 동안 상한을 넘겨 예약해 놓고 한꺼번에 터진다. 적 등록과 `OnEnemySpawned`는 표식이 열리는 시점에 한다(웨이브 스케일링·시대 색이 그 순간 값으로 걸려야 한다). 되돌리기 쉽게 `spawnInsideField` 토글을 남겼다 — 끄면 예전 가장자리 스폰 그대로다. 난입 적(한 번에 4마리)도 같은 표식을 쓴다. 예고 없이 필드 안에서 4마리가 터지면 피할 방법이 없다.
- **주의해서 피한 함정**: 표식 색을 프리팹 색에서 뽑으면 **현대·미래가 중세와 같은 프리팹을 공유**하고 색은 `EraConfig`가 덮으므로 뒤 두 시대 표식이 중세 색으로 나온다. `EraManager.CurrentConfig.enemyColor`를 직접 참조하게 했다(폴백은 프리팹 색).
- **검증 방법**: Play 모드 실측 — 표식 자리 `(7.65,-1.34)`에서 **거리 0.00으로 적 출현**, 12초 관찰 동안 표식이 전부 아레나 `x[-10.8,10.8] y[-5.4,5.4]` 안, 표식-플레이어 거리 최소 **6.94**(설정 6.0 이상 충족)·최대 8.87, 시대 전환 시 미개봉 표식 1 → 0(다음 시대에 이전 시대 적이 튀어나오지 않음), 상한 `alive + 표식 ≤ maxAlive` 충족. 콘솔 에러 0건, 씬 오염 0.
- **담당**: 개발

### 2026-08-08 (D-2) | 커밋·푸시·WebGL 빌드·프로덕션 배포 (기믹 5종 + 이펙트 반영)

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: 미커밋으로 남아 있던 D-3 작업 세션 4건(레이저·기믹 3종·이펙트·스폰 표식)을 커밋 `a2dfbad` 1건으로 push. WebGL 재빌드 후 `deploy.bat`으로 프로덕션 배포(https://timecore-chi.vercel.app).
- **프롬프트 원문**: "지금 컴퓨터 껐다 켰는데 지금 까지 한 내용 커밋 되어있니?" → "전부 커밋하고 푸시하고 빌드하고 배포까지 해줘"
- **설계 판단과 근거**: 8/5에는 기능 단위로 커밋을 쪼갰지만(`6c14693`/`98340af`) 이번엔 **1건으로 묶었다** — 네 작업이 전부 `SampleScene.unity` 하나와 `docs/AI_LOG.md` 하나를 건드려, 쪼개면 각 커밋이 컴파일·실행되지 않는 반쪽 씬을 담게 된다. 롤백 단위로서 의미가 없는 분할은 하지 않았다.
- **검증 방법**: 빌드 전 콘솔 에러 0건. `BuildPipeline.BuildPlayer`는 이번에도 Sentis 셰이더 경고로 "failed" 오보고 → 산출물 4종 전부 타임스탬프 갱신(01:06)으로 성공 판정. `Build/index.html`에 `{{{ }}}` 매크로 잔존 0. 배포 후 `curl -I`로 4종 `Content-Length`가 로컬과 **바이트 단위 전부 일치**(data 25,621,991 / wasm 7,460,314 / framework 75,723 / loader 26,982), `.wasm.br`은 `application/wasm` + `content-encoding: br` 유지. 총 33,176,297 → **33,185,010 bytes (+8,713)** — 신규 스크립트 10개뿐이고 새 에셋이 0이라 증가분이 코드 몫에 그쳤다(이펙트를 ParticleSystem 대신 코드 풀로 만든 판단이 용량에서도 확인됐다).
- **AI 산출물 vs 사용자 개입**: 커밋·푸시·빌드 판정·배포·검증 전량 AI 수행. 브라우저 최종 확인은 사용자가 수행.
- **담당**: 개발

### 2026-08-08 (D-2) | 보스별 전용 기믹 3종 (총 12패턴) — 새 프리팹 0

- **도구**: Claude Code (Opus 5) / Unity MCP
- **작업**: `Boss.cs` 전면 재작성. 시대 인덱스로 고르는 패턴 세트 4개 × 3패턴. 종류 8개(`ChargeDash`/`RadialBurst`/`AimedVolley`/`BeamStrike`/`HomingField`/`VentDeploy`/`SlowField`/`BlinkStrike`)를 파라미터만 바꿔 조합한다. 원시=광폭 돌진·대지 분쇄·포식자의 추격 / 중세=화살 세례·심문의 창·처형 낙인 / 현대=십자 폭격·기관포 소사·포탑 전개 / 미래=시간 정지 영역·위상 도약·시간 균열 격자. `WaveManager.ConfigureBoss`에 시대 인덱스 인자 추가, `EraManager`가 `(int)CurrentEra`를 넘긴다.
- **프롬프트 원문**: "스테이지 보스에 맞는 기믹이 보스마다 3개씩은 있었으면 좋겠어"
- **설계 판단과 근거**: 12패턴을 12코루틴으로 쓰면 D-2에 관리가 안 된다 → 종류 8개짜리 스위치 하나 + 시대별 파라미터. 패턴 실체는 전부 **이미 있는 기믹 프리팹의 재사용**이라 새 프리팹·스프라이트가 0이고 WebGL 용량 증가는 코드 몫뿐이다. 기믹 프리팹 참조는 보스 프리팹에 꽂지 않고 씬의 `EraHazardSpawner`/`RiftZoneSpawner`에서 런타임에 빌려온다 — 프리팹 외부 편집은 에디터 모달 위험이 있고(8/5 8분 차단 전례), 씬에 어차피 참조가 하나뿐이라 이득이 없다. 시전(telegraph) 구간은 색이 아니라 **크기 펄스**로 표현했다: `Health`의 피격 플래시가 같은 `SpriteRenderer.color`를 쓰고 있어 색으로 하면 서로 덮어쓴다. 보스 사망 시 남은 예고 레이저·장판·탄을 전부 걷어낸다 — 죽은 보스의 레이저에 맞아 죽는 건 어떤 설명으로도 방어가 안 된다. 3연 돌진 때문에 보스가 화면 밖으로 나가므로 위치를 `ArenaBounds.Clamp`로 붙잡았다(기존엔 클램프가 없었다).
- **검증 방법**: 컴파일 에러 0건. `SerializedObject`로 실측 — `Assets/Prefabs/Boss.prefab`이 신규 필드 `patternSets`를 **코드 기본값 4세트**로 물고 오고(YAML에 없던 필드라 초기화자가 살아남는다) 기존 참조(`projectilePrefab`=BossProjectile, `expOrbPrefab`=ExpOrb)는 유지됨을 확인. 4세트 × 3패턴의 label/kind/followUpRadial 전부 의도한 값. 씬 스포너가 빌려줄 프리팹 5종(HazardBeam/HomingHazard/RiftVent/BossProjectile/RiftZone) 모두 non-null. **씬·프리팹 파일 편집 0건.** 실제 플레이 감각(난이도·읽히는지)은 미실측 — 사용자 Play 모드 확인 대기.
- **AI 산출물 vs 사용자 개입**: 설계·구현·에디터 실측 전량 AI. 밸런스 판단은 사용자 확인 대기.
- **담당**: 개발

### 2026-08-08 (D-2) | 기믹 VFX 전면 개편 — 레이저가 "히트박스 사각형"으로 보이던 문제

- **도구**: Claude Code (Opus 5) / Unity MCP (Play 모드 + Camera_Capture 실측)
- **작업**: `HazardBeam`을 사각형 1장 → **다중 레이어/세그먼트**로 재작성하고 시대별 스타일 4종 추가(원시 균열 / 중세 화살 / 현대 폭격 / 미래 레이저). `EffectSystem`에 Ring·Spray·Linger 추가(조각별 감속·축소율을 들고 있게 해 같은 Update로 처리). 신규 `Hitstop`·`ScreenFlash`·`AmbientParticles` 3종. 보스 돌진 잔상·순간이동 링·격파 다단 폭발.
- **프롬프트 원문**: "1,2,3,4 다 해주는데 뭔가 지금 레이저 기믹은 그냥 히트 박스만 표시하는 느낌이잖아 뭔가 더 레이저나 그 시대에 맞는 느낌이 나게 해줬으면 좋겠어"
- **설계 판단과 근거**: 루트 SpriteRenderer에 스케일을 걸던 구조라 자식을 붙일 수 없었다 → 루트는 스프라이트·정렬 순서 템플릿으로만 쓰고 그리지 않는다. 조각은 Configure 시점에 스타일이 필요한 만큼만 만든다(레이저 5, 균열·폭격 9, 화살 11). 판정(`Contains`)은 손대지 않았다 — 보이는 게 화려해져도 맞는 범위는 예고 막대 그대로여야 한다. `Health.OnHit`을 새로 뺐다: `OnDamaged`가 `Heal()`에서도 나가서 **회복 코어를 먹을 때마다 화면이 흔들리고 있었다**(기존 버그). `Hitstop`은 증강 카드의 `timeScale=0`과 충돌하므로 "내가 넣은 값이 그대로일 때만" 되돌린다. `ScreenFlash`/`AmbientParticles`는 씬에 배치하지 않고 런타임에 카메라 자식으로 자가 생성 — **씬·프리팹 파일 편집 0건**(모달 위험 회피).
- **오진단과 되돌린 값**: 눈으로 안 보고 잡은 초기값이 3연속 빗나갔다. (1) 균열 파편을 `Lerp(color,black,0.72)`로 두니 **흙바닥과 같은 갈색**이라 완전히 안 보였다 → 0.9로 올리니 이번엔 새까만 블록 → 0.8. (2) 시대 색 4종이 전부 이미 밝아서 바탕에 흰색을 섞을수록 채도만 빠졌다(0.55 → 살구색 띠) → 0.12. (3) 폭격 팝을 `sqrt(t)` 크기 + `(1-t)` 알파로 두니 **작을 때 제일 진하고 클 때 제일 옅은** 역방향이라 크고 흐린 사각형만 남았다 → `t^0.35` + `(1-t³)`. (4) 앰비언트 입자는 실측 알파가 0.12·6~11px로 바닥 텍스처에 완전히 묻혔다 → 알파 0.30~0.49 / 7~16px.
- **검증 방법**: Play 모드에서 `timeScale=0.1`로 늦춰 4스타일을 나란히 스폰하고 `Camera_Capture`로 **3회 반복 캡처 → 값 수정 → 재캡처**. 최종적으로 4종이 서로 다른 물건으로 읽히는 것을 눈으로 확인. 조각 renderer 합 34개(빔 4개 기준), 이펙트 풀 96 → 160(코드 바닥값, 씬 편집 없이), 앰비언트 24개 고정. 콘솔 에러 0건. **씬·프리팹 변경 0건**(git status에 `.cs`와 문서만).
- **AI 산출물 vs 사용자 개입**: 설계·구현·실측·값 보정 전량 AI. 실제 플레이 중 체감(히트스톱이 저사양 브라우저에서 렉으로 느껴지는지)은 사용자 확인 대기.
- **담당**: 개발

### 2026-08-08 (D-2) | 커밋·푸시·WebGL 빌드·프로덕션 배포 (보스 기믹 12종 + VFX 반영)

- **도구**: Claude Code (Opus 5) / Unity MCP / vercel CLI
- **작업**: 보스 패턴 12종과 VFX 개편을 커밋 `712934f` 1건으로 push, WebGL 재빌드 후 `deploy.bat`으로 프로덕션 배포.
- **프롬프트 원문**: "전부 다 하면 문제 없으면 커밋 푸시 빌드 배포 해줘"
- **설계 판단과 근거**: 두 작업이 `Boss.cs` 한 파일에 겹쳐 있어 쪼개면 컴파일되지 않는 반쪽 커밋이 된다 — 8/8 앞 커밋(`a2dfbad`)과 같은 이유로 1건으로 묶었다.
- **검증 방법**: 빌드 전 콘솔 에러 0건. `BuildPipeline.BuildPlayer`가 이번에도 Sentis 셰이더 경고로 "failed" 오보고 → 산출물 4종 타임스탬프 갱신(21:21:56~21:22:06)으로 성공 판정. `Build/index.html` 미치환 매크로 0건 + 참조 경로 4종 일치. 배포 후 `curl -I`로 **4종 Content-Length가 로컬과 바이트 단위 전부 일치**(data 25,623,672 / wasm 7,479,099 / framework 75,630 / loader 26,982), `.wasm.br`은 `application/wasm` + `content-encoding: br` 유지. 총 33,189,486 → **33,209,859 bytes (+20,373)** — 신규 스크립트 3개와 기존 10개 수정뿐이고 **새 에셋이 0**이라 증가분이 코드 몫에 그쳤다(내장 Square만 재사용한 판단이 용량에서 확인됐다).
- **AI 산출물 vs 사용자 개입**: 커밋·푸시·빌드 판정·배포·검증 전량 AI 수행. 브라우저 실제 플레이 확인은 사용자 대기.
- **담당**: 개발

### 2026-08-08 (D-2) | 보스전에 시대 기믹이 하나도 안 나오던 문제 + 배경 파티클 제거

- **도구**: Claude Code (Opus 5) / Unity MCP (Play 모드 실측)
- **작업**: `WaveManager.BossActive` 추가. `EraHazardSpawner`·`RiftZoneSpawner`·`RecoveryCoreSpawner`의 게이트를 "잡몹 스폰이 켜져 있을 때만"에서 "보스전에도 돌되 간격만 늘림"으로 변경(빔·장판·분출구 1.7배, 감속 지대 2.0배). 조잡하다는 판단으로 `AmbientParticles.cs` 삭제.
- **프롬프트 원문**: "배경 파티클이 너무 조잡해서 그냥 빼줘 그리고 보스를 할때 맵기믹이 안나오는데 수정해줘"
- **설계 판단과 근거**: 원인은 세 스포너가 전부 `EnemySpawner.SpawningEnabled` 하나를 신호로 쓰고 있었고, `WaveManager`가 보스 등장 시 그것을 꺼버린 것이다. 원래는 의도한 설계였지만(보스 패턴 위에 레이저가 겹치면 피할 수 없는 죽음) 실제로는 보스전 아레나가 텅 비었다. 끄는 대신 **주기를 늘리는** 쪽으로 바꿨다 — 위험은 남기되 동시 발생 확률을 낮춘다. `_bossSpawned`를 그대로 쓰지 않고 `BossActive`를 따로 둔 이유: `_bossSpawned`는 시대 전환까지 true라 보스를 잡고 모래시계를 주우러 가는 동안에도 레이저가 계속 깔린다. `RecoveryCoreSpawner`도 함께 열었다 — 디렉터의 "생존 위기" 개입이 가장 필요한 구간이 보스전인데 정작 그때 회복 코어를 하나도 못 놓고 있었다(같은 게이트에 걸려 있던 별개 버그). 씬 참조는 새로 꽂지 않고 `FindFirstObjectByType`로 런타임 캐시 — 씬 편집 회피.
- **오진단 3연속과 그 교훈**: Play 모드 검증에서 세 번 헛짚었다. (1) `TitleController.StartRun()`을 안 불러 `GameManager.Instance`가 null → `CanFire()`가 false를 반환하는데 이걸 "고친 게 안 먹혔다"로 읽었다. (2) 고쳐서 다시 보니 `IsGameOver=True`·`timeScale=0` — **무인 플레이어가 보스전에서 7.6초 만에 죽어** 스포너가 전부 멎은 것이었다. (3) Play 세션을 오래 켜 둔 탓에 웨이브가 혼자 진행돼 보스가 이미 소환된 오염 상태에서 측정하고 있었다. 셋 다 코드가 아니라 **측정 환경**이 원인이었다.
- **검증 방법**: 점 샘플링(빔 수명 약 2.5초)으로는 못 잡아서, bool을 반환하는 공개 API로 게이트를 직접 쳤다. 보스 소환과 **같은 프레임**에서 `BossActive=True` / `SpawningEnabled=False`(예전 게이트가 막던 바로 그 조건)일 때 `RiftZoneSpawner.SpawnOnPlayer()`와 `RecoveryCoreSpawner.Spawn()`이 **둘 다 true** 반환, 실제 오브젝트 각 1개 생성 확인. 콘솔 에러 0건, 씬·프리팹 변경 0건.
- **AI 산출물 vs 사용자 개입**: 원인 규명·구현·검증 전량 AI. 보스전 기믹 밀도(1.7배/2.0배)가 적당한지는 사용자 확인 대기.
- **담당**: 개발

### 2026-08-08 (D-2) | 이펙트가 "네모네모"하던 문제 — 에셋 대신 텍스처를 코드로 굽기

- **도구**: Claude Code (Opus 5) / Unity MCP (Play 모드 + Camera_Capture 3회 반복)
- **작업**: `FxTextures.cs` 신규 — Dot·Ring·GlowBar·SoftSquare·EdgeGradient·Solid 6종을 런타임 생성. `EffectSystem` 조각을 Square → Dot, `HazardBeam` 조각을 층별로(바탕/화살=GlowBar, 파편=SoftSquare, 폭발·캡=Dot), `ScreenFlash` 가장자리를 진짜 그라데이션으로. `Boss` 돌진에 회당 웅크림(0.35초) 추가 + 속도 12 → 9.
- **프롬프트 원문**: "이팩트들이 그냥 너무 네모네모 너무 조잡한데 에셋스토어에서 무료로 들고올수 있는 어울리는걸로 들고와서 적용해주는건 어때" / (앞선 지적) "보스 돌진이 너무 빨라"
- **설계 판단과 근거**: 에셋스토어를 쓰지 않았다. (1) MCP로 다운로드가 불가능하고 임포트 다이얼로그가 모달이며, (2) 무료 VFX 팩은 대개 ParticleSystem 기반인데 이 프로젝트는 저사양 브라우저 때문에 그것을 일부러 버렸다 — 결국 팩에서 텍스처만 꺼내 쓰게 되므로 직접 굽는 편이 용량·라이선스 모두 이득이다(사용자 승인 후 진행). **전부 64x64에 PPU 64로 굽는다** — 그래야 월드 bounds가 정확히 1x1이라 스프라이트마다 스케일 식이 어긋나지 않는다. 빌드 용량 증가 **0바이트**, RAM 약 96KB. 돌진은 속도만 낮추지 않았다: 3연타 중 **첫 발만 예고가 있고 2·3번째는 무예고**였던 것이 "빠르다"의 실제 원인이라, 돌진마다 웅크림을 붙이고 조준을 웅크림 **뒤로** 옮겼다(미리 조준하면 예고를 보고 움직여도 따라온다).
- **오진단과 되돌린 값**: (1) 링을 조각 12~14개를 원형으로 흩뿌려 만들었더니 조각 자체가 고리라 **"도넛으로 만든 도넛"**이 됐다 → 고리 한 장이 커지거나 오므라들게 바꿨다(조각도 14개 → 1개). (2) 균열 파편 굵기를 0.7~0.95로 두니 뜨거운 바탕을 다 덮어 갈색 덩어리로 보였다 → 0.42~0.68. (3) Play 세션이 오래 켜져 있어 `EffectSystem` 풀이 0으로 읽혔는데 이를 코드 버그로 오해했다. `SetActive` 토글로 `Awake`를 다시 태우려 한 것도 틀렸다(Awake는 재활성 시 실행되지 않는다) — Play를 껐다 켜니 풀 160으로 정상이었다.
- **검증 방법**: `timeScale=0.05`로 늦춰 빔 4종 + 링 2종(확장/수축) + Burst를 한 화면에 놓고 캡처 → 값 수정 → 재캡처를 3회. 최종 캡처에서 4종이 전부 사각형이 아닌 형태로 읽히는 것 확인. FxTextures 6종 bounds 전부 `(1.00, 1.00)` 실측. 돌진: 1회 이동거리 6.60 → **4.95 유닛**, 돌진 사이 반응시간 0.45초(무예고) → **0.85초**(웅크림 포함). 콘솔 에러 0건, 씬·프리팹 변경 0건.
- **AI 산출물 vs 사용자 개입**: 방향 결정은 사용자(에셋 도입 제안 → 코드 생성으로 합의). 구현·실측·값 보정 전량 AI.
- **담당**: 개발

### 2026-08-09 (D-1) | 시대별 기믹을 "느낌" 기준으로 재설계 (균열/찌르기/연쇄/레이저)

- **도구**: Claude Code (Opus 5) / Unity MCP (Play 모드 + Camera_Capture 4회 반복)
- **작업**: `HazardBeam` 4스타일을 사용자가 말한 감각에 맞춰 다시 짰다. 원시=이어진 지그재그 균열, 중세=굵은 창 5자루 찌르기(`FxTextures.Spear` 신규), 현대=끝에서부터 순차 연쇄 폭발, 미래=4겹 레이저 강화.
- **프롬프트 원문**: "미래시대 레이저는 좀 그렇네 원시시대는 땅이 갈라지는 듯한 이팩트면 좋겠고 중세시대는 창으로 콱 찌르는 느낌이면 좋겠고 현대시대는 끝에서 부터 펑펑펑펑펑 순서대로 터지는 느낌이면 좋겠어"
- **설계 판단과 근거**: 세 가지 다 **"조각을 흩뿌리는" 방식으로는 안 되는 것**이었다. (1) 균열: 마디를 각자 랜덤 위치·각도로 두면 얼룩덜룩한 막대다. 빔 위에 꼭짓점을 위아래 번갈아 찍고 **마디가 꼭짓점 두 개를 잇게** 해서 길이·각도가 자동으로 정해지게 했다 — 그래야 선이 끊기지 않는다. (2) 찌르기: 얇은 화살 10개를 순차로 흘리면 "빗줄기"지 "찌르기"가 아니다. 5자루로 줄이고 시간차를 12%로 좁혀 거의 동시에, 길이 1.8배에서 0.09초 만에 제 길이로 박히게 했다 — 이 **낙차**가 "콱"이다. 막대로는 안 돼서 촉이 뾰족한 스프라이트를 새로 구웠다. (3) 연쇄: 이미 순차였는데도 안 읽힌 이유는 시간차가 50%인데 폭발 하나가 35%나 살아 있어 전부 겹쳤기 때문이다. 시간차 85% / 수명 22%로 바꾸고 위치 랜덤 흔들기를 뺐다(인덱스 순서 = 빔 위 순서여야 "끝에서부터"가 성립). (4) 레이저는 방향 유지 요청이라 겹을 4장으로 늘려 광량을 만들었다 — 한 겹만 밝게 해서는 세지지 않는다.
- **되돌린 값**: 균열 지그재그 진폭 0.34 → 0.27, 굵기 0.46 → 0.38 (판정 띠 밖으로 크게 벗어남). 창 길이 2.3배 → 1.5배 (촉이 띠 밖으로 1.5유닛 초과). 레이저 헤일로 1.9배·0.14 → 1.55배 — **격자 4줄의 헤일로가 겹쳐 바닥 전체가 보라색으로 물들면서 어디까지가 판정인지 안 읽혔다.** 피하는 게임에서 그게 제일 나쁘다.
- **검증 방법**: `timeScale` 0.05~0.15로 늦춰 스타일별 캡처 4회. 균열이 한 줄로 이어져 왼쪽부터 순서대로 벌어지는 것, 창 5자루가 촉을 아래로 박는 것, 폭발이 왼쪽 끝부터 하나씩 켜지는 것, 격자 4줄이 흰 코어를 가진 네온으로 보이는 것을 각각 확인. 콘솔 에러 0건, 씬·프리팹 변경 0건, 새 에셋 0.
- **AI 산출물 vs 사용자 개입**: 감각 방향은 전부 사용자 지시. 구조 설계·구현·실측·값 보정은 AI.
- **담당**: 개발

### 2026-08-09 (D-1) | 빌드·배포 (기믹 재설계 + VFX + 보스전 기믹 반영)

- **도구**: Claude Code (Opus 5) / Unity MCP / vercel CLI
- **작업**: `58b0e58`까지의 커밋 5건(보스전 기믹 활성화, VFX 텍스처 코드 생성, 돌진 예고, 기믹 4종 재설계)을 WebGL 빌드 후 프로덕션 배포.
- **프롬프트 원문**: "커밋 푸시 빌드 배포 해줘"
- **설계 판단과 근거**: 커밋·푸시는 직전 작업에서 이미 끝나 있어(`main` == `origin/main`) 빌드·배포만 수행했다.
- **검증 방법**: 빌드 전 콘솔 에러 0건. **이번에는 `BuildPipeline`이 `Succeeded`를 정직하게 반환했다**(에러 0, 3분 28초) — 앞선 두 번의 "failed"가 Sentis 셰이더 경고에 의한 오보고였음이 대조로 확인됐다. 산출물 4종 전부 01:34 갱신, `index.html` 미치환 매크로 0건. 배포 후 `curl -I`로 4종 Content-Length가 로컬과 **바이트 단위 전부 일치**(data 25,611,754 / wasm 7,471,832 / framework 75,687 / loader 26,982), `.wasm.br`은 `application/wasm` + `br` 유지. 총 33,209,859 → **33,190,731 bytes (−19,128)** — 새 스크립트(FxTextures)가 늘었는데도 줄었다. `AmbientParticles` 삭제분이 더 컸다. 텍스처를 코드로 굽는 판단이 용량에서 재확인됐다(에셋 팩이었다면 증가했을 자리다).
- **AI 산출물 vs 사용자 개입**: 빌드 판정·배포·검증 전량 AI. 브라우저 실제 플레이 확인은 사용자 대기.
- **담당**: 개발

### 2026-08-09 (D-1) | 컬러 아트 수용 배선 (틴트 → 스프라이트 교체, 하위호환)

- **도구**: Claude Code (Opus 5) / Unity MCP (ValidateScript + RunCommand 컴파일 프로브)
- **작업**: 디자이너 컬러 아트 9장(몹 4·보스 4·플레이어 1)이 도착하기 전에 받을 자리를 먼저 깔았다. `EraConfig`에 `enemySprite`/`bossSprite` 추가, `Health.SetAppearance(sprite, accent)` 신설, 피격 플래시·VFX 색 경로 분리.
- **프롬프트 원문**: "일단 지금 캐릭터랑 몹, 보스 스프라이트를 디자이너가 오늘 중으로 줄거야 그리고 그 컨셉에 맞춰서 보스 기믹을 수정할거고 시대별 몹 강함을 조절할거야"
- **설계 판단과 근거**: (1) **전량 하위호환으로 짰다** — `sprite == null`이면 기존 `SetBaseColor` 경로로 그대로 떨어진다. D-1에 9장이 한꺼번에 오지 않을 가능성이 높아, 온 것부터 시대별로 순차 적용되게 하려는 것. 아트 도착 전에도 현재 빌드가 안 깨진다. (2) **피격 플래시가 컬러 아트에서 무력화된다**: `SpriteRenderer.color`는 곱연산이라 흰색을 곱하면 화면 변화가 0이다. 흰 사각형 시절엔 먹히던 연출이 그대로 죽는다 — 셰이더 추가(WebGL 용량) 대신 붉은 곱연산 틴트로 바꿨다. (3) **`enemyColor`를 지우지 않고 의미만 바꿨다**: 틴트 → 파편·스폰 표식의 강조색. 이 4색은 배경 대비를 실측해 고른 값이라 버리기 아깝고, 지우면 `EnemySpawner.PortalColor`까지 흰색이 된다. `Health`에 `AccentColor`를 따로 두어 `BaseColor`(플래시 복귀용)와 분리하고 VFX 15곳을 옮겼다.
- **검증 방법**: `Unity_ValidateScript` 4개 파일 구문 오류 0건. 콘솔이 완전히 비어 있어(`totalCount: 0`) 재컴파일 여부가 불확실했으므로, **리플렉션 없이 새 멤버를 직접 참조하는 `Unity_RunCommand` 프로브**로 어셈블리 반영을 확정했다(`isCompilationSuccessful: true`, `enemySprite`/`bossSprite` 모두 null 기본값 확인). 씬·프리팹 변경 0건, 새 에셋 0.
- **미검증**: 실제 아트를 꽂은 뒤의 판정 일치(콜라이더 1×1 고정)와 붉은 플래시 가독성. 아트 도착 후 실측 예정.
- **AI 산출물 vs 사용자 개입**: 아트 형태 결정(시대별 완성 컬러 9장)은 사용자. 구조 설계·구현·검증은 AI.
- **담당**: 개발

### 2026-08-09 (D-1) | 시대별 몹 강함·물량을 컨셉 단위로 분리

- **도구**: Claude Code (Opus 5) / Unity MCP (SerializedObject 주입 + 편집 모드 배선 실측)
- **작업**: 물량 노브가 아예 없었다 — `ResetForNewEra()`가 4시대 전부 같은 `_initialSpawnInterval`로 되돌리고 `maxAlive`는 시대별로 손대지 않았다. `EraConfig`에 `enemySpeedMultiplier`/`spawnInterval`/`maxAlive`를 추가하고 `Enemy.ApplySpeedMultiplier()`를 신설했다. 인자가 6개로 늘어 `ConfigureEnemyScaling`을 `EraConfig` 하나 받는 형태로 바꿨다(호출부 1곳).
- **프롬프트 원문**: "시대별 몹 강함 부터 해줘 각 시대별로 컨셉에 맞게 물량도 조절되고 강함도 조절되게 해줘"
- **설계 판단과 근거**: 시대마다 '더 단단해짐'만 반복하면 질감이 안 갈린다고 보고 **압박의 출처를 시대별로 다르게** 뒀다. 원시=야수 떼(HP 0.85·간격 1.7·상한 48), 중세=중장 보병(HP 1.45·속도 0.85·간격 2.4·상한 30), 현대=기계화(HP 1.30·속도 1.15·간격 1.6·상한 46), 미래=소수 정예(HP 2.10·속도 1.25·간격 2.2·상한 34). **현대 HP를 중세보다 낮춘 것이 핵심 결정**이다 — 중세는 탱킹, 현대는 물량·속도로 압박해야 두 시대가 구분된다. 속도는 스폰 직후 1회만 곱한다(적 40마리 이상 × 저사양 브라우저라 Update 매 프레임 곱셈을 피했다). 난입 적은 `SpawnWithPortal`이 `OnEnemySpawned`를 쏘지 않아 이중 적용이 없음을 확인하고 `Dress`에서 직접 걸었다.
- **검증 방법**: 초당 유입 HP(개체 HP ÷ 스폰 간격)로 검산 — 기존 `12.5/18/21.75/26.25` → 신규 `12.5/18.1/24.4/28.6`으로 단조 증가하며 원시·중세는 기존과 동일하게 맞췄다. 적 최대 속도 3.25 < 플레이어 6이라 카이팅 성립 확인. `SerializedObject` 주입 후 씬 4개 항목 기록 확인, 실제 씬 오브젝트로 `ConfigureEnemyScaling` 호출해 스포너가 `2/40 → 1.7/48`로 바뀌고 원복되는 것까지 실측. 콘솔 에러 0건.
- **오진단**: "새 필드는 씬에서 0으로 역직렬화되니 가드가 없으면 적이 얼어붙는다"고 주석에 적었으나 **틀렸다.** `eraConfigs`가 MonoBehaviour 배열 초기화식이라 역직렬화 때 C# 값이 먼저 깔리고 씬에 존재하는 필드만 덮인다(주입 로그에서 HP만 옛 값, 신규 3종은 C# 값으로 찍혀 발견). 가드는 남기되 근거를 '인스펙터 미입력 대비'로 정정했다.
- **미검증**: 사람이 실제로 플레이한 체감. 특히 현대 HP 하향이 '약해졌다'로 읽히는지, 원시 상한 48이 저사양 브라우저에서 프레임을 깎는지(풀링 없음).
- **담당**: 개발

### 2026-08-09 (D-1) | 중세 기믹을 전기로 교체 + Kenney 스프라이트 5종 도입

- **도구**: Claude Code (Opus 5) / Unity MCP (Play 모드 캡처 3회 + TextureImporter 주입)
- **작업**: 중세 시대 기믹을 창 찌르기(Volley) → **전기 방전(Electric)** 으로 교체하고, 절차 생성으로는 안 나오는 질감 5곳에 Kenney Particle Pack(CC0) 스프라이트를 물렸다. `FxSprites.cs` 신설, `EffectSystem.Burst/Spray/Linger`에 `shape` 인자 개방.
- **프롬프트 원문**: "니가 생각했을때 제일 맘에 드는걸로 넣어줘 내가 이야기 했던 시대별 맵 이팩트 설명과 비슷한걸로 해주고 중세시대만 전기로 바꾸자 다 적용해줘 그리고 커밋 푸시 빌드 배포 해줘"
- **설계 판단과 근거**: 전기는 균열의 지그재그 뼈대를 **재사용**했다(`BuildFissureNodes` → `BuildZigzagNodes` 매개변수화). 같은 구조로 정반대 인상을 만드는 게 관건이라 셋을 갈랐다 — (1) 순서 없이 한 번에 켜기(균열 0.45 → 전기 0.06), (2) 훨씬 얇고 밝게, (3) 마디마다 위상을 어긋낸 깜빡임. 스프라이트는 **절차 생성이 못 하는 것에만** 썼다: 폭격 폭발=Flame(매끈한 원은 "빛나는 공"이지 폭발이 아니다), 균열 흙먼지·보스 잔상=Smoke, 전기 관절·적 사망·레이저 발사구=Spark, 감속 지대=Twirl. `Electric`은 **enum 끝에 추가**했다 — 중간에 끼우면 씬에 직렬화된 보스 패턴의 `beamStyle` 인덱스가 전부 밀린다. 로딩은 Resources 정적 지연 로드로 했다(씬·프리팹에 참조 필드를 추가하는 것이 하드 룰과 부딪힌다).
- **오진단·되돌린 값**: (1) **PPU 함정** — `maxTextureSize 128`로 줄였더니 Unity가 스프라이트 PPU를 128→32로 자동 보정해 **bounds가 4×4**가 됐다. `EffectSystem`/`HazardBeam`은 "스프라이트는 1유닛"을 하드코딩하고 있어 모든 이펙트가 4배로 나올 뻔했다. 월드 크기는 해상도가 아니라 `원본 512px ÷ 임포터 PPU`로 정해진다 — PPU를 512로 잡아 1×1 확정. (2) 전기 코어 `Lerp(색,흰색,0.85)` → **0.62** (순백이라 강철청이 사라져 중세로 안 읽혔다). (3) 바탕 띠 알파 0.28 → **0.5** (모래 배경 위에서 판정 범위가 아예 안 보였다). (4) 마디 굵기 0.07~0.13 → 0.10~0.17.
- **검증 방법**: Play 모드 캡처로 4시대를 한 화면에 놓고 비교(원시 검은 지그재그 / 중세 흰 아크 + 강철청 띠 / 미래 레이저 끝단 캡). **Play 중에는 스크립트가 재컴파일되지 않아 1차 캡처가 수정 전 코드였다** — Play 종료 → 재컴파일 → 재진입으로 반영 확인. 스프라이트 5종 `bounds=(1.00,1.00)` 실측, enum 정수 `Fissure=0/Volley=1/Bombardment=2/Laser=3/Electric=4`로 기존 값 불변 확인. 아크 진폭 0.40×폭 1.4=0.56 < 판정 반폭 0.7 확인. 콘솔 에러 0건.
- **미검증**: 사람이 실제로 플레이한 체감, 저사양 브라우저에서 아크 10마디 × 격자 다중 소환 시 프레임.
- **담당**: 개발

### 2026-08-09 (D-1) | 클리어 타임 랭킹 (로그인 없이 이름만)

- **도구**: Claude Code (Opus 5) / Unity MCP (씬 UI 생성 + SerializedObject 배선) / Vercel CLI / curl
- **작업**: io게임식 랭킹. Vercel 서버리스 함수(`deploy/api/leaderboard.js`) + Upstash Redis sorted set, Unity 쪽은 `Leaderboard.cs`(UnityWebRequest) + 타이틀 이름 입력칸 + 결과 화면 오른쪽 랭킹 카드.
- **프롬프트 원문**: "그 지렁이 게임 알아? io게임 말이야 거기선 로그인 안하고 이름만 적고 랭킹이 뜨던데 우리도 이름만 적고 랭킹을 볼수있게 하고 싶은데 어떻게 해야할까"
- **설계 판단과 근거**: **같은 도메인에 붙인 것이 핵심**이다 — WebGL 리더보드가 깨지는 1순위 원인이 CORS인데 게임이 이미 Vercel에 있으므로 `api/`를 얹으면 same-origin이라 아예 발생하지 않는다(서드파티 dreamlo류를 기각한 이유). URL도 하드코딩 대신 `Application.absoluteURL`에서 오리진을 뽑는다 — 프리뷰 배포에서 열리면 프로덕션 URL이 교차 출처가 된다. `UnityWebRequest`는 소켓·`System.Net`을 타지 않아 CLAUDE.md 통신 금지 규칙에 걸리지 않는다(WebGL의 유일한 정식 수단). 개인 최고 기록만 남기려고 `ZADD LT`를 썼다. UGS는 새 패키지+용량이라 기각.
- **폰트가 진짜 병목이었다**: 서브셋 159자에 대문자가 17자뿐이고(`F G J N Q U X Y Z` 없음) 한글 음절은 원천적으로 못 담는다. 이름을 A-Z 0-9로 제한하고 표시를 TMP 내장 `LiberationSans SDF`로 돌려 **폰트 재굽기를 0으로 만들었다**. 카드 문구를 영문(`LOADING... / NO RECORDS YET / OFFLINE`)으로 둔 것도 같은 이유다 — "불러오는 중/기록 없음/연결 실패"의 글자가 서브셋에 하나도 없어 통째로 □가 된다.
- **검증 방법**: curl로 6종 실측 — 한글·소문자 이름 400, 180초 미만 400, 1시간 초과 400, 같은 이름 느린 기록은 미갱신(332100 유지), 빠른 기록은 갱신(280000), 2건 오름차순 정렬 확인. 배포 후 산출물 4종 Content-Length 로컬과 바이트 일치. 4:3 화면에서 랭킹 카드가 잘리는 것을 CanvasScaler 공식으로 계산해 발견(보이는 반폭 831 < 카드 끝 840) → 클리어 시에만 결과 창을 -230 밀어 span −610..610으로 교정.
- **오진단·막힌 것**: (1) 빌드 `locationPathName`을 `"Build/Build"`로 줘 산출물이 한 단계 깊게 생성됐다(생성된 index.html이 기존과 동일함을 확인하고 이동으로 처리). (2) 테스트 데이터를 지우려 `vercel env pull`로 토큰을 받으려 했으나 **Sensitive 지정 변수는 되읽기가 막힌다**(`URL=""`). 함수 런타임 읽기는 정상이라 설계상 올바른 동작이고, 정리는 Upstash 콘솔에서 사람이 해야 한다.
- **미검증**: 브라우저에서 사람이 실제로 이름 입력 → 클리어 → 순위 표시까지 완주한 기록. 4시대 클리어가 어려워 순위표가 빈 채로 보일 가능성(사망 기록 포함 여부는 사용자 판단 대기).
- **담당**: 개발
