// UGS 대시보드 Cloud Code > Scripts > ReportMatch 에 그대로 붙여넣고 Publish
// 파라미터: isWin(Boolean), mySets(Integer), opponentSets(Integer), opponentId(String, 선택)
const { LeaderboardsApi } = require("@unity-services/leaderboards-2.0");

const LEADERBOARD_ID = "Ranking";
const SETS_TO_WIN = 4;
// 진 쪽이 딴 세트 수(0~3)별 배율
const ROUND_MULTIPLIER = [1.3, 1.2, 1.1, 1.0];

module.exports = async ({ params, context, logger }) => {
  const { projectId, playerId } = context;
  const { isWin, mySets, opponentSets, opponentId } = params;

  validate(playerId, isWin, mySets, opponentSets, opponentId);

  const api = new LeaderboardsApi(context);
  // 두 사람 점수를 동시에 읽어서 왕복 한 번 절약
  const [me, opponent] = await Promise.all([
    getEntry(api, projectId, playerId, logger),
    opponentId ? getEntry(api, projectId, opponentId, logger) : Promise.resolve({ score: 0, rank: null }),
  ]);

  const myTier = tierIndex(me.rank);
  const opponentTier = tierIndex(opponent.rank);
  const loserSets = isWin ? opponentSets : mySets;

  const base = isWin ? randomInt(10, 20) : randomInt(5, 15);
  const multiplier = tierMultiplier(opponentTier - myTier, isWin) * ROUND_MULTIPLIER[loserSets];
  const amount = Math.max(1, Math.round(base * multiplier));

  const newScore = isWin ? me.score + amount : Math.max(0, me.score - amount);
  await api.addLeaderboardPlayerScore(projectId, LEADERBOARD_ID, playerId, { score: newScore });

  const delta = newScore - me.score;
  logger.info("ReportMatch", { playerId, isWin, mySets, opponentSets, myTier, opponentTier, base, multiplier, delta, newScore });
  return { delta: delta, score: newScore };
};

function validate(playerId, isWin, mySets, opponentSets, opponentId) {
  if (!playerId) throw new Error("로그인한 플레이어만 호출할 수 있습니다");
  if (typeof isWin !== "boolean") throw new Error("isWin은 true/false 여야 합니다");
  if (!Number.isInteger(mySets) || !Number.isInteger(opponentSets)) throw new Error("세트 수는 정수여야 합니다");

  const winnerSets = isWin ? mySets : opponentSets;
  const loserSets = isWin ? opponentSets : mySets;
  if (winnerSets !== SETS_TO_WIN || loserSets < 0 || loserSets >= SETS_TO_WIN) throw new Error("세트 스코어가 올바르지 않습니다");

  if (opponentId != null && (typeof opponentId !== "string" || opponentId === playerId)) throw new Error("상대 ID가 올바르지 않습니다");
}

// 순위표에 없으면(404) 처음 하는 사람 → 0점, 순위 없음
async function getEntry(api, projectId, playerId, logger) {
  try {
    const result = await api.getLeaderboardPlayerScore(projectId, LEADERBOARD_ID, playerId);
    return { score: result.data.score, rank: result.data.rank };
  } catch (err) {
    if (err.response && err.response.status === 404) return { score: 0, rank: null };
    logger.error("점수 읽기 실패", { playerId, error: err.message });
    throw err;
  }
}

// 게임의 LeaderboardManager.TierByRank 와 같은 기준 (rank는 0부터)
// 원숭이 0, 공룡 1, 마그마 2, 메테오 3, 빙하기 4, 멸종 5
function tierIndex(rank) {
  if (rank == null) return 0;
  if (rank < 1) return 5;
  if (rank < 6) return 4;
  if (rank < 16) return 3;
  if (rank < 31) return 2;
  if (rank < 48) return 1;
  return 0;
}

// diff = 상대 티어 - 내 티어
function tierMultiplier(diff, isWin) {
  let value;
  if (isWin) value = diff >= 0 ? 1 + 0.4 * diff : 1 + 0.15 * diff;
  else value = diff >= 0 ? 1 - 0.2 * diff : 1 - 0.3 * diff;
  return Math.min(3, Math.max(0.3, value));
}

function randomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}
