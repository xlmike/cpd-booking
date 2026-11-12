// Runs before the page renders so components don't flash. Kept in a file because the
// Content Security Policy allows no inline scripts.
document.body.className += ' js-enabled' + ('noModule' in HTMLScriptElement.prototype ? ' govuk-frontend-supported' : '')
