// TimeCore 랭킹 API (Vercel Serverless Function)
//
// GET  /api/leaderboard   → 상위 10개
// POST /api/leaderboard   → { name, era, cleared, ms } 등록 후 상위 10개 반환
//
// 저장소는 Upstash Redis(REST). sorted set 하나만 쓴다.
//   key   : timecore:lb
//   member: 플레이어 이름
//   score : "진행도" — 높을수록 상위
//
// 진행도를 하나의 숫자로 접는 이유: 정렬 기준이 두 개(도달 시대 → 시간)인데
// sorted set은 점수가 하나뿐이다. 시대를 큰 자리, 시간을 작은 자리에 넣어 한 숫자로 만든다.
//
//   클리어  : 4 * ERA_BLOCK + (MAX_MS - 클리어타임)   → 빠를수록 높다
//   사망    : 시대 * ERA_BLOCK + 생존시간             → 오래 버틸수록 높다
//
// 클리어는 항상 시대 4 자리를 쓰므로 어떤 사망 기록보다도 위에 온다.
// 게임과 같은 도메인에 배포되므로 CORS가 발생하지 않는다 —
// WebGL 리더보드가 깨지는 1순위 원인이라 서드파티 서비스 대비 가장 큰 이점이다.

const KEY = 'timecore:lb';
const TOP_N = 10;

// 이름은 대문자 영문 + 숫자만. 폰트 서브셋(159자)에 한글 이름을 담을 수 없어
// 클라이언트가 LiberationSans로 표시하는데, 그쪽도 ASCII만 확실하다.
const NAME_RE = /^[A-Z0-9]{1,8}$/;

const MAX_MS = 3600000;        // 시간 상한 1시간. 진행도의 '작은 자리' 폭이기도 하다
const ERA_BLOCK = 10000000;    // MAX_MS보다 충분히 커야 시대끼리 자리가 안 섞인다
const CLEAR_ERA = 4;           // 클리어는 마지막 시대(3)보다 한 칸 위

// 웨이브 타이머만으로 시대당 최소 60초(15초 × 4웨이브)가 강제된다.
// 4시대면 이론적 최소가 240초라, 클리어 기록이 180초 미만이면 조작으로 보고 거절한다.
const MIN_CLEAR_MS = 180000;

function encode(era, cleared, ms) {
  return cleared
    ? CLEAR_ERA * ERA_BLOCK + (MAX_MS - ms)
    : era * ERA_BLOCK + ms;
}

function decode(score) {
  const era = Math.floor(score / ERA_BLOCK);
  const rest = score - era * ERA_BLOCK;

  return era >= CLEAR_ERA
    ? { era: CLEAR_ERA - 1, cleared: true, ms: MAX_MS - rest }
    : { era, cleared: false, ms: rest };
}

async function redis(command) {
  const url = process.env.UPSTASH_REDIS_REST_URL;
  const token = process.env.UPSTASH_REDIS_REST_TOKEN;
  if (!url || !token) throw new Error('UPSTASH env vars missing');

  const res = await fetch(url, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(command),
  });

  if (!res.ok) throw new Error(`upstash ${res.status}: ${await res.text()}`);
  const json = await res.json();
  if (json.error) throw new Error(`upstash: ${json.error}`);
  return json.result;
}

/** ZRANGE REV는 [member, score, member, score, ...] 평면 배열로 온다. */
async function top() {
  const flat = await redis(['ZRANGE', KEY, '0', String(TOP_N - 1), 'REV', 'WITHSCORES']);
  const out = [];
  for (let i = 0; i + 1 < flat.length; i += 2) {
    const d = decode(Number(flat[i + 1]));
    out.push({ name: flat[i], era: d.era, cleared: d.cleared, ms: d.ms });
  }
  return out;
}

module.exports = async (req, res) => {
  // 브라우저가 결과를 캐시하면 새 기록이 안 보인다.
  res.setHeader('Cache-Control', 'no-store');

  try {
    if (req.method === 'GET') {
      return res.status(200).json({ entries: await top() });
    }

    if (req.method === 'POST') {
      // Vercel이 Content-Type: application/json이면 이미 파싱해 준다.
      const body = typeof req.body === 'string' ? JSON.parse(req.body || '{}') : (req.body || {});

      const name = String(body.name || '').trim().toUpperCase();
      const ms = Math.round(Number(body.ms));
      const era = Math.round(Number(body.era));
      const cleared = body.cleared === true;

      if (!NAME_RE.test(name)) {
        return res.status(400).json({ error: 'name must be 1-8 chars of A-Z 0-9' });
      }
      if (!Number.isFinite(ms) || ms < 0 || ms > MAX_MS) {
        return res.status(400).json({ error: `ms must be 0..${MAX_MS}` });
      }
      if (!Number.isInteger(era) || era < 0 || era > CLEAR_ERA - 1) {
        return res.status(400).json({ error: `era must be 0..${CLEAR_ERA - 1}` });
      }
      if (cleared && ms < MIN_CLEAR_MS) {
        return res.status(400).json({ error: `cleared ms must be >= ${MIN_CLEAR_MS}` });
      }

      // GT: 같은 이름이 이미 있으면 "더 좋은 기록일 때만" 갱신한다. 신규 추가는 그대로 된다.
      // 이게 없으면 같은 이름으로 나쁜 판을 하면 best가 덮여 사라진다.
      await redis(['ZADD', KEY, 'GT', String(encode(era, cleared, ms)), name]);

      return res.status(200).json({ entries: await top() });
    }

    res.setHeader('Allow', 'GET, POST');
    return res.status(405).json({ error: 'method not allowed' });
  } catch (e) {
    // 랭킹이 죽어도 게임은 돌아야 한다. 클라이언트는 실패를 조용히 무시한다.
    return res.status(500).json({ error: String(e.message || e) });
  }
};
