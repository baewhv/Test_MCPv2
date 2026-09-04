const { execFile, execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const projectRoot = path.resolve(__dirname, '../../../../');
const logsDir = path.join(projectRoot, 'Logs');
const defaultUnityPath = 'C:\\Program Files\\Unity\\Hub\\Editor\\6000.5.8f1\\Editor\\Unity.exe';

function getUnityPath() {
  const specPath = path.join(projectRoot, 'docs/PROJECT_SPEC.md');
  if (fs.existsSync(specPath)) {
    const content = fs.readFileSync(specPath, 'utf8');
    const match = content.match(/Unity Editor Path\s*:\s*`([^`]+)`/i);
    if (match && fs.existsSync(match[1])) {
      return match[1];
    }
  }
  if (fs.existsSync(defaultUnityPath)) return defaultUnityPath;
  throw new Error('Unity.exe 경로를 찾을 수 없습니다. docs/PROJECT_SPEC.md를 확인해주세요.');
}

const args = process.argv.slice(2);
const command = args[0] || 'compile';

async function runCompile() {
  const unityPath = getUnityPath();
  const logFile = path.join(logsDir, 'Editor.log');
  console.log(`[Unity CLI] 무인 컴파일 검증 시작 (Project: ${projectRoot})`);

  try {
    execSync(`"${unityPath}" -batchmode -projectPath "${projectRoot}" -quit`, {
      stdio: 'inherit',
      timeout: 180000
    });
    console.log('[Unity CLI] 컴파일 검증 완료: 정상 (Exit Code: 0)');
    return true;
  } catch (err) {
    console.error(`[Unity CLI] 컴파일 실패: ${err.message}`);
    process.exit(1);
  }
}

async function runTests(platform = 'EditMode') {
  const unityPath = getUnityPath();
  const resultsPath = path.join(logsDir, `test_results_${platform.toLowerCase()}.xml`);
  console.log(`[Unity CLI] NUnit 단위 테스트 실행 (${platform})`);

  try {
    execSync(`"${unityPath}" -batchmode -projectPath "${projectRoot}" -runTests -testPlatform ${platform} -testResults "${resultsPath}" -quit`, {
      stdio: 'inherit',
      timeout: 300000
    });
    console.log(`[Unity CLI] 테스트 실행 완료. 결과 파일: ${resultsPath}`);
    if (fs.existsSync(resultsPath)) {
      const xml = fs.readFileSync(resultsPath, 'utf8');
      const totalMatch = xml.match(/total="(\d+)"/);
      const passedMatch = xml.match(/passed="(\d+)"/);
      const failedMatch = xml.match(/failed="(\d+)"/);
      console.log(`[Unity CLI 테스트 결과] 전체: ${totalMatch ? totalMatch[1] : 0}, 통과: ${passedMatch ? passedMatch[1] : 0}, 실패: ${failedMatch ? failedMatch[1] : 0}`);
    }
    return true;
  } catch (err) {
    console.error(`[Unity CLI] 테스트 실패 또는 에러 발생: ${err.message}`);
    process.exit(1);
  }
}

switch (command) {
  case 'compile':
    runCompile();
    break;
  case 'test':
    const platform = args[1] || 'EditMode';
    runTests(platform);
    break;
  default:
    console.log('사용법: node unity_cli.js [compile | test <EditMode|PlayMode>]');
}
