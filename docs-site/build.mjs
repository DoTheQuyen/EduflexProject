// Build both manuals.
//
//   site/          Staff manual   — every page, for the Staff Portal.
//   site-student/  Student manual — the student subset, for the Student Portal.
//
// Both come from the same docs/ tree and the same .vitepress/config.mts; the config
// branches on DOCS_TARGET. Each build runs in its own process so the config module is
// evaluated fresh with the right target — importing it twice in one process would
// reuse the first evaluation.
import { spawn } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { dirname } from 'node:path'

const root = dirname(fileURLToPath(import.meta.url))

function buildManual(target) {
  return new Promise((resolve, reject) => {
    console.log(`\n— building ${target} manual —`)
    const child = spawn('npx', ['vitepress', 'build', 'docs'], {
      cwd: root,
      env: { ...process.env, DOCS_TARGET: target },
      stdio: 'inherit',
      shell: process.platform === 'win32'
    })
    child.on('exit', code =>
      code === 0 ? resolve() : reject(new Error(`${target} build failed (exit ${code})`))
    )
  })
}

const only = process.argv[2]?.replace(/^--/, '')
const targets = only ? [only] : ['staff', 'student']

for (const target of targets) {
  if (!['staff', 'student'].includes(target)) {
    console.error(`unknown target "${target}" — expected staff or student`)
    process.exit(1)
  }
  await buildManual(target)
}

console.log('\ndone')
