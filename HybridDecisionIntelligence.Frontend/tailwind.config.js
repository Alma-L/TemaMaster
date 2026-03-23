/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        indigo: {
          50: '#f0f4ff',
          100: '#e0e7ff',
          600: '#4f46e5',
          700: '#4338ca',
          900: '#312e81',
        },
        blue: {
          50: '#f0f4ff',
          500: '#3b82f6',
          600: '#2563eb',
        },
        green: {
          50: '#f0fdf4',
          100: '#dcfce7',
          600: '#16a34a',
          800: '#166534',
        },
        red: {
          50: '#fef2f2',
          100: '#fee2e2',
          600: '#dc2626',
          800: '#991b1b',
        },
        yellow: {
          50: '#fefce8',
          100: '#fef08a',
          200: '#fef08a',
          600: '#ca8a04',
        },
        gray: {
          50: '#f9fafb',
          100: '#f3f4f6',
          200: '#e5e7eb',
          300: '#d1d5db',
          500: '#6b7280',
          600: '#4b5563',
          700: '#374151',
          800: '#1f2937',
          900: '#111827',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['Fira Code', 'monospace'],
      },
      boxShadow: {
        sm: '0 1px 2px 0 rgb(0 0 0 / 0.05)',
        md: '0 4px 6px -1px rgb(0 0 0 / 0.1)',
        lg: '0 10px 15px -3px rgb(0 0 0 / 0.1)',
        xl: '0 20px 25px -5px rgb(0 0 0 / 0.1)',
      },
      animation: {
        spin: 'spin 1s linear infinite',
        pulse: 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
};
