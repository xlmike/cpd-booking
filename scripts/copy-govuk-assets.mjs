// Copies GOV.UK Frontend's compiled JavaScript and assets into wwwroot, so the app serves them
// from its own origin (allowed by the Content Security Policy) instead of a third-party CDN.
import { cpSync, mkdirSync } from 'node:fs'
import { join } from 'node:path'

const source = join('node_modules', 'govuk-frontend', 'dist', 'govuk')
const target = join('src', 'CpdBooking.Web', 'wwwroot', 'lib', 'govuk')

mkdirSync(target, { recursive: true })
for (const file of ['govuk-frontend.min.js', 'govuk-frontend.min.js.map']) {
  cpSync(join(source, file), join(target, file))
}
cpSync(join(source, 'assets'), join(target, 'assets'), { recursive: true })

console.log(`Copied GOV.UK Frontend to ${target}`)
