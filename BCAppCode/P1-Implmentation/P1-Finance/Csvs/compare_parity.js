const fs = require('fs');

function parseCsvLine(line) {
  const out = [];
  let cur = '';
  let inQuotes = false;
  for (let i = 0; i < line.length; i++) {
    const ch = line[i];
    if (inQuotes) {
      if (ch === '"') {
        if (line[i + 1] === '"') {
          cur += '"';
          i++;
        } else {
          inQuotes = false;
        }
      } else {
        cur += ch;
      }
    } else if (ch === '"') {
      inQuotes = true;
    } else if (ch === ',') {
      out.push(cur);
      cur = '';
    } else {
      cur += ch;
    }
  }
  out.push(cur);
  return out;
}

function loadCsv(file) {
  const text = fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '');
  const lines = text.split(/\r?\n/).filter((l) => l.trim());
  let start = 0;
  if (lines[0].startsWith('MethodName,')) start = 1;
  const rows = {};
  for (let i = start; i < lines.length; i++) {
    const cols = parseCsvLine(lines[i]);
    if (cols.length < 4) continue;
    const [method, table, keyJson, rowJson] = cols;
    const key = JSON.parse(keyJson);
    const row = !rowJson || rowJson === 'NULL' ? null : JSON.parse(rowJson);
    rows[method] = { table, key, row };
  }
  return rows;
}

function norm(v) {
  if (v === null || v === undefined) return null;
  if (typeof v === 'boolean') return v;
  if (typeof v === 'number') return v;
  if (typeof v === 'string') {
    let s = v.trim().replace(/\\\//g, '/');
    if (s === '') return '';
    if (/^-?\d+$/.test(s)) return parseInt(s, 10);
    if (/^-?\d+\.\d+$/.test(s)) return parseFloat(s);
    s = s.replace(/\.\d+/, '').replace('T', ' ');
    return s;
  }
  return v;
}

function compare(a, b, path = '') {
  const diffs = [];
  if (
    a &&
    typeof a === 'object' &&
    !Array.isArray(a) &&
    b &&
    typeof b === 'object' &&
    !Array.isArray(b)
  ) {
    const keys = new Set([...Object.keys(a), ...Object.keys(b)]);
    for (const k of [...keys].sort()) {
      const hasA = Object.prototype.hasOwnProperty.call(a, k);
      const hasB = Object.prototype.hasOwnProperty.call(b, k);
      const p = path ? `${path}.${k}` : k;
      if (!hasA) {
        if (b[k] !== null && b[k] !== '' && b[k] !== undefined) {
          diffs.push([p, '<missing>', b[k]]);
        }
        continue;
      }
      if (!hasB) {
        if (a[k] !== null && a[k] !== '' && a[k] !== undefined) {
          diffs.push([p, a[k], '<missing>']);
        }
        continue;
      }
      diffs.push(...compare(a[k], b[k], p));
    }
    return diffs;
  }
  const na = norm(a);
  const nb = norm(b);
  if (na === nb) return diffs;
  if (String(a) === String(b)) return diffs;
  diffs.push([path, a, b]);
  return diffs;
}

function fmt(v) {
  if (typeof v === 'string' && v.length > 70) return `${v.slice(0, 70)}...`;
  return v;
}

const bhgPath =
  'c:/Users/tsaty/Desktop/BCAppCode/BCAppCode/P1-Implmentation/P1-Finance/Csvs/data1.csv';
const fabPath =
  'c:/Users/tsaty/Desktop/BCAppCode/BCAppCode/P1-Implmentation/P1-Finance/Csvs/untitled (5).csv';

const bhg = loadCsv(bhgPath);
const fab = loadCsv(fabPath);
const ignore = new Set(['LastModAt', 'RowChkSum']);
const methods = [...new Set([...Object.keys(bhg), ...Object.keys(fab)])].sort();

console.log('=== PARITY SUMMARY: BHG_DR vs Fabric Silver (site AHK sample rows) ===');
console.log(`BHG rows: ${Object.keys(bhg).length} | Fabric rows: ${Object.keys(fab).length}\n`);

let pass = 0;
let warn = 0;
let fail = 0;

for (const m of methods) {
  if (!bhg[m]) {
    console.log(`[${m}] MISSING in BHG`);
    fail++;
    continue;
  }
  if (!fab[m]) {
    console.log(`[${m}] MISSING in Fabric`);
    fail++;
    continue;
  }

  const br = bhg[m].row;
  const fr = fab[m].row;

  if (br === null && fr === null) {
    console.log(`[${m}] both NULL`);
    warn++;
    continue;
  }
  if (br === null) {
    console.log(`[${m}] BHG NULL | Fabric has data`);
    fail++;
    continue;
  }
  if (fr === null) {
    console.log(`[${m}] Fabric NULL | BHG has data`);
    fail++;
    continue;
  }

  const diffs = compare(br, fr);
  const meaningful = diffs.filter((d) => !ignore.has(d[0].split('.').pop()));
  const csA = br.RowChkSum ?? 'n/a';
  const csB = fr.RowChkSum ?? 'n/a';
  const csMatch = csA === csB;

  console.log(
    `[${m}] RowChkSum match: ${csMatch} (${csA} vs ${csB}) | meaningful diffs: ${meaningful.length}`
  );
  meaningful.slice(0, 12).forEach(([p, a, b]) => {
    console.log(`  - ${p}: BHG=${JSON.stringify(fmt(a))} | Fabric=${JSON.stringify(fmt(b))}`);
  });
  if (meaningful.length > 12) {
    console.log(`  ... +${meaningful.length - 12} more`);
  }
  console.log('');

  if (meaningful.length === 0 && csMatch !== false) pass++;
  else if (meaningful.every(([p]) => ['PrimKey', 'PCID', 'SalesForceId', 'ExpirationDate', 'f10local', 'tpTermDate', 'tpaEffDATE', 'tpaTermDATE', 'tpadt', 'REMARKS'].some((x) => p.endsWith(x)))) warn++;
  else fail++;
}

console.log('=== COUNTS ===');
console.log(`Clean parity (business fields + checksum): ${pass}`);
console.log(`Minor/metadata gaps only: ${warn}`);
console.log(`Needs investigation: ${fail}`);
