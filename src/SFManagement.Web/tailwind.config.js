/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Views/**/*.cshtml',
    './Pages/**/*.cshtml',
    './wwwroot/js/**/*.js',
    './**/*.html',
  ],
  darkMode: 'class',
  theme: {
    extend: {
      fontSize: {
        'pill': ['13px', { lineHeight: '1.25rem' }],
      },
      colors: {
        brand: {
          DEFAULT: '#21668f',
          dark: '#19869b',
          light: '#e7f9f8',
          deep: '#1a5273',
        },
      },
    },
  },
};
