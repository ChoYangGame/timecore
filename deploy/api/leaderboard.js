// TimeCore 랭킹 API (Vercel Serverless Function)
//
// GET  /api/leaderboard        → 상위 10개
// POST /api/leaderboard        → { name, ms } 등록 후 상위 10개 반환
//
// 저장소는 Upstash Redis(REST). sorted set 하나만 쓴다.
//   key   : timecore:lb
//   score : 클리어 시간(ms) — 낮을수록 상위
//   member: 플레이어 이름
//
// 게임과 같은 도메인(timecore-chi.vercel.app)에 배포되므로 CORS가 발생하지 않는다.
// WebGL 리더보드가 깨지는 1순위 원인이 CORS라 이 점이 서드파티 서비스 대비 가장 큰 이점이다.

const KEY = 'timecore:lb';
const TOP_N = 10;

// 이름은 대문자 영문 + 숫자만. 폰트 서브셋(159자)에 한글 이름을 담을 수 없어
// 클라이언트가 LiberationSans로 표시하는데, 그쪽도 ASCII만 확실하다.
const NAME_RE = /^[A-Z0-9]{1,8}$/;

// 웨이브 타이머만으로 시대당 최소 60초(15초 × 4웨이브)가 강제된다.
// 4시대면 이론적 최소가 240초라, 180초 미만은 조작으로 보고 거절한다.
const MIN_MS = 180000;
const MAX_MS = 3600000;

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

/** ZRANGE는 [member, score, member, score, ...] 평면 배열로 온다. */
async function top() {
  const flat = await redis(['ZRANGE', KEY, '0', String(TOP_N - 1), 'WITHSCORES']);
  const out = [];
  for (let i = 0; i + 1 < flat.length; i += 2) {
    out.push({ name: flat[i], ms: Number(flat[i + 1]) });
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

      if (!NAME_RE.test(name)) {
        return res.status(400).json({ error: 'name must be 1-8 chars of A-Z 0-9' });
      }
      if (!Number.isFinite(ms) || ms < MIN_MS || ms > MAX_MS) {
        return res.status(400).json({ error: `ms must be ${MIN_MS}..${MAX_MS}` });
      }

      // LT: 같은 이름이 이미 있으면 "더 빠를 때만" 갱신한다. 신규 추가는 그대로 된다.
      // 이게 없으면 같은 이름으로 느린 기록을 내면 best가 덮여 사라진다.
      await redis(['ZADD', KEY, 'LT', String(ms), name]);

      return res.status(200).json({ entries: await top() });
    }

    res.setHeader('Allow', 'GET, POST');
    return res.status(405).json({ error: 'method not allowed' });
  } catch (e) {
    // 랭킹이 죽어도 게임은 돌아야 한다. 클라이언트는 실패를 조용히 무시한다.
    return res.status(500).json({ error: String(e.message || e) });
  }
};
