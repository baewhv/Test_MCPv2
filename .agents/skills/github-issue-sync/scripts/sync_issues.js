const fs = require('fs');
const https = require('https');

function getGitHubToken() {
  const mcpConfigPath = 'C:\\Users\\KGA1\\.gemini\\config\\mcp_config.json';
  if (fs.existsSync(mcpConfigPath)) {
    const mcpConfig = JSON.parse(fs.readFileSync(mcpConfigPath, 'utf8'));
    const githubMcp = mcpConfig.mcpServers && mcpConfig.mcpServers.github;
    if (githubMcp && githubMcp.env) {
      return githubMcp.env.GITHUB_PERSONAL_ACCESS_TOKEN || githubMcp.env.GITHUB_TOKEN || '';
    }
  }
  return process.env.GITHUB_TOKEN || '';
}

const token = getGitHubToken();

function githubRequest(method, endpoint, data) {
  return new Promise((resolve, reject) => {
    const options = {
      hostname: 'api.github.com',
      port: 443,
      path: endpoint,
      method: method,
      headers: {
        'User-Agent': 'Antigravity-Agent',
        'Authorization': `Bearer ${token}`,
        'Accept': 'application/vnd.github.v3+json',
        'Content-Type': 'application/json'
      }
    };
    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', chunk => body += chunk);
      res.on('end', () => {
        try {
          const json = body ? JSON.parse(body) : {};
          if (res.statusCode >= 200 && res.statusCode < 300) {
            resolve(json);
          } else {
            reject(new Error(`HTTP ${res.statusCode}: ${JSON.stringify(json)}`));
          }
        } catch (e) {
          reject(e);
        }
      });
    });
    req.on('error', reject);
    if (data) req.write(JSON.stringify(data));
    req.end();
  });
}

async function run() {
  const owner = 'baewhv';
  const repo = 'Test_MCPTest';

  console.log(`=== GitHub Issues 점검 및 동기화: ${owner}/${repo} ===\n`);
  
  // 1. Open 및 Closed 이슈 조회
  const issues = await githubRequest('GET', `/repos/${owner}/${repo}/issues?state=all&per_page=100`);
  
  const pendingProposals = [];
  const processedRejected = [];
  const processedAccepted = [];
  const processedCompleted = [];

  const worklistPath = 'C:\\Users\\KGA1\\Desktop\\TestMCP\\docs\\work\\worklist.md';
  let worklistContent = fs.existsSync(worklistPath) ? fs.readFileSync(worklistPath, 'utf8') : '';

  for (const issue of issues) {
    if (issue.pull_request) continue; // PR 제외, 순수 Issue만 대상

    const title = issue.title;
    const number = issue.number;
    const isOpen = issue.state === 'open';

    // 1. [반려] 태그 -> open 상태면 close 처리
    if (title.includes('[반려]') && isOpen) {
      await githubRequest('PATCH', `/repos/${owner}/${repo}/issues/${number}`, { state: 'closed' });
      processedRejected.push(`#${number} ${title}`);
    }

    // 2. [수락] 태그 -> [착수]로 제목 변경 및 worklist 최우선 지시사항 등록 확인
    else if (title.includes('[수락]')) {
      const newTitle = title.replace('[수락]', '[착수]');
      await githubRequest('PATCH', `/repos/${owner}/${repo}/issues/${number}`, { title: newTitle });
      
      // worklist에 등록 확인
      const taskEntry = `- [ ] ${title.replace(/\[AI_[a-z]+\]\[수락\]\s*/, '')} (Issue #${number})`;
      if (!worklistContent.includes(`(Issue #${number})`)) {
        if (worklistContent.includes('## 사용자 최우선 지시사항')) {
          worklistContent = worklistContent.replace('## 사용자 최우선 지시사항', `## 사용자 최우선 지시사항\n${taskEntry}`);
        } else {
          worklistContent = `## 사용자 최우선 지시사항\n${taskEntry}\n\n` + worklistContent;
        }
        fs.writeFileSync(worklistPath, worklistContent, 'utf8');
      }
      processedAccepted.push(`#${number} ${title} ➔ ${newTitle}`);
    }

    // 3. [완료] 태그 -> open 상태면 close 처리
    else if (title.includes('[완료]') && isOpen) {
      await githubRequest('PATCH', `/repos/${owner}/${repo}/issues/${number}`, { state: 'closed' });
      processedCompleted.push(`#${number} ${title}`);
    }

    // 4. [제안] 태그 -> open 상태인 경우 카운트 및 목록화
    else if (title.includes('[제안]') && isOpen) {
      pendingProposals.push({ number, title, url: issue.html_url });
    }
  }

  console.log('### 이슈 동기화 결과 요약:');
  console.log(`- [반려 ➔ Close 처리]: ${processedRejected.length}건`);
  processedRejected.forEach(item => console.log(`  * ${item}`));

  console.log(`- [수락 ➔ 착수 전환 및 worklist 반영]: ${processedAccepted.length}건`);
  processedAccepted.forEach(item => console.log(`  * ${item}`));

  console.log(`- [완료 ➔ Close 처리]: ${processedCompleted.length}건`);
  processedCompleted.forEach(item => console.log(`  * ${item}`));

  console.log(`\n- [미결 제안 (확인 필요)]: ${pendingProposals.length}건`);
  if (pendingProposals.length > 0) {
    console.log(`⚠️ 확인해야 할 이슈(제안)가 ${pendingProposals.length}건 있습니다:`);
    pendingProposals.forEach(p => console.log(`  * #${p.number}: ${p.title}`));
  } else {
    console.log('  * 대기 중인 미결 제안이 없습니다.');
  }
}

run().catch(err => {
  console.error('Error:', err.message);
  process.exit(1);
});
