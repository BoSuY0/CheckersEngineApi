'use strict';

const board = document.getElementById('board');
const positionInput = document.getElementById('position');
const suggestion = document.getElementById('suggestion');
const validation = document.getElementById('validation');
const errorMessage = document.getElementById('error');

// Standard numbering: square 1 is top row, second column; rows alternate their first playable column.
function cellOf(square) {
  const index = square - 1;
  const row = Math.floor(index / 4);
  return row * 8 + 2 * (index % 4) + (row % 2 === 0 ? 1 : 0);
}

// Reads the canonical position the API returns ("pdn:B:W18,K22:B1,5"): no ranges, kings prefixed by "K".
function piecesOf(positionKey) {
  const pieces = new Map();
  for (const list of positionKey.split(':').slice(2)) {
    const color = list[0] === 'W' ? 'white' : 'black';
    for (const item of list.slice(1).split(',').filter(Boolean)) {
      const king = item.startsWith('K');
      pieces.set(Number(king ? item.slice(1) : item), { color, king });
    }
  }
  return pieces;
}

function drawBoard(pieces = new Map(), path = []) {
  const cells = Array.from({ length: 64 }, () => document.createElement('div'));
  for (let square = 1; square <= 32; square++) {
    const cell = cells[cellOf(square)];
    cell.className = path.includes(square) ? 'dark path' : 'dark';
    const number = document.createElement('span');
    number.className = 'number';
    number.textContent = square;
    cell.append(number);
    const piece = pieces.get(square);
    if (piece) {
      const disc = document.createElement('span');
      disc.className = `piece ${piece.color}${piece.king ? ' king' : ''}`;
      cell.append(disc);
    }
  }
  board.replaceChildren(...cells);
}

async function post(url, body) {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  const json = await response.json();
  if (!response.ok) {
    const details = json.detail ?? Object.values(json.errors ?? {}).flat().join(' ');
    throw new Error(`${response.status} ${json.title} ${details}`.trim());
  }
  return json;
}

// One unbreakable span per move: a long line wraps between moves, never after a move's hyphen.
function movesOf(moves) {
  return moves.flatMap((move, index) => {
    const span = document.createElement('span');
    span.className = 'move';
    span.textContent = move;
    return index === 0 ? [span] : [' ', span];
  });
}

function showSuggestion(result) {
  const rows = {
    bestMove: result.bestMove,
    pv: movesOf(result.pv),
    scoreOrWDL: result.scoreOrWDL,
    depth: result.depth,
    nodes: result.nodes,
    tablebaseHit: result.info.tablebaseHit,
    timeMs: result.info.timeMs,
    positionKey: result.positionKey,
  };
  suggestion.replaceChildren(...Object.entries(rows).flatMap(([name, value]) => {
    const term = document.createElement('dt');
    term.textContent = name;
    const description = document.createElement('dd');
    description.append(...[value].flat());
    return [term, description];
  }));
}

// Runs one request and shows its failure (ProblemDetails) instead of a result.
async function attempt(request) {
  errorMessage.textContent = '';
  try {
    await request();
  } catch (error) {
    errorMessage.textContent = error.message;
  }
}

document.getElementById('suggest-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  const level = document.getElementById('level').value;
  suggestion.replaceChildren();
  await attempt(async () => {
    const result = await post('/v1/move/suggest', {
      gameId: 'checkers-8x8',
      state: { notation: 'PDN', position: positionInput.value },
      ...(level && { level }),
    });
    drawBoard(piecesOf(result.positionKey), result.bestMove.split(/[-x]/).map(Number));
    showSuggestion(result);
  });
});

document.getElementById('validate-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  validation.textContent = '';
  await attempt(async () => {
    const result = await post('/v1/move/validate', {
      position: positionInput.value,
      move: document.getElementById('move').value,
    });
    validation.textContent = `legal: ${result.legal}`;
    validation.dataset.legal = result.legal;
  });
});

drawBoard();
