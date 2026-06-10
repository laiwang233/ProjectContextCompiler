/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        sans: ["Aptos", "Segoe UI", "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "sans-serif"],
        mono: ["Cascadia Mono", "Consolas", "monospace"]
      },
      colors: {
        paper: "#f4f1ea",
        ink: "#1c1e21",
        line: "#c8c1b4",
        moss: "#28705f",
        rust: "#a14b36",
        steel: "#375a7f",
        amber: "#b9852a"
      }
    }
  },
  plugins: []
};
