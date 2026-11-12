import { initAll } from '/lib/govuk/govuk-frontend.min.js'
import { CourseFilter } from '/js/course-filter.mjs'

initAll()

document.querySelectorAll('[data-module="app-course-filter"]').forEach((input) => {
  new CourseFilter(input) // eslint-disable-line no-new
})
