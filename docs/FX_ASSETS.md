# FX / 이펙트 무료 에셋 후보 (2026-08-09 조사)

> 결론부터: **1위 Kenney Particle Pack (CC0)**. 유일하게 지금 코드에 코드 변경 없이 꽂힌다.
> 다만 **아무것도 안 넣는 것이 여전히 유효한 선택지**다. 아래 "0순위" 항목을 먼저 볼 것.

---

## 판단 기준

이 프로젝트 기준으로 가중치를 매겼다. 일반적인 "좋은 FX 팩" 순위와 다르다.

| 순위 | 기준 | 이유 |
|---|---|---|
| 1 | **WebGL 동작 여부** | 안 돌면 나머지는 볼 필요가 없다 |
| 2 | **빌드 용량 증가분** | 용량 = 로딩 시간 (CLAUDE.md) |
| 3 | **기존 코드에 꽂히는가** | D-1. 새 시스템을 붙일 시간이 없다 |
| 4 | 라이선스 | CC0 > CC BY > Asset Store EULA |
| 5 | 저사양 브라우저 프레임 | 목표 사양이 저사양 노트북 브라우저 |
| 6 | 아트 톤 정합성 | 단색 도형 + 색 틴트 기조 |

### 실격 사유가 되는 것

**VFX Graph 기반 팩은 전부 실격이다.** VFX Graph는 compute shader를 요구하고
**WebGL2는 compute shader를 지원하지 않는다.** WebGPU에서는 되지만 실험 단계라 프로덕션 비권장이다.
무료 FX 팩 추천 목록의 상당수가 여기 걸리므로 받기 전에 반드시 확인할 것.
→ 대안은 **레거시 파티클 시스템(Shuriken)** 또는 **스프라이트 시트**뿐이다.

---

## 0순위 — 아무것도 안 넣기 (현행 유지)

**이걸 먼저 기각한 뒤에 아래를 보는 게 맞다.**

- `FxTextures.cs`가 6종을 런타임에 절차 생성한다 (Dot / Ring / GlowBar / SoftSquare / EdgeGradient / Solid / Spear).
- 실측 근거: VFX 전면 개편 + 기믹 4종 재설계를 하고도 빌드가 **19,128 bytes 줄었다.**
  새 에셋을 하나도 안 썼기 때문이다. 에셋 팩 도입은 이 이점을 정확히 되돌리는 방향이다.
- 제출 문서 관점에서도 "제약 때문에 코드 생성을 택했고 용량으로 검증됐다"가 강한 서술이다.

**그럼에도 도입할 이유**: 절차 생성으로는 만들기 어려운 것 — 연기, 불꽃, 파편 디테일, 텍스처 노이즈.
지금 이펙트가 "기하학적"으로 보이는 건 그 때문이다. 톤을 바꾸고 싶다면 아래 1위가 답이다.

---

## 1위 — Kenney Particle Pack ⭐ 추천

| | |
|---|---|
| 링크 | https://kenney.nl/assets/particle-pack |
| 라이선스 | **CC0** (출처 표기 의무 없음) |
| 구성 | 80종 스프라이트 + 라이트 쿠키 + 셰이더 |
| WebGL | ✅ 단순 PNG 스프라이트 |
| 미러 | [OpenGameArt](https://opengameart.org/content/particle-pack-80-sprites) · [Kenney 전체 CC0](https://opengameart.org/content/all-cc0-uploader-kenney) |

**추천 이유 — 코드를 안 고쳐도 된다.**

`EffectSystem.Spawn()`이 이미 스프라이트를 인자로 받는다:

```csharp
// EffectSystem.cs:178
float size, float lifetime, float dragValue, float shrink, Sprite shape = null)
// EffectSystem.cs:189
_sr[slot].sprite = shape != null ? shape : FxTextures.Dot;
```

즉 **PNG를 임포트해서 `shape` 자리에 넘기면 끝이다.** 새 프리팹도, 새 파티클 시스템도,
새 패키지도 필요 없다. 풀링·수명·감속 로직이 전부 그대로 재사용된다.
CLAUDE.md의 "새 프리팹 추가 금지", "새 워크플로우 도입 금지"에도 걸리지 않는다.

**용량**: 필요한 3~5종만 골라 담으면 무시할 수준이다. 80종을 통째로 넣지 말 것.
(전체 팩 다운로드 용량은 미확인 — 어차피 선별해서 넣을 것이라 무의미하다.)

**주의**: Kenney 스프라이트는 흰색/밝은 단색 위주다.
시대 색을 `SpriteRenderer.color`로 곱해 쓰는 현재 방식과 잘 맞는다 —
다만 **VFX 작업 때 실측된 것처럼 흰색을 섞을수록 배경에 묻힌다.** 대비는 어둠으로 만들 것.

**같은 계열 추가 후보**: [Smoke Particles](https://kenney.nl/assets/smoke-particles) (CC0),
[particle 태그 전체](https://kenney.nl/assets/tag:particle)

---

## 2위 — itch.io 2D 스프라이트 시트 FX

| | |
|---|---|
| 링크 | [free + effects](https://itch.io/game-assets/free/tag-effects) · [2D + explosions](https://itch.io/game-assets/free/tag-2d/tag-explosions) · [CC0 + pixel art](https://itch.io/game-assets/free/tag-cc0/tag-pixel-art) |
| 라이선스 | **팩마다 다름 — 반드시 개별 확인** |
| WebGL | ✅ 스프라이트 시트 |

프레임 애니메이션이라 폭발·참격 같은 "찰나의 임팩트"가 절차 생성보다 훨씬 좋다.

**도입 비용이 1위보다 크다.** `EffectSystem`은 스프라이트 1장을 늘리고 줄이는 구조라
**프레임 전환 로직이 없다.** 시트를 쓰려면 Update에 프레임 인덱스를 돌리는 코드가 붙는다
(작긴 하다 — `_sr[i].sprite = frames[(int)(t * fps)]` 수준).

**톤 위험**: 대부분 픽셀아트다. 지금 게임은 픽셀아트가 아니라 매끈한 도형이라 **섞으면 어색해진다.**
쓸 거면 픽셀아트가 아닌 팩을 고를 것.

**라이선스 함정**: itch.io는 팩마다 조건이 제각각이고 "free"가 "무제한 상업 이용 가능"을 뜻하지 않는다.
제출작이므로 각 팩의 라이선스 파일을 직접 읽고 `docs/AI_LOG.md` 출처 표에 기재해야 한다.

---

## 3위 — Cartoon FX Remaster Free (Jean Moreno / JMO Assets)

| | |
|---|---|
| 링크 | [Asset Store](https://assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565) · [제작자 사이트](https://jeanmoreno.com/unity/cartoonfxfree/) |
| 라이선스 | 무료 / **Unity Asset Store EULA** (CC0 아님) |
| 구성 | 50종 이펙트 + Cartoon FX Easy Editor |
| WebGL | ✅ Shuriken 기반 (VFX Graph 아님) |

폭발·연기·불·물보라·마법·전기 등 구색이 좋고, 2D·URP 양쪽을 지원하며 색/크기 조정 에디터가 딸려 온다.
**품질만 보면 이 목록에서 가장 좋다.**

**그런데 3위인 이유:**

1. **프리팹 기반 파티클 시스템 50종**이 들어온다. CLAUDE.md의 "요청 없는 새 프리팹 추가 금지"와
   정면으로 부딪히고, 지금의 코드 주도 FX 구조와 워크플로우가 이원화된다.
2. **용량이 1위와 비교가 안 된다.** 50종 전부의 텍스처가 따라온다.
   (정확한 용량 미확인 — Asset Store 페이지가 JS 렌더링이라 조회 실패했다. 받기 전에 확인 필요.)
3. 카툰 톤이라 "시간 이상" 컨셉과 붙는지 봐야 한다.

**쓴다면**: 프리팹 50개를 전부 넣지 말고 **필요한 2~3종만 남기고 나머지를 삭제**한 뒤 빌드할 것.

---

## 4위 — Unity Particle Pack (Unity Technologies) ❌ 사실상 실격

| | |
|---|---|
| 링크 | https://assetstore.unity.com/packages/vfx/particles/particle-pack-127325 |
| 라이선스 | 무료 |
| **다운로드 용량** | **157.4 MB** |

**현재 빌드 전체가 33.2 MB다. 이 팩 하나가 그 5배다.**
선별해서 쓰면 되지만, 그럴 거면 1위가 모든 면에서 낫다. 기록용으로만 남긴다.

---

## 요약

| 순위 | 후보 | 라이선스 | WebGL | 코드 변경 | 용량 |
|---|---|---|---|---|---|
| 0 | **현행 유지 (코드 생성)** | — | ✅ | 없음 | **0** |
| 1 | **Kenney Particle Pack** | CC0 | ✅ | **없음** | 매우 작음 |
| 2 | itch.io 스프라이트 시트 | 팩별 상이 | ✅ | 프레임 로직 필요 | 작음 |
| 3 | Cartoon FX Remaster Free | AS EULA | ✅ | 프리팹 도입 | 중간 |
| 4 | Unity Particle Pack | 무료 | ✅ | 프리팹 도입 | **157.4 MB** |
| — | VFX Graph 기반 팩 전부 | — | ❌ | — | — |

## 권고

**D-1에 넣는다면 1위만.** 그것도 스프라이트 3~5장 선별해서
`EffectSystem.Spawn(..., shape:)`에 넘기는 선까지다. 그 이상은 마감 대비 위험이 크다.

도입을 확정하면 CLAUDE.md에 따라 **같은 커밋에서 `docs/AI_LOG.md` 하단 출처 표에
라이선스와 함께 행을 추가**해야 한다.

## 참고 링크

- VFX Graph WebGL 미지원 근거: [Unity Manual — Web graphics APIs](https://docs.unity3d.com/6000.2/Documentation/Manual/webgl-graphics.html) ·
  [Unity Discussions](https://discussions.unity.com/t/webgl-with-visual-effect-graph/803212)
- [Kenney 전체 에셋](https://kenney.nl/assets)
- [OpenGameArt](https://opengameart.org/)
