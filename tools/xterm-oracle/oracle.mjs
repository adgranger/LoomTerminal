// Generates the expected screen snapshots of the conformance fixtures with @xterm/headless.
// Usage: node oracle.mjs <fixtures folder> [--check]
import { readdirSync, readFileSync, writeFileSync, existsSync } from 'node:fs';
import { join, basename } from 'node:path';
import xtermHeadless from '@xterm/headless';
import unicode11 from '@xterm/addon-unicode11';

const { Terminal } = xtermHeadless;
const { Unicode11Addon } = unicode11;

const DEFAULT_COLS = 80;
const DEFAULT_ROWS = 24;

function decodeEscapes(line, lineNumber) {
  let out = '';
  for (let i = 0; i < line.length;) {
    const c = line[i++];
    if (c !== '\\') {
      out += c;
      continue;
    }
    if (i >= line.length) {
      throw new Error(`Line ${lineNumber}: dangling backslash.`);
    }
    const e = line[i++];
    switch (e) {
      case 'e': out += '\x1b'; break;
      case 'a': out += '\x07'; break;
      case 'r': out += '\r'; break;
      case 'n': out += '\n'; break;
      case 't': out += '\t'; break;
      case 'x':
      case 'u': {
        const digits = e === 'x' ? 2 : 4;
        const hex = line.substr(i, digits);
        if (hex.length !== digits || !/^[0-9a-fA-F]+$/.test(hex)) {
          throw new Error(`Line ${lineNumber}: invalid hexadecimal escape.`);
        }
        out += String.fromCharCode(parseInt(hex, 16));
        i += digits;
        break;
      }
      default:
        throw new Error(`Line ${lineNumber}: unknown escape '\\${e}'.`);
    }
  }
  return out;
}

function parseSize(text, lineNumber) {
  const match = /^(\d+) (\d+)$/.exec(text);
  if (!match) {
    throw new Error(`Line ${lineNumber}: expected '<cols> <rows>', got '${text}'.`);
  }
  return [Number(match[1]), Number(match[2])];
}

function readInput(text) {
  const lines = text.replace(/\r\n|\r|\n/g, '\n').split('\n');
  let cols = DEFAULT_COLS;
  let rows = DEFAULT_ROWS;
  const events = [];
  let pending = '';
  const flush = () => {
    if (pending.length > 0) {
      events.push({ kind: 'text', data: pending });
      pending = '';
    }
  };
  lines.forEach((line, i) => {
    if (line.startsWith('#!size ') && i === 0) {
      [cols, rows] = parseSize(line.slice('#!size '.length), i + 1);
    } else if (line.startsWith('#!resize ')) {
      flush();
      const [c, r] = parseSize(line.slice('#!resize '.length), i + 1);
      events.push({ kind: 'resize', cols: c, rows: r });
    } else if (line.startsWith('#!')) {
      throw new Error(`Line ${i + 1}: unknown directive '${line}'.`);
    } else if (!line.startsWith('#')) {
      pending += decodeEscapes(line, i + 1);
    }
  });
  flush();
  return { cols, rows, events };
}

function readRecording(text) {
  const events = [];
  text.replace(/\r\n|\r|\n/g, '\n').split('\n').forEach((line, i) => {
    if (line.length === 0) {
      return;
    }
    if (line.startsWith('O ')) {
      events.push({ kind: 'bytes', data: new Uint8Array(Buffer.from(line.slice(2), 'base64')) });
    } else if (line.startsWith('R ')) {
      const [c, r] = parseSize(line.slice(2), i + 1);
      events.push({ kind: 'resize', cols: c, rows: r });
    } else {
      throw new Error(`Line ${i + 1}: unknown event '${line}'.`);
    }
  });
  return { cols: DEFAULT_COLS, rows: DEFAULT_ROWS, events };
}

function colorName(isDefault, isPalette16, isPalette256, isRgb, value) {
  if (isRgb) {
    return `rgb:${value.toString(16).padStart(6, '0')}`;
  }
  if (isPalette16) {
    return `p16:${value}`;
  }
  if (isPalette256) {
    return `p256:${value}`;
  }
  return 'default';
}

const UNDERLINE_STYLES = ['none', 'single', 'double', 'curly', 'dotted', 'dashed'];

function cellKey(cell) {
  const fgMode = cell.getFgColorMode();
  const bgMode = cell.getBgColorMode();
  // The color modes of xterm.js: 0 default, 0x1000000 p16, 0x2000000 p256, 0x3000000 rgb.
  const fg = colorName(fgMode === 0, fgMode === 0x1000000, fgMode === 0x2000000, fgMode === 0x3000000, cell.getFgColor());
  const bg = colorName(bgMode === 0, bgMode === 0x1000000, bgMode === 0x2000000, bgMode === 0x3000000, cell.getBgColor());
  const flags = [];
  if (cell.isBold()) flags.push('bold');
  if (cell.isDim()) flags.push('dim');
  if (cell.isItalic()) flags.push('italic');
  if (cell.isUnderline()) {
    const style = cell.hasExtendedAttrs() && cell.extended.underlineStyle !== 0 ? cell.extended.underlineStyle : 1;
    flags.push(`underline:${UNDERLINE_STYLES[style]}`);
  }
  if (cell.isBlink()) flags.push('blink');
  if (cell.isInverse()) flags.push('inverse');
  if (cell.isInvisible()) flags.push('invisible');
  if (cell.isStrikethrough()) flags.push('strikethrough');
  if (cell.isOverline()) flags.push('overline');
  return { fg, bg, flags };
}

function captureLine(line, cols) {
  // Trimmed length: after the last cell that has content, including the width of a wide character.
  let trimmed = 0;
  for (let x = 0; x < cols; x++) {
    const cell = line.getCell(x);
    if (cell.getChars() !== '') {
      trimmed = x + cell.getWidth();
    }
  }
  const runs = [];
  let start = 0;
  let current = null;
  for (let x = 0; x < trimmed; x++) {
    const key = cellKey(line.getCell(x));
    if (current !== null && (key.fg !== current.fg || key.bg !== current.bg || key.flags.join() !== current.flags.join())) {
      runs.push({ start, length: x - start, fg: current.fg, bg: current.bg, flags: current.flags });
      start = x;
    }
    current = key;
  }
  if (trimmed > 0) {
    runs.push({ start, length: trimmed - start, fg: current.fg, bg: current.bg, flags: current.flags });
  }
  return { text: line.translateToString(true), wrapped: line.isWrapped, runs };
}

function capture(terminal, title) {
  const buffer = terminal.buffer.active;
  const lines = [];
  for (let y = 0; y < terminal.rows; y++) {
    lines.push(captureLine(buffer.getLine(buffer.baseY + y), terminal.cols));
  }
  const modes = terminal.modes;
  return {
    cols: terminal.cols,
    rows: terminal.rows,
    cursor: { x: buffer.cursorX, y: buffer.cursorY, visible: !terminal._core.coreService.isCursorHidden },
    activeBuffer: buffer.type,
    modes: {
      applicationCursorKeys: modes.applicationCursorKeysMode,
      bracketedPaste: modes.bracketedPasteMode,
      insert: modes.insertMode,
      origin: modes.originMode,
      wraparound: modes.wraparoundMode,
      mouseTracking: modes.mouseTrackingMode,
    },
    title,
    lines,
  };
}

const write = (terminal, data) => new Promise((resolve) => terminal.write(data, resolve));

async function replay(script) {
  const terminal = new Terminal({ cols: script.cols, rows: script.rows, scrollback: 1000, allowProposedApi: true });
  terminal.loadAddon(new Unicode11Addon());
  terminal.unicode.activeVersion = '11';
  let title = '';
  terminal.onTitleChange((value) => { title = value; });
  for (const event of script.events) {
    if (event.kind === 'resize') {
      terminal.resize(event.cols, event.rows);
    } else {
      await write(terminal, event.data);
    }
  }
  const snapshot = capture(terminal, title);
  terminal.dispose();
  return snapshot;
}

async function main() {
  const args = process.argv.slice(2);
  const check = args.includes('--check');
  const folder = args.find((a) => !a.startsWith('--'));
  if (!folder) {
    console.error('Usage: node oracle.mjs <fixtures folder> [--check]');
    process.exit(2);
  }
  const jobs = [
    ['scenarios', '.input', readInput],
    ['recorded', '.vtrec', readRecording],
  ];
  let changed = 0;
  for (const [sub, extension, reader] of jobs) {
    const dir = join(folder, sub);
    if (!existsSync(dir)) {
      continue;
    }
    for (const file of readdirSync(dir).filter((f) => f.endsWith(extension)).sort()) {
      const snapshot = await replay(reader(readFileSync(join(dir, file), 'utf8')));
      const json = `${JSON.stringify(snapshot, null, 2)}\n`;
      const target = join(dir, `${basename(file, extension)}.json`);
      const same = existsSync(target) && readFileSync(target, 'utf8') === json;
      if (!same) {
        changed++;
        console.log(`${check ? 'would change' : 'wrote'} ${target}`);
        if (!check) {
          writeFileSync(target, json);
        }
      }
    }
  }
  if (check && changed > 0) {
    process.exit(1);
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
