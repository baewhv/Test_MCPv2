const fs = require('fs');
const path = require('path');

const args = process.argv.slice(2);
const params = {};
for (let i = 0; i < args.length; i++) {
  if (args[i].startsWith('--')) {
    const key = args[i].substring(2);
    const value = args[i + 1] && !args[i + 1].startsWith('--') ? args[i + 1] : true;
    params[key] = value;
    if (value !== true) i++;
  }
}

const from = params.from || 'System';
const to = params.to || 'All';
const type = params.type || 'Notice';
const msg = params.msg || '소통 내용 없음';

const now = new Date();
const yyyy = now.getFullYear();
const mm = String(now.getMonth() + 1).padStart(2, '0');
const dd = String(now.getDate()).padStart(2, '0');
const todayStr = `${yyyy}-${mm}-${dd}`;

const hh = String(now.getHours()).padStart(2, '0');
const min = String(now.getMinutes()).padStart(2, '0');
const ss = String(now.getSeconds()).padStart(2, '0');
const timeStr = `${hh}:${min}:${ss}`;

const logDir = path.resolve(__dirname, '../../../../docs/logs');
if (!fs.existsSync(logDir)) {
  fs.mkdirSync(logDir, { recursive: true });
}

const logFile = path.join(logDir, `agent_comm_${todayStr}.md`);
let content = '';

if (!fs.existsSync(logFile)) {
  content = `# 에이전트 실시간 협업 소통 기록 (${todayStr})\n\n| 시각 (Time) | 발신 (From) | 수신 (To) | 소통 유형 | 주요 전달 내용 및 데이터 요약 |\n| :--- | :--- | :--- | :--- | :--- |\n`;
} else {
  content = fs.readFileSync(logFile, 'utf8');
}

const sanitizedMsg = msg.replace(/\r?\n/g, ' ').replace(/\|/g, '-');
const newRow = `| ${timeStr} | ${from} | ${to} | ${type} | ${sanitizedMsg} |\n`;
content += newRow;

fs.writeFileSync(logFile, content, 'utf8');
console.log(`[소통 로깅 완료] ${timeStr} [${from} -> ${to}] ${type}: ${sanitizedMsg}`);
