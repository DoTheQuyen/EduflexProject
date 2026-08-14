const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

function getSha() {
  if (process.env.BUILD_SHA) return process.env.BUILD_SHA.substring(0, 7);
  try {
    return execSync('git rev-parse --short HEAD').toString().trim();
  } catch {
    return 'dev';
  }
}

const sha = getSha();
const buildDate = new Date().toISOString();

const content = `export const buildInfo = {
  version: '${sha}',
  buildDate: '${buildDate}'
};
`;

fs.writeFileSync(
  path.join(__dirname, '..', 'src', 'app', 'environments', 'build-info.ts'),
  content,
);

console.log(`Generated build-info.ts -> version ${sha}, buildDate ${buildDate}`);
