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
