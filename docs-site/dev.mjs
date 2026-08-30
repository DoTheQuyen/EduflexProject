// Run one manual's dev server.
//
//   node dev.mjs           staff manual   -> http://localhost:5173
//   node dev.mjs student   student manual -> http://localhost:5174
//
// Same shape as build.mjs: DOCS_TARGET selects which manual .vitepress/config.mts
// builds, so the two dev servers must be separate processes on separate ports.
import { spawn } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { dirname } from 'node:path'

const root = dirname(fileURLToPath(import.meta.url))
const target = (process.argv[2] || 'staff').replace(/^--/, '')

if (!['staff', 'student'].includes(target)) {
  console.error(`unknown target "${target}" — expected staff or student`)
  process.exit(1)
}

const port = target === 'student' ? '5174' : '5173'
console.log(`${target} manual -> http://localhost:${port}`)

spawn('npx', ['vitepress', 'dev', 'docs', '--port', port], {
  cwd: root,
  env: { ...process.env, DOCS_TARGET: target },
  stdio: 'inherit',
  shell: process.platform === 'win32'
})
