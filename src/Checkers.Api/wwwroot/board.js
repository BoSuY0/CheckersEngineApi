'use strict';

const INITIAL_POSITION = 'B:W21-32:B1-12';

const board = document.getElementById('board');
const positionInput = document.getElementById('position');
const levelSelect = document.getElementById('level');
const statusLine = document.getElementById('status');
const suggestion = document.getElementById('suggestion');
const validation = document.getElementById('validation');
const errorMessage = document.getElementById('error');

// The server owns the rules: it returns the position and every legal move with the position that move leads to.
let current = { position: '', moves: [] };
let selected = [];
let lastMove = [];
let thinking = false;

// Standard numbering: square 1 is top row, second column; rows alternate their first playable column.
function cellOf(square) {
  const index = square - 1;
  const row = Math.floor(index / 4);
  return row * 8 + 2 * (index % 4) + (row % 2 === 0 ? 1 : 0);
}

// "11-15" or "6x15x24": the start square followed by every landing square.
function pathOf(move) {
  return move.split(/[-x]/).map(Number);
}

// Reads a canonical position ("B:W18,K22:B1,5"): no ranges, kings prefixed by "K".
function piecesOf(position) {
  const pieces = new Map();
  for (const list of position.split(':').slice(1)) {
    const color = list[0] === 'W' ? 'white' : 'black';
    for (const item of list.slice(1).split(',').filter(Boolean)) {
      const king = item.startsWith('K');
      pieces.set(Number(king ? item.slice(1) : item), { color, king });
    }
  }
  return pieces;
}

function sideToMove() {
  return current.position.startsWith('W') ? 'white' : 'black';
}

// The moves that continue the squares clicked so far.
function candidates() {
  return current.moves.filter((move) => selected.every((square, index) => move.path[index] === square));
}

function drawBoard() {
  const pieces = piecesOf(current.position);
  const next = new Set(candidates().map((move) => move.path[selected.length]));
  const cells = Array.from({ length: 64 }, () => document.createElement('div'));
  for (let square = 1; square <= 32; square++) {
    const cell = cells[cellOf(square)];
    cell.dataset.square = square;
    cell.classList.add('dark');
    cell.classList.toggle('path', lastMove.includes(square));
    cell.classList.toggle('selected', selected.includes(square));
    cell.classList.toggle(selected.length === 0 ? 'movable' : 'target', !thinking && next.has(square));
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

function showStatus() {
  const side = sideToMove();
  const name = side === 'white' ? 'White' : 'Black';
  statusLine.dataset.side = side;
  if (thinking) {
    statusLine.textContent = `Engine is thinking for ${name}…`;
  } else if (current.moves.length === 0) {
    statusLine.textContent = `${name} has no legal move: ${side === 'white' ? 'Black' : 'White'} wins`;
  } else {
    const capture = current.moves[0].move.includes('x') ? ', capture is mandatory' : '';
    statusLine.textContent = `${name} to move${capture}`;
  }
}

function render() {
  drawBoard();
  showStatus();
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

async function load(position, move = []) {
  const result = await post('/v1/move/legal', { position });
  current = {
    position: result.position,
    moves: result.moves.map((legal) => ({ ...legal, path: pathOf(legal.move) })),
  };
  positionInput.value = result.position;
  selected = [];
  lastMove = move;
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

// The engine plays the side to move: /v1/move/suggest picks the move, and the listed result of that move follows.
async function engineMove() {
  thinking = true;
  render();
  try {
    const result = await post('/v1/move/suggest', {
      gameId: 'checkers-8x8',
      state: { notation: 'PDN', position: current.position },
      ...(levelSelect.value && { level: levelSelect.value }),
    });
    showSuggestion(result);
    const move = current.moves.find((legal) => legal.move === result.bestMove);
    await load(move.position, move.path);
  } finally {
    thinking = false;
    render();
  }
}

// Clicks select a piece, then its landing squares one by one; a complete path plays the move and the engine replies.
board.addEventListener('click', (event) => {
  const square = Number(event.target.closest('[data-square]')?.dataset.square);
  if (thinking || !square) {
    return;
  }
  const isNext = candidates().some((move) => move.path[selected.length] === square);
  const isStart = current.moves.some((move) => move.path[0] === square);
  selected = isNext ? [...selected, square] : isStart ? [square] : [];
  const played = candidates().find((move) => move.path.length === selected.length);
  if (!played) {
    drawBoard();
    return;
  }
  attempt(async () => {
    await load(played.position, played.path);
    render();
    if (current.moves.length > 0) {
      await engineMove();
    }
  });
});

document.getElementById('position-form').addEventListener('submit', (event) => {
  event.preventDefault();
  suggestion.replaceChildren();
  attempt(async () => {
    await load(positionInput.value);
    render();
  });
});

document.getElementById('suggest-form').addEventListener('submit', (event) => {
  event.preventDefault();
  if (!thinking && current.moves.length > 0) {
    attempt(engineMove);
  }
});

document.getElementById('new-game').addEventListener('click', () => {
  positionInput.value = INITIAL_POSITION;
  document.getElementById('position-form').requestSubmit();
});

document.getElementById('validate-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  validation.textContent = '';
  await attempt(async () => {
    const result = await post('/v1/move/validate', {
      position: current.position,
      move: document.getElementById('move').value,
    });
    validation.textContent = `legal: ${result.legal}`;
    validation.dataset.legal = result.legal;
  });
});

document.getElementById('position-form').requestSubmit();
