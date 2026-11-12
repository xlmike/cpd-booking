/**
 * Progressive enhancement: filters the course list as the user types.
 * Without JavaScript the search form still works through a normal GET request.
 * The result count is in a role="status" region, so screen readers announce changes.
 */
export class CourseFilter {
  /** @param {HTMLInputElement} input */
  constructor (input) {
    this.input = input
    this.list = document.getElementById(input.getAttribute('aria-controls') ?? '')
    this.status = document.getElementById('course-count')
    if (!this.list || !this.status) return

    this.items = Array.from(this.list.querySelectorAll('[data-search]'))
    let timer
    input.addEventListener('input', () => {
      clearTimeout(timer)
      timer = setTimeout(() => this.filter(), 300) // wait for typing to pause, so announcements aren't noisy
    })
  }

  filter () {
    const term = this.input.value.trim().toLowerCase()
    let shown = 0
    for (const item of this.items) {
      const matches = term === '' || item.dataset.search.toLowerCase().includes(term)
      item.hidden = !matches
      if (matches) shown++
    }
    // textContent, never innerHTML: user input is never written into the page as HTML (XSS).
    this.status.textContent = `${shown} ${shown === 1 ? 'course' : 'courses'} found`
  }
}
